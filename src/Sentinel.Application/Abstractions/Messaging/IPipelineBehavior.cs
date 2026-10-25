using Sentinel.Domain.Common;

namespace Sentinel.Application.Abstractions.Messaging;

/// <summary>Calls the next behaviour in the pipeline, or the handler itself at the end.</summary>
public delegate Task<TResponse> RequestHandlerDelegate<TResponse>();

/// <summary>
/// Code that wraps every handler: validation, logging, timing. Behaviours are
/// the Decorator pattern applied by the dispatcher, which is how cross-cutting
/// concerns stay out of the handlers (the S and O of SOLID in one interface).
/// </summary>
public interface IPipelineBehavior<in TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : Result
{
    Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken);
}
