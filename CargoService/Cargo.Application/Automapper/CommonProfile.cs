using AutoMapper;
using IDeal.Common.Components.Paginator;
using System.Threading.Tasks;
using IdealResults;

namespace Cargo.Application.Automapper
{
    public class CommonProfile : Profile
    {
        public CommonProfile()
        {
            this.CreateMap(typeof(PagedResult<>), typeof(PagedResult<>));
            this.CreateMap(typeof(SingleResult<>), typeof(SingleResult<>));
            this.CreateMap(typeof(Result<>), typeof(Result<>));
            this.CreateMap(typeof(Task<>), typeof(Task<>));
        }
    }
}
