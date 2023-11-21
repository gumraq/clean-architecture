using AutoMapper;
using IDeal.Common.Components;
using IDeal.Common.Messaging.Messages;
using Cargo.Infrastructure.Data;
using Cargo.Infrastructure.Data.Model;
using Cargo.Infrastructure.Data.Model.Settings;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cargo.Application.Services;
using IDeal.Common.Components.Messages.ObjectStructures.Fbrs.Ver2;
using System.Globalization;
using MoreLinq;
using IDeal.Common.Components.Messages.ObjectStructures.Fbls.Ver4;
using Cargo.Application.Validation;
using IDeal.Common.Components.Messages.ObjectStructures.Fnas.Ver1;

namespace Cargo.Application.CommandHandlers
{
    public class ProcessFbrConsumer : IConsumer<ProcessFbr>
    {
        private IMapper mapper;
        private CargoContext dbContext;
        private ILogger<ProcessFbrConsumer> logger;
        private AwbService awbService;
        private ProcessFbrValidator processFbrValidator;


        public ProcessFbrConsumer(ILogger<ProcessFbrConsumer> logger, IMapper mapper, CargoContext dbContext, AwbService awbService, ProcessFbrValidator processFbrValidator)
        {
            this.logger = logger;
            this.dbContext = dbContext;
            this.mapper = mapper;
            this.awbService = awbService;
            this.processFbrValidator = processFbrValidator;
        }

        public async Task Consume(ConsumeContext<ProcessFbr> context)
        {
            var validationResult = processFbrValidator.Validate(context.Message);
            if (!validationResult.IsValid)
            {
                var fna = new Fna()
                {
                    StandardMessageIdentification = new IDeal.Common.Components.Messages.ObjectStructures.Fnas.Ver1.StandardMessageIdentification() { StandardMessageIdentifier = "FNA", MessageTypeVersionNumber = "1" },
                    ReceivedMessageDetail = context.Message.StandardMessageText,
                    ReasonForRejectionError = validationResult.Errors
                        .Select(errorMessage => new ReasonForRejectionError() { ReasonForRejectionErrorInner = errorMessage.ErrorMessage })
                        .ToList()
                };

                logger.LogInformation("Oтправляем FNA: {0}", string.Join("/", validationResult.Errors));
                await context.Publish<BuildMessage>(new { __CorrelationId = context.CorrelationId, Fna = fna });
            }
            else
            {
                Fbr fbr = context.Message.Fbr;
                string carrierCode = fbr.FlightInformation.FlightIdentification.CarrierCode;
                string flightNumber = fbr.FlightInformation.FlightIdentification.FlightNumber;
                string day = fbr.FlightInformation.FlightIdentification.Day;
                string month = fbr.FlightInformation.FlightIdentification.Month;

                DateTime date = this.GetFlightDate(day, month);

                string flightIdentifier = string.Concat(carrierCode, flightNumber);


                // Получаем список всех броней, на которые есть рейсы, попавшие в интервал
                List<Booking> bookings = this.dbContext.Bookings.AsNoTracking()
                    .Include(a => a.Awb)
                    .Include(i => i.FlightSchedule)
                    .Where(b => b.FlightSchedule.Number == flightIdentifier && b.FlightSchedule.FlightDate.Date == date)
                    .Where(b => b.SpaceAllocationCode == "KK")
                    .ToList();

                if (bookings.Any())
                {
                    bookings
                        .GroupBy(gb => gb.FlightScheduleId)
                        .ForEach(async gb =>
                        {
                            FlightShedule flight = gb.First().FlightSchedule;
                            flight.Bookings = gb.ToList();

                            Fbl fbl = mapper.Map<Fbl>(flight);
                            await context.Publish<BuildMessage>(new { __CorrelationId = Guid.NewGuid(), Carrier = fbl.FlightInformation.CarrierCode, Fbl = fbl, LinkedObjectId = flight.Id, CustomerId=55 });
                        });
                }
                else
                {
                    var flight = this.dbContext.FlightShedules.AsNoTracking().FirstOrDefault(f => f.Number == flightIdentifier && f.FlightDate.Date == date);
                    Fbl fbl = new Fbl
                    {
                        MessageIdentifier = "FBL",
                        FlightInformation = new IDeal.Common.Components.Messages.ObjectStructures.Fbls.Ver4.FlightInformation
                        {
                            AirportCode = flight.Origin,
                            CarrierCode = carrierCode,
                            FlightNumber = flightNumber,
                            Day = day,
                            Month = month
                        },
                        PortUnloading = new PortUnloading
                        {
                            AirportCode = flight.Destination,
                            NilCargoIndication = new NilCargoIndication { NilCargoCode = "NIL" }
                        },
                        ListCompleteIndicator = "LAST",
                    };
                    await context.Publish<BuildMessage>(new { __CorrelationId = Guid.NewGuid(), Carrier = fbl.FlightInformation.CarrierCode, Fbl = fbl, LinkedObjectId = flight.Id, CustomerId = 55 });
                }
            }
        }

        private DateTime GetFlightDate(string day, string month)
        {
            var dt = (day ?? "01") + (month ?? "JAN") + DateTime.Now.Year;
            return DateTime.ParseExact(dt, "ddMMMyyyy", new CultureInfo("en-US"));
        }
    }
}
