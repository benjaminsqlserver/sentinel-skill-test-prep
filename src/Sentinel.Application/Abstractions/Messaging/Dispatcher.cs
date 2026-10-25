using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Sentinel.Domain.Common;

namespace Sentinel.Application.Abstractions.Messaging;

/// <summary>Sends a request to its single handler, through the pipeline.</summary>
public interface IDispatcher
{
    Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        where TResponse : Result;
}

/// <summary>
/// Our replacement for MediatR's <c>ISender</c>. The only clever part is the
/// wrapper: <see cref="Send{TResponse}"/> knows the response type statically
/// but the request type only at run time, and DI needs both to resolve
/// <c>IRequestHandler&lt;TRequest, TResponse&gt;</c>. A closed generic wrapper,
/// built once per request type with reflection and cached, bridges the gap so
/// that every call after the first is ordinary, strongly-typed code.
/// </summary>
public sealed class Dispatcher(IServiceProvider services) : IDispatcher
{
    private static readonly ConcurrentDictionary<Type, object> Wrappers = new();

    public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        where TResponse : Result
    {
        ArgumentNullException.ThrowIfNull(request);

        var wrapper = (RequestHandlerWrapper<TResponse>)Wrappers.GetOrAdd(
            request.GetType(),
            requestType => Activator.CreateInstance(
                typeof(RequestHandlerWrapper<,>).MakeGenericType(requestType, typeof(TResponse)))!);

        return wrapper.Handle(request, services, cancellationToken);
    }
}

internal abstract class RequestHandlerWrapper<TResponse> where TResponse : Result
{
    public abstract Task<TResponse> Handle(IRequest<TResponse> request, IServiceProvider services, CancellationToken cancellationToken);
}

internal sealed class RequestHandlerWrapper<TRequest, TResponse> : RequestHandlerWrapper<TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : Result
{
    public override Task<TResponse> Handle(IRequest<TResponse> request, IServiceProvider services, CancellationToken cancellationToken)
    {
        var typedRequest = (TRequest)request;
        var handler = services.GetRequiredService<IRequestHandler<TRequest, TResponse>>();

        RequestHandlerDelegate<TResponse> pipeline = () => handler.Handle(typedRequest, cancellationToken);

        // Registered first runs outermost: wrap in reverse so that
        // AddBehavior(A); AddBehavior(B) gives A(B(handler)).
        foreach (var behavior in services.GetServices<IPipelineBehavior<TRequest, TResponse>>().Reverse())
        {
            var next = pipeline;
            pipeline = () => behavior.Handle(typedRequest, next, cancellationToken);
        }

        return pipeline();
    }
}
