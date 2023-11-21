﻿using Cargo.Contract.Commands;
using Cargo.Contract.Queries.Schedule;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using IdealResults;
using IDeal.Common.Components;

namespace Cargo.ServiceHost.Controllers
{
    [Authorize]
    [Route("Api/[controller]/V1")]
    [ApiController]
    public class FlightScheduleController : ControllerBase
    {
        private readonly ILogger<FlightScheduleController> logger;
        private readonly IMediator mediator;

        /// <summary>
        /// Рейсы в управлении
        /// </summary>
        /// <param name="mediator"></param>
        /// <param name="logger"></param>
        public FlightScheduleController(IMediator mediator, ILogger<FlightScheduleController> logger)
        {
            this.logger = logger;
            this.mediator = mediator;
        }

        /// <summary>
        /// Статус продаж на рейсе.
        /// </summary>
        /// <param name="command"></param>
        [HttpPost(nameof(SaleState))]
        public async Task<Result<FlightSaleState>> SaleState([FromBody] SaleStateCommand command)
        {
            return await mediator.Send(command);
        }

        /// <summary>
        /// Список рейсов из расписания с фильтром
        /// </summary>
        /// <returns>Список рейсов из расписания</returns>
        [HttpGet(nameof(FlightScheduleWithFilter))]
        public async Task<IActionResult> FlightScheduleWithFilter([FromQuery] FlightSchedulesQuery query)
        {
            return base.Ok(await this.mediator.Send(query));
        }
    }
}
