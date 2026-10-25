using Sentinel.Domain.Common;

namespace Sentinel.Application.Abstractions.Messaging;

/// <summary>
/// Anything the dispatcher can send. Every response is a <see cref="Result"/>,
/// so a caller always has to look at the outcome before it can use a value.
/// </summary>
public interface IRequest<TResponse> where TResponse : Result;

/// <summary>Changes state and returns nothing but success or failure.</summary>
public interface ICommand : IRequest<Result>;

/// <summary>Changes state and returns a value, typically the id of something created.</summary>
public interface ICommand<TValue> : IRequest<Result<TValue>>;

/// <summary>Reads state. A query must never change anything.</summary>
public interface IQuery<TValue> : IRequest<Result<TValue>>;
