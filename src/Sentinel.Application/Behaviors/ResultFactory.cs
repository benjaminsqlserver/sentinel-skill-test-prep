using System.Collections.Concurrent;
using System.Reflection;
using Sentinel.Domain.Common;

namespace Sentinel.Application.Behaviors;

/// <summary>
/// Builds a failed <typeparamref name="TResponse"/> when all a behaviour
/// knows is that TResponse is "some kind of Result". For <c>Result</c> that
/// is a direct call; for <c>Result&lt;T&gt;</c> the T is only known at run
/// time, so the factory method is found by reflection once and cached.
/// </summary>
internal static class ResultFactory
{
    private static readonly ConcurrentDictionary<Type, Func<Error, Result>> Factories = new();

    public static TResponse Failure<TResponse>(Error error) where TResponse : Result
    {
        if (typeof(TResponse) == typeof(Result))
            return (TResponse)Result.Failure(error);

        var factory = Factories.GetOrAdd(typeof(TResponse), static type =>
        {
            // DeclaredOnly: Result<T>.Failure hides Result.Failure, and we want the one on Result<T>.
            var method = type.GetMethod(
                nameof(Result.Failure),
                BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly,
                [typeof(Error)])
                ?? throw new InvalidOperationException($"{type} has no static Failure(Error) method.");

            return method.CreateDelegate<Func<Error, Result>>();
        });

        return (TResponse)factory(error);
    }
}
