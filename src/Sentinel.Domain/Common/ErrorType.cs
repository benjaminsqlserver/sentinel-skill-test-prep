namespace Sentinel.Domain.Common;

/// <summary>
/// The category of a failure. The API layer maps each category to exactly one
/// HTTP status code, so adding a member here is an API change.
/// </summary>
public enum ErrorType
{
    Validation,
    NotFound,
    Conflict,
    Forbidden
}
