# Chapter 9 · The Application Layer

The Application layer turns requests into calls on the domain. Its
machinery (messages, handlers, a dispatcher, a pipeline) is small, and
writing it yourself is the fastest way to understand what MediatR and its
relatives do.

## 9.1 The messages

<!-- include: src/Sentinel.Application/Abstractions/Messaging/Requests.cs -->

Every request declares its response type, and every response is a
`Result`. The compiler therefore knows that
`dispatcher.Send(new GetUserByIdQuery(id))` returns `Result<UserDetailDto>`;
no casts anywhere.

<!-- include: src/Sentinel.Application/Abstractions/Messaging/Handlers.cs -->

The dispatcher only knows `IRequestHandler<TRequest, TResponse>`. The three
specialised interfaces exist for the reader: a class that implements
`IQueryHandler<,>` tells you at a glance that it must not change state.

## 9.2 The pipeline

<!-- include: src/Sentinel.Application/Abstractions/Messaging/IPipelineBehavior.cs -->

A behaviour receives the request and a delegate for "the rest of the
pipeline". It can act before, after, or instead of calling it. That is the
Decorator pattern, applied uniformly to every handler.

## 9.3 The dispatcher

<!-- include: src/Sentinel.Application/Abstractions/Messaging/Dispatcher.cs -->

The interesting problem is generic type resolution. `Send<TResponse>` knows
the response type at compile time, but the concrete request type
(`GetUserByIdQuery`) only at run time, and DI needs both to resolve
`IRequestHandler<GetUserByIdQuery, Result<UserDetailDto>>`. The solution:

1. On the first `Send` for a request type, build the closed generic type
   `RequestHandlerWrapper<GetUserByIdQuery, Result<UserDetailDto>>` with
   `MakeGenericType` and create one instance.
2. Cache it in a `ConcurrentDictionary` keyed by request type.
3. From then on, call it through the abstract base
   `RequestHandlerWrapper<TResponse>`, whose `Handle` is ordinary,
   strongly-typed code.

Reflection runs once per request type for the life of the process.

Behaviours are wrapped in reverse registration order so that the first one
registered runs outermost: register logging then validation, and a rejected
request is still logged.

## 9.4 Validation without a library

<!-- include: src/Sentinel.Application/Abstractions/Validation/Validation.cs -->

FluentValidation is excellent; it is also a dependency for something that,
in this application, needs three helpers. Writing them shows the boundary
that matters more than the library:

| Kind of rule | Example | Where it lives |
|---|---|---|
| Shape | "Email is required", "PageSize between 1 and 100" | Validator |
| Needs the database | "That email is already registered" | Handler |
| Business invariant | "A role cannot be granted twice" | Domain |

A validator that queries the database, or a handler that checks required
fields, is a sign the layers have blurred.

## 9.5 The behaviours

<!-- include: src/Sentinel.Application/Behaviors/ValidationBehavior.cs -->

When validation fails, the behaviour must return a failed `TResponse`
without knowing what `TResponse` is beyond "some `Result`":

<!-- include: src/Sentinel.Application/Behaviors/ResultFactory.cs -->

`BindingFlags.DeclaredOnly` matters: `Result<T>` declares its own static
`Failure` that hides the base one, and we want the one that returns
`Result<T>`.

<!-- include: src/Sentinel.Application/Behaviors/LoggingBehavior.cs -->

`[LoggerMessage]` generates the logging code at compile time: no boxing, no
parsing the template on every call, and structured properties
(`RequestName`, `ErrorCode`) that you can filter on. When a ticket arrives
with nothing but "it failed around 10 this morning", these lines are your
first evidence (Chapter 21).

## 9.6 The ports

<!-- include: src/Sentinel.Application/Abstractions/Ports/Ports.cs -->

`IClock` deserves a word. A handler that calls `DateTime.UtcNow` directly
cannot be tested at "thirty-one days later", and every time-based rule in
this system (reporting windows, clock skew, relative times) needs exactly
that. Every "now" in Sentinel comes from an `IClock`, on the server, and
from a `Clock` service in Angular.

## 9.7 Registering everything

<!-- include: src/Sentinel.Application/DependencyInjection.cs -->

Adding a slice never touches this file: the scan finds every closed
implementation of `IRequestHandler<,>` and `IValidator<>`. That is the
open/closed principle doing visible work.
