namespace ServiceAbc.Interfaces.Queries
{
    /// <summary>Запрос. Маркер интерфейс</summary>
    /// <typeparam name="TResponse"></typeparam>
    public interface IQuery<TResponse> : MediatR.IRequest<TResponse>
    {
    }
}
