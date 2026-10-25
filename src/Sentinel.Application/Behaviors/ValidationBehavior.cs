using Sentinel.Application.Abstractions.Messaging;
using Sentinel.Application.Abstractions.Validation;
using Sentinel.Domain.Common;

namespace Sentinel.Application.Behaviors;

/// <summary>
/// Runs every validator registered for the request and short-circuits with a
/// <see cref="ValidationError"/> if any of them complain. The handler is
/// never called with a malformed request, so it never has to check.
/// </summary>
internal sealed class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : Result
{
    public Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var failures = validators
            .SelectMany(v => v.Validate(request))
            .ToList();

        return failures.Count == 0
            ? next()
            : Task.FromResult(ResultFactory.Failure<TResponse>(new ValidationError(failures)));
    }
}
