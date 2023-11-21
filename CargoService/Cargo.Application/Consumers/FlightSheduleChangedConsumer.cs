using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MassTransit;
using Cargo.Infrastructure.Data;
using Cargo.Infrastructure.Data.Model;
using AutoMapper;
using IDeal.Common.Messaging.Shedule;

namespace Cargo.Application.CommandHandlers
{
    public class FlightSheduleChangedConsumer : IConsumer<FlightSheduleChanged>
    {
        ILogger<FlightSheduleChangedConsumer> logger;
        CargoContext dbContext;
        IMapper mapper;

        public FlightSheduleChangedConsumer(ILogger<FlightSheduleChangedConsumer> logger, CargoContext dbContext, IMapper mapper)
        {
            this.logger = logger;
            this.dbContext = dbContext;
            this.mapper = mapper;
        }

        public async Task Consume(ConsumeContext<FlightSheduleChanged> context)
        {
            if (context.Message?.Id == null)
            {
                throw new ArgumentNullException("Пропущен параметр для определения рейса");
            }
            FlightShedule flight = this.dbContext.FlightShedules.Find(context.Message.Id);

            bool isNew = flight == null;

            flight = this.mapper.Map(context.Message, flight);

            if (isNew)
            {
                this.dbContext.FlightShedules.Add(flight);
            }

            await this.dbContext.SaveChangesAsync();
        }
    }
}
