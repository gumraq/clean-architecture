using AutoMapper;
using IdealResults;
using IDeal.Common.Components.Paginator;
using Cargo.Contract.DTOs;
using Cargo.Contract.DTOs.Bookings;
using Cargo.Contract.Queries.Bookings;
using Cargo.Infrastructure.Data;
using Cargo.Infrastructure.Data.Model;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Cargo.Application.QueryHandlers.Bookings
{
    public class BookingsSacNoKkQueryCountHandler : IQueryHandler<BookingsSacNoKkQueryCount, Result<int>>
    {
        CargoContext CargoContext;

        public BookingsSacNoKkQueryCountHandler(CargoContext CargoContext)
        {
            this.CargoContext = CargoContext;
        }

        public async Task<Result<int>> Handle(BookingsSacNoKkQueryCount request, CancellationToken cancellationToken)
        {
            Task<Result<int>> task = Task.Run(() =>
            {
                IQueryable<Booking> query = this.CargoContext.Bookings
                .Include(b => b.FlightSchedule)
                .Include(b => b.Awb);
                bool isCarrier = false;

                if (request.AgentId != null)
                {
                    var contrag = this.CargoContext.Contragents.Include(c => c.Carrier).AsNoTracking().FirstOrDefault(c => c.Id == request.AgentId);
                    if (contrag?.Carrier != null)
                    {
                        int[] ids = this.CargoContext.Agents.AsNoTracking().Where(a => a.CarrierId == 55).Select(a => a.ContragentId).ToArray();
                        isCarrier = true;
                        query = query.Where(a => a.Awb.AgentId.HasValue && ids.Contains(a.Awb.AgentId.Value));
                    }
                }

                if (!isCarrier)
                {
                    query = query.Where(a => request.AgentId != null ? a.Awb.AgentId == request.AgentId : true);
                }

                var bookings = query
                .Where(b => request.FlightId != null ? b.FlightScheduleId == (ulong)request.FlightId : true)
                .Where(b => string.IsNullOrEmpty(request.AwbIdentification) ? true : EF.Functions.Like(string.Concat(b.Awb.AcPrefix, "-", b.Awb.SerialNumber), $"{request.AwbIdentification}%"))
                .Where(b => string.IsNullOrEmpty(request.Product) ? true : EF.Functions.Like(string.Concat(b.Awb.Product), $"{request.Product}%"))
                .Where(b => string.IsNullOrEmpty(request.AwbOrigin) ? true : request.AwbOrigin == b.Awb.Origin)
                .Where(b => string.IsNullOrEmpty(request.AwbDestination) ? true : request.AwbDestination == b.Awb.Destination)
                .Where(b => request.AwbCreateAt.HasValue ? request.AwbCreateAt <= b.Awb.CreatedDate : true)
                .Where(b => request.AwbCreateTo.HasValue ? request.AwbCreateTo >= b.Awb.CreatedDate : true)
                .Where(b => b.SpaceAllocationCode != "KK")
                .AsNoTracking()
                .Count();
                
                Result<int> result = Result.Ok(bookings);
                return result;
            });
            return await task;
        }
    }
}
