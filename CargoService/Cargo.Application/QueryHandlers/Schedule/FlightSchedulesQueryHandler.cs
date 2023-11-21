using Cargo.Contract.Queries;
using IDeal.Common.Components.Paginator;
using Cargo.Infrastructure.Data;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using Cargo.Contract.DTOs;
using System;
using System.Collections.Generic;
using MoreLinq;
using Cargo.Contract.Queries.Schedule;
using Cargo.Application.Services;

namespace Cargo.Application.QueryHandlers.Schedule
{
    public class FlightSchedulesQueryHandler : IQueryHandler<FlightSchedulesQuery, PagedResult<FlightSheduleDto>>
    {
        CargoContext CargoContext;
        IMapper mapper;
        SettingsService commPayloaderService;

        public FlightSchedulesQueryHandler(CargoContext CargoContext, IMapper mapper, SettingsService commPayloaderService)
        {
            this.CargoContext = CargoContext;
            this.mapper = mapper;
            this.commPayloaderService = commPayloaderService;
        }

        public async Task<PagedResult<FlightSheduleDto>> Handle(FlightSchedulesQuery request, CancellationToken cancellationToken)
        {
                CommPayloadFlatDetail[] pkzs = (await this.commPayloaderService.Payloads()).Value.ToArray();
                var page = this.CargoContext
                .FlightShedules
                .Where(f => !string.IsNullOrEmpty(request.CarrierCode) ? f.Number.Substring(0, 2) == request.CarrierCode : true)
                .Where(f => !string.IsNullOrEmpty(request.Destination) ? f.Destination == request.Destination : true)
                .Where(f => !string.IsNullOrEmpty(request.Origin) ? f.Origin == request.Origin : true)
                .Where(f => !string.IsNullOrEmpty(request.FlightNumber) ? f.Number.Contains(request.FlightNumber) : true)
                .Where(f => request.FlightDate.HasValue && request.FlightDate != DateTime.MinValue ? f.FlightDate == request.FlightDate : true)
                .Where(f => request.DateFrom.HasValue && request.DateFrom != DateTime.MinValue ? f.FlightDate >= request.DateFrom : true)
                .Where(f => request.DateTo.HasValue && request.DateTo != DateTime.MaxValue ? f.FlightDate <= request.DateTo : true)
                .OrderBy(f => f.StOrigin)
                .AsNoTracking()
                .Page(new PageInfo {PageIndex = request.PageIndex ??0, PageSize = request.PageSize ?? 20 });

                ulong[] fids = page.Items.Select(f => f.Id).ToArray();
                var bookings = this.CargoContext.Bookings.Where(b => fids.Contains(b.FlightScheduleId)).ToList();

                var pageDto = this.mapper.Map<PagedResult<FlightSheduleDto>>(page);

                pageDto.Items
                .Join(CargoContext.IataLocations, fs=>fs.Origin, il=>il.Code,(fs,il)=> { fs.StOriginLocal = fs.StOrigin?.Add(il.TimeZone);return fs; })
                .Join(CargoContext.IataLocations, fs => fs.Destination, il => il.Code, (fs, il) => { fs.StDestinationLocal = fs.StDestination?.Add(il.TimeZone); return fs; })
                .Select(fs =>
                {
                    fs.StdOriginLocalDayChange = 0;
                    fs.StdOriginUtcDayChange = (fs.StOrigin?.Date - fs.FlightDate?.Date)?.Days;
                    fs.StaDestLocalDayChange = (fs.StDestinationLocal - fs.FlightDate?.Date)?.Days;
                    fs.StaDestUtcDayChange = (fs.StDestination?.Date - fs.FlightDate?.Date)?.Days;
                    return fs;
                })
                .GroupJoin(pkzs, fs => fs.AircraftType, cpl => cpl.AircraftType, (fs, cpls) => { fs.CommPayloadInfo = new FlightCommPayloadInfoDto { FlightId = fs.Id, DateFlight = fs.FlightDate, FlightShedule = fs, NumberFlight = fs.Number, WeightPlan = ((decimal?)fs.PayloadWeight) ?? cpls.FirstOrDefault()?.Weight ?? 0, VolumePlan = ((decimal?)fs.PayloadVolume) ?? cpls.FirstOrDefault()?.Volume ?? 0 }; return fs; })
                .GroupJoin(bookings, f=>f.Id,b=>b.FlightScheduleId,(fs,bs)=>
                {
                    fs.CommPayloadInfo.VolumeFact = bs.Where(b=>b.SpaceAllocationCode=="KK").Sum(b => b.VolumeAmount);
                    fs.CommPayloadInfo.WeightFact = bs.Where(b => b.SpaceAllocationCode == "KK").Sum(b => b.Weight);
                    fs.CommPayloadInfo.VolumeRemain = fs.CommPayloadInfo.VolumePlan - fs.CommPayloadInfo.VolumeFact;
                    fs.CommPayloadInfo.WeightRemain = fs.CommPayloadInfo.WeightPlan - fs.CommPayloadInfo.WeightFact;
                    return fs;
                }).ToList();

                return pageDto;
        }
    }
}
