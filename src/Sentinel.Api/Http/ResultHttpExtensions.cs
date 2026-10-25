using Sentinel.Application.Abstractions.Validation;
using Sentinel.Domain.Common;

namespace Sentinel.Api.Http;

/// <summary>
/// The single place where a business outcome becomes an HTTP response.
/// Every endpoint ends in one of these calls, so the mapping from
/// <see cref="ErrorType"/> to status code cannot differ between endpoints.
/// </summary>
public static class ResultHttpExtensions
{
    public static IResult ToNoContent(this Result result) =>
        result.IsSuccess ? TypedResults.NoContent() : result.Error!.ToProblem();

    public static IResult ToOk<T>(this Result<T> result) =>
        result.IsSuccess ? TypedResults.Ok(result.Value) : result.Error!.ToProblem();

    public static IResult ToCreated(this Result<Guid> result, Func<Guid, string> location) =>
        result.IsSuccess
            ? TypedResults.Created(location(result.Value), new Sentinel.Contracts.CreatedResponse(result.Value))
            : result.Error!.ToProblem();

    public static IResult ToProblem(this Error error)
    {
        if (error is ValidationError validation)
        {
            var errors = validation.Failures
                .GroupBy(f => f.Property)
                .ToDictionary(g => g.Key, g => g.Select(f => f.Message).ToArray());

            return TypedResults.ValidationProblem(errors, title: validation.Message, type: validation.Code);
        }

        var status = error.Type switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            ErrorType.Forbidden => StatusCodes.Status403Forbidden,
            _ => StatusCodes.Status500InternalServerError
        };

        return TypedResults.Problem(
            statusCode: status,
            title: error.Code,
            detail: error.Message,
            extensions: new Dictionary<string, object?> { ["code"] = error.Code });
    }
}
