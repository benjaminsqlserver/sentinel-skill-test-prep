using Sentinel.Domain.Common;

namespace Sentinel.Application.Abstractions.Validation;

public sealed record ValidationFailure(string Property, string Message);

/// <summary>
/// An <see cref="Error"/> carrying one message per offending field, which the
/// API turns into an RFC 9457 validation problem response.
/// </summary>
public sealed record ValidationError(IReadOnlyList<ValidationFailure> Failures)
    : Error("Validation.Failed", "One or more fields are invalid.", ErrorType.Validation);

/// <summary>
/// Checks the <i>shape</i> of a request: required fields, lengths, ranges.
/// Rules that need the database ("is this email taken?") belong in the
/// handler; rules about the business belong in the domain.
/// </summary>
public interface IValidator<in TRequest>
{
    IEnumerable<ValidationFailure> Validate(TRequest request);
}

/// <summary>
/// A tiny base class in place of FluentValidation. It is not a framework:
/// it is three helpers that make a validator read like a checklist.
/// </summary>
public abstract class Validator<TRequest> : IValidator<TRequest>
{
    public abstract IEnumerable<ValidationFailure> Validate(TRequest request);

    protected static IEnumerable<ValidationFailure> Required(string? value, string property)
    {
        if (string.IsNullOrWhiteSpace(value))
            yield return new ValidationFailure(property, $"{property} is required.");
    }

    protected static IEnumerable<ValidationFailure> MaxLength(string? value, int max, string property)
    {
        if (value is not null && value.Trim().Length > max)
            yield return new ValidationFailure(property, $"{property} cannot exceed {max} characters.");
    }

    protected static IEnumerable<ValidationFailure> InRange(int value, int min, int max, string property)
    {
        if (value < min || value > max)
            yield return new ValidationFailure(property, $"{property} must be between {min} and {max}.");
    }
}
