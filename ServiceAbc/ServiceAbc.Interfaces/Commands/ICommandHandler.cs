namespace ServiceAbc.Interfaces.Commands
{
    public interface ICommandHandler<in TRequest, TResponse> : MediatR.IRequestHandler<TRequest, TResponse> where TRequest : ICommand<TResponse>
    {

    }
}
