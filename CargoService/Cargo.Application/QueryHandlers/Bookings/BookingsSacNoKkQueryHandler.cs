using Cargo.Infrastructure.Data;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using System.Linq;
using Cargo.Infrastructure.Data.Model;
using IDeal.Common.Components.Paginator;
using Microsoft.EntityFrameworkCore;
using Cargo.Contract.Queries.Bookings;
using Cargo.Contract.DTOs.Bookings;

namespace Cargo.Application.QueryHandlers.Bookings
{
    public class BookingsSacNoKkQueryHandler : IQueryHandler<BookingsSacNoKkQuery, PagedResult<AwbLightDto>>
    {
        CargoContext CargoContext;

        IMapper mapper;

        public BookingsSacNoKkQueryHandler(CargoContext CargoContext, IMapper mapper)
        {
            this.CargoContext = CargoContext;
            this.mapper = mapper;
        }

        public async Task<PagedResult<AwbLightDto>> Handle(BookingsSacNoKkQuery request, CancellationToken cancellationToken)
        {
            Task<PagedResult<Booking>> task = Task.Run(() =>
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
                        .Include(b => b.FlightSchedule)
                    .Include(b => b.Awb)
                    .Where(b => request.FlightId != null ? b.FlightScheduleId == (ulong)request.FlightId : true)
                    .Where(b => string.IsNullOrEmpty(request.AwbIdentification) ? true : EF.Functions.Like(string.Concat(b.Awb.AcPrefix, "-", b.Awb.SerialNumber), $"{request.AwbIdentification}%"))
                    .Where(b => string.IsNullOrEmpty(request.Product) ? true : EF.Functions.Like(string.Concat(b.Awb.Product), $"{request.Product}%"))
                    .Where(b => string.IsNullOrEmpty(request.AwbOrigin) ? true : request.AwbOrigin == b.Awb.Origin)
                    .Where(b => string.IsNullOrEmpty(request.AwbDestination) ? true : request.AwbDestination == b.Awb.Destination)
                    .Where(b => request.AwbCreateAt.HasValue ? request.AwbCreateAt <= b.Awb.CreatedDate : true)
                    .Where(b => request.AwbCreateTo.HasValue ? request.AwbCreateTo >= b.Awb.CreatedDate : true)
                    .Where(b => b.SpaceAllocationCode != "KK");

                    if (string.IsNullOrEmpty(request.OrderBy))
                    {
                        bookings = bookings.OrderByDescending(b => b.FlightSchedule.FlightDate);
                    }
                    else
                    {
                        bookings = bookings.OrderBy(request.OrderBy, request.Desc ?? false);
                    }

                    return bookings.Page(new PageInfo { PageIndex = request.PageIndex ?? 0, PageSize = request.PageSize ?? 20 });
                }, cancellationToken);
            return await this.mapper.Map<Task<PagedResult<AwbLightDto>>>(task);
        }
    }
}
