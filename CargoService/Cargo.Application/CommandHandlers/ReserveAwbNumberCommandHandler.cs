using System.Threading;
using System.Threading.Tasks;
using IdealResults;
using Cargo.Application.Services;
using Cargo.Infrastructure.Data.Model;
using Cargo.Contract.Commands;

namespace Cargo.Application.CommandHandlers
{
    public class ReserveAwbNumberCommandHandler : ICommandHandler<ReserveAwbNumberCommand, Result<int>>
    {
        AwbService awbService;

        public ReserveAwbNumberCommandHandler(AwbService awbService)
        {
            this.awbService = awbService;
        }

        public async Task<Result<int>> Handle(ReserveAwbNumberCommand request, CancellationToken cancellationToken)
        {
            Result<AgentContractPoolAwbNums> reserveResult = await this.awbService.ReserveAsync(request.AgentId, request.AwbIdentifier);
            if (!reserveResult.IsSuccess)
            {
                return reserveResult.ToResult<int>();
            }

            var saveResult = await awbService.SaveAwbAsync(reserveResult.Value);
            if (!saveResult.IsSuccess)
            {
                return saveResult.ToResult<int>();
            }

            return saveResult.Value;
        }

    }
}