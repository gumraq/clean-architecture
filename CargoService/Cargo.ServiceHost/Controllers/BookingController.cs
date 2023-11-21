﻿using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using AutoMapper;
using MediatR;
using Cargo.Contract.Queries.Bookings;
using IDeal.Common.Version;
using Cargo.Contract.Commands;

namespace Cargo.ServiceHost.Controllers
{
    [Authorize]
    [Route("Api/[controller]/V1")]
    [ApiController]
    public class BookingController : ControllerBase
    {
        private readonly IMediator mediator;
        IMapper mapper;

        /// <summary>
        /// Работа с бронированием
        /// </summary>
        /// <param name="mediator"></param>
        public BookingController(IMediator mediator, IMapper mapper)
        {
            this.mediator = mediator;
            this.mapper = mapper;
        }
        
        /// <summary>
        ///  Информацмя о сервисе
        /// </summary>
        /// <returns>Информация о сервсе</returns>
        [HttpGet(nameof(Info))]
        public Task<IActionResult> Info()
        {
            return Task.FromResult<IActionResult>(base.Ok(ServiceVersion.GetJsonString()));
        }

        /// <summary>
        /// Получение списка изменений статусов
        /// </summary>
        /// <param name="query">Запрос</param>
        /// <returns>Список накладных</returns>
        [HttpGet(nameof(Tracking))]
        public async Task<IActionResult> Tracking([FromQuery] TrackingQuery query)
        {
            return base.Ok(await this.mediator.Send(query));
        }
        #region Awb        
        /// <summary>
        /// Получение объекта накладной
        /// </summary>
        /// <param name="query">запрос</param>
        /// <returns>Результат обновления: 1- выполнено 0- не выполнено</returns>
        [HttpGet(nameof(Awb))]
        public async Task<IActionResult> Awb([FromQuery] AwbQuery query)
        {
            var result = await this.mediator.Send(query);
            if (!result.IsValid)
            {
                return base.ValidationProblem(new ValidationProblemDetails(result.ToDictionary()));
            }
            else if (result.IsFailed)
            {
                return base.StatusCode(500, result);
            }
            return Ok(result.Value);
        }

        /// <summary>
        /// Получение списка накладных
        /// </summary>
        /// <param name="query">Запрос</param>
        /// <returns>Список накладных</returns>
        [HttpGet(nameof(Awbs))]
        public async Task<IActionResult> Awbs([FromQuery] AwbsQuery query)
        {
            return base.Ok(await this.mediator.Send(query));
        }        

        /// <summary>
        /// Резервирование номера накладной из пула
        /// </summary>
        /// <param name="command">Команда</param>
        [HttpPost(nameof(ReserveAwbNumber))]
        [MessageInjector]
        public async Task<IActionResult> ReserveAwbNumber([FromBody] ReserveAwbNumberCommand command)
        {
            var resultRequest = await this.mediator.Send(command);

            resultRequest.LogIfFailedOrInvalid();
            if (!resultRequest.IsValid)
            {
                return base.ValidationProblem(new ValidationProblemDetails(resultRequest.ToDictionary()));
            }
            else if (resultRequest.IsFailed)
            {
                return base.StatusCode(500, resultRequest);
            }

            var resultResponse = await this.mediator.Send(new AwbQuery { Id = resultRequest.Value });
            if (!resultResponse.IsValid)
            {
                return base.ValidationProblem(new ValidationProblemDetails(resultResponse.ToDictionary()));
            }
            else if (resultResponse.IsFailed)
            {
                return base.StatusCode(500, resultResponse);
            }
            return Ok(resultResponse.Value);
        }

        /// <summary>
        /// Сохранение накладной со всеми полями
        /// </summary>
        /// <param name="command">Команда</param>
        /// <returns>Сохраненная накладная</returns>
        [HttpPost(nameof(SaveAwb))]
        [MessageInjector]
        public async Task<IActionResult> SaveAwb([FromBody] SaveAwbCommand command)
        {
            var resultCmd = await this.mediator.Send(command);
            if (!resultCmd.IsValid)
            {
                return base.ValidationProblem(new ValidationProblemDetails(resultCmd.ToDictionary()));
            }
            else if (resultCmd.IsFailed)
            {
                return base.StatusCode(500, resultCmd);
            }

            var resultQry = await this.mediator.Send(new AwbQuery { Id = resultCmd.Value });
            if (!resultQry.IsValid)
            {
                return base.ValidationProblem(new ValidationProblemDetails(resultQry.ToDictionary()));
            }
            else if (resultQry.IsFailed)
            {
                return base.StatusCode(500, resultQry);
            }
            return Ok(resultQry.Value);
        }
        #endregion
        #region Bookings        

        /// <summary>
        /// Получение списка броней не KK
        /// </summary>
        /// <param name="query">Запрос</param>
        /// <returns>Список брони</returns>
        [HttpGet(nameof(BookingsSacNoKk))]
        [MessageInjector]
        public async Task<IActionResult> BookingsSacNoKk([FromQuery] BookingsSacNoKkQuery query)
        {
            return base.Ok(await this.mediator.Send(query));
        }

        /// <summary>
        /// Получение количетсво броней не KK
        /// </summary>
        /// <param name="query">Запрос</param>
        /// <returns>Количетсво броней</returns>
        [HttpGet(nameof(BookingsSacNoKkCount))]
        [MessageInjector]
        public async Task<IActionResult> BookingsSacNoKkCount([FromQuery] BookingsSacNoKkQueryCount query)
        {
            return base.Ok(await this.mediator.Send(query));
        }
        #endregion
    }
}
