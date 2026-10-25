namespace Sentinel.Domain.Common;

/// <summary>
/// A failure that is part of the normal flow of the business — "that email is
/// already registered", "that role does not exist". Exceptions are reserved for
/// things that should never happen; errors are things that happen every day.
/// Not sealed: the Application layer derives a ValidationError that carries
/// one message per field.
/// </summary>
public record Error(string Code, string Message, ErrorType Type)
{
    public static Error Validation(string code, string message) => new(code, message, ErrorType.Validation);
    public static Error NotFound(string code, string message) => new(code, message, ErrorType.NotFound);
    public static Error Conflict(string code, string message) => new(code, message, ErrorType.Conflict);
    public static Error Forbidden(string code, string message) => new(code, message, ErrorType.Forbidden);

    public override string ToString() => $"{Code}: {Message}";
}
