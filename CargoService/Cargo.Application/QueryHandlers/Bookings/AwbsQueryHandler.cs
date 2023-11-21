using Cargo.Infrastructure.Data;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using System.Linq;
using Cargo.Infrastructure.Data.Model;
using IDeal.Common.Components.Paginator;
using Microsoft.EntityFrameworkCore;
using Cargo.Contract.Queries.Bookings;
using System;
using Cargo.Contract.DTOs.Bookings;

namespace Cargo.Application.QueryHandlers.Bookings
{
    public class AwbsQueryHandler : IQueryHandler<AwbsQuery, PagedResult<AwbLightDto>>
    {
        CargoContext CargoContext;
        IMapper mapper;

        public AwbsQueryHandler(CargoContext CargoContext, IMapper mapper)
        {
            this.CargoContext = CargoContext;
            this.mapper = mapper;
        }

        public async Task<PagedResult<AwbLightDto>> Handle(AwbsQuery request, CancellationToken cancellationToken)
        {
            Task<PagedResult<Awb>> task = Task.Run(() =>
            {
                IQueryable<Awb> query = this.CargoContext.Awbs;
                bool isCarrier = false;
                
                if (request.AgentId != null)
                {
                    var contrag = this.CargoContext.Contragents.Include(c=>c.Carrier).AsNoTracking().FirstOrDefault(c => c.Id == request.AgentId);
                    if (contrag?.Carrier != null)
                    {
                        int[] ids = this.CargoContext.Agents.AsNoTracking().Where(a=>a.CarrierId == 55).Select(a=>a.ContragentId).ToArray();
                        isCarrier = true;
                        query = query.Where(a => a.AgentId.HasValue && ids.Contains(a.AgentId.Value));
                    }
                }

                if (!isCarrier)
                {
                    query = query.Where(a => request.AgentId != null ? a.AgentId == request.AgentId : true);
                }

                var awbs = query
                .Where(a => request.FlightId != null ? a.Bookings.Any(b => b.FlightScheduleId == (ulong)request.FlightId) : true)
                .Where(a => string.IsNullOrEmpty(request.AwbIdentification) ? true : EF.Functions.Like(string.Concat(a.AcPrefix, "-", a.SerialNumber), $"{request.AwbIdentification}%"))
                .Where(a => string.IsNullOrEmpty(request.Product) ? true : EF.Functions.Like(string.Concat(a.Product), $"{request.Product}%"))
                .Where(a => string.IsNullOrEmpty(request.AwbOrigin) ? true : request.AwbOrigin == a.Origin)
                .Where(a => string.IsNullOrEmpty(request.AwbDestination) ? true : request.AwbDestination == a.Destination)
                .Where(a => request.AwbCreateAt.HasValue ? request.AwbCreateAt <= a.CreatedDate : true)
                .Where(a => request.AwbCreateTo.HasValue ? request.AwbCreateTo >= a.CreatedDate : true)
                .OrderByDescending(a=>a.CreatedDate)
                .Page(new PageInfo { PageIndex = request.PageIndex ?? 0, PageSize = request.PageSize ?? 20 });

                int[] awbIds = awbs.Items.Select(item => item.Id).ToArray();

                var bookings = this.CargoContext.Bookings.Include(b=>b.FlightSchedule)
                .Where(b => awbIds.Contains(b.AwbId));
                awbs.Items.GroupJoin(bookings, a => a.Id, b => b.AwbId, (a, bs) => { a.Bookings = bs.OrderByDescending(b=>b.FlightSchedule?.FlightDate).ToList(); return a; }).ToList();


                return awbs;
            }, cancellationToken);
            return await this.mapper.Map<Task<PagedResult<AwbLightDto>>>(task);
        }
    }
}
