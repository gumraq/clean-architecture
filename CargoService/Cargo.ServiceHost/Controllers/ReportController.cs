using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Cargo.Contract.Queries.Report;

namespace Cargo.ServiceHost.Controllers
{
    [Authorize]
    [Route("Api/[controller]/V1")]
    [ApiController]
    public class ReportController : ControllerBase
    {
        private readonly ILogger<ReportController> logger;
        private readonly IMediator mediator;
      
        public ReportController(IMediator mediator, ILogger<ReportController> logger)
        {
            this.logger = logger;
            this.mediator = mediator;
        }

        /// <summary>
        /// Бронирование на рейсах
        /// </summary>
        /// <param name="query"></param>
        [HttpPost(nameof(GetReportBookingOnFlights))]
        public async Task<byte[]> GetReportBookingOnFlights([FromBody] ReportBookingOnFlightsQuery query)
        {
            return await this.mediator.Send(query);
        }

        /// <summary>
        /// Бронирования за период
        /// </summary>
        /// <param name="query"></param>
        [HttpPost(nameof(GetReportBookingsPerPeriod))]
        public async Task<byte[]> GetReportBookingsPerPeriod([FromBody] ReportBookingsPerPeriodQuery query)
        {
            return await this.mediator.Send(query);
        }

        /// <summary>
        /// Получить бланк FWB (в байтах) по ID накладной
        /// </summary>
        /// <param name="query"></param>
        [HttpPost(nameof(FwbBlankByAwbId))]
        public async Task<byte[]> FwbBlankByAwbId([FromBody] ReportFwbBlankByAwbIdQuery query)
        {
            return await this.mediator.Send(query);
        }
    }
}