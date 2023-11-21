using AutoMapper;
using Cargo.Contract.DTOs;
using Cargo.Infrastructure.Data.Model.Dictionary;
using Cargo.Infrastructure.Data.Model.Dictionary.Settings;
using Cargo.Infrastructure.Data.Model.Rates;

namespace Cargo.Application.Automapper
{
    public class RatesProfile : Profile
    {
        public RatesProfile()
        {
            CreateMap<TariffGroup, TariffGroup>();
            CreateMap<TariffGroupDto, TariffGroup>().ReverseMap();

            CreateMap<TariffSolution, TariffSolution>()
                .ForMember(x => x.AwbOriginTariffGroup, opt => opt.Ignore())
                .ForMember(x => x.AwbDestinationTariffGroup, opt => opt.Ignore())
                .ForMember(x => x.RateGrid, opt => opt.Ignore())
                .ForMember(x => x.Addons, opt => opt.Ignore());

            CreateMap<CarrierCharge, CarrierCharge>()
                 .ForMember(x => x.CarrierChargeBindings, opt => opt.Ignore());
        }
    }
}