using FluentValidation;
using Cargo.Contract.Commands;

namespace Cargo.Application.Validation
{
    public class ReserveAwbNumberCommandValidator : AbstractValidator<ReserveAwbNumberCommand>
    {
        public ReserveAwbNumberCommandValidator()
        {
            RuleFor(command => command.AgentId).GreaterThan(0);
            RuleFor(command => command.AwbIdentifier).Matches(@"^\d{3}-\d{8}$");
        }
    }
}
