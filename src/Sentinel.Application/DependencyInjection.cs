using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Sentinel.Application.Abstractions.Messaging;
using Sentinel.Application.Abstractions.Validation;
using Sentinel.Application.Behaviors;
using Sentinel.Application.Security;
using Sentinel.Domain.Security;

namespace Sentinel.Application;

public static class DependencyInjection
{
    /// <summary>
    /// Registers the dispatcher, the pipeline, and every handler and validator
    /// in this assembly. Adding a slice never means editing this file.
    /// </summary>
    public static IServiceCollection AddApplication(this IServiceCollection services, DetectionPolicy? detectionPolicy = null)
    {
        services.AddScoped<IDispatcher, Dispatcher>();

        // Order matters: logging wraps validation, so rejected requests are logged too.
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        var assembly = Assembly.GetExecutingAssembly();
        RegisterClosedImplementations(services, assembly, typeof(IRequestHandler<,>));
        RegisterClosedImplementations(services, assembly, typeof(IValidator<>));

        services.AddSingleton(detectionPolicy ?? DetectionPolicy.Default);
        services.AddScoped<AccountAssessor>();

        return services;
    }

    /// <summary>
    /// For every concrete class implementing a closed form of
    /// <paramref name="openInterface"/>, register it against that closed form.
    /// This is the twenty lines of assembly scanning that Scrutor would
    /// otherwise provide.
    /// </summary>
    private static void RegisterClosedImplementations(IServiceCollection services, Assembly assembly, Type openInterface)
    {
        var registrations =
            from type in assembly.GetTypes()
            where type is { IsClass: true, IsAbstract: false, IsGenericTypeDefinition: false }
            from implemented in type.GetInterfaces()
            where implemented.IsGenericType && implemented.GetGenericTypeDefinition() == openInterface
            select (Service: implemented, Implementation: type);

        foreach (var (service, implementation) in registrations)
            services.AddScoped(service, implementation);
    }
}
