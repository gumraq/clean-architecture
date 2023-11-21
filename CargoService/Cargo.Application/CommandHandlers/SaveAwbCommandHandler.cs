using System;
using System.Linq;
using AutoMapper;
using System.Threading;
using System.Threading.Tasks;
using Cargo.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using IdealResults;
using Cargo.Infrastructure.Data.Model;
using MoreLinq;
using MassTransit;

using IDeal.Common.Components;
using Cargo.Application.Services;
using Cargo.Contract.Commands;

namespace Cargo.Application.CommandHandlers
{
    public class SaveAwbCommandHandler : ICommandHandler<SaveAwbCommand, Result<int>>
    {
        IMapper mapper;
        AwbService awbService;

        public SaveAwbCommandHandler(IMapper mapper, AwbService awbService)
        {
            this.mapper = mapper;
            this.awbService = awbService;
        }

        public async Task<Result<int>> Handle(SaveAwbCommand request, CancellationToken cancellationToken)
        {
            Result<Awb> trackedResult = awbService.TrackedAwb(request.Awb.Id);

            if (!trackedResult.IsSuccess)
            {
                return trackedResult.ToResult<int>();
            }

            mapper.Map(request.Awb, trackedResult.Value);
            Result<int> saveResult = await awbService.SaveAwb(trackedResult.Value, request.Status);

            if (!saveResult.IsSuccess)
            {
                return saveResult.ToResult<int>();
            }
            return Result.Ok(saveResult.Value);
        }
    }
}
