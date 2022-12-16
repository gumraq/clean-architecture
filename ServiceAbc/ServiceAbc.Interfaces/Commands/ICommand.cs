namespace ServiceAbc.Interfaces.Commands
{
    public interface ICommand<out TResponse> : MediatR.IRequest<TResponse>
    { }
}
