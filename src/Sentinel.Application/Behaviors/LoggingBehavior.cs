using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Sentinel.Application.Abstractions.Messaging;
using Sentinel.Domain.Common;

namespace Sentinel.Application.Behaviors;

/// <summary>
/// Logs every request's name, outcome and duration. When a support ticket
/// arrives with nothing but a time and a user, this log line is usually the
/// first evidence you have (Chapter 21).
/// </summary>
internal sealed partial class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : Result
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var started = Stopwatch.GetTimestamp();

        var response = await next();

        var elapsedMs = Stopwatch.GetElapsedTime(started).TotalMilliseconds;
        if (response.IsSuccess)
            LogSucceeded(requestName, elapsedMs);
        else
            LogFailed(requestName, response.Error!.Code, response.Error.Message, elapsedMs);

        return response;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "{RequestName} succeeded in {ElapsedMs:0.0} ms")]
    private partial void LogSucceeded(string requestName, double elapsedMs);

    [LoggerMessage(Level = LogLevel.Warning, Message = "{RequestName} failed with {ErrorCode} ({ErrorMessage}) in {ElapsedMs:0.0} ms")]
    private partial void LogFailed(string requestName, string errorCode, string errorMessage, double elapsedMs);
}
