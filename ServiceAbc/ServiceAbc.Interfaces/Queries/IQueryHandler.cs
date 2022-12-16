namespace ServiceAbc.Interfaces.Queries
{
    public interface IQueryHandler<in TRequest, TResponse> : MediatR.IRequestHandler<TRequest, TResponse> where TRequest : IQuery<TResponse>
    {

    }
}
