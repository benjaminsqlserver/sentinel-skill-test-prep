using System.Reflection;
using Sentinel.Application.Abstractions.Messaging;

namespace Sentinel.Architecture.Tests;

/// <summary>
/// The dependency rule, enforced by the build. A diagram in a wiki can be
/// ignored; a red test cannot. No NetArchTest package: reading referenced
/// assemblies and walking types is all it takes.
/// </summary>
public class DependencyRuleTests
{
    private static readonly Assembly Domain = typeof(Sentinel.Domain.Common.Result).Assembly;
    private static readonly Assembly Contracts = typeof(Sentinel.Contracts.PageDto).Assembly;
    private static readonly Assembly Application = typeof(IDispatcher).Assembly;
    private static readonly Assembly Infrastructure = typeof(Sentinel.Infrastructure.DependencyInjection).Assembly;
    private static readonly Assembly Api = typeof(Program).Assembly;

    private static IEnumerable<string> ReferencedNames(Assembly assembly) =>
        assembly.GetReferencedAssemblies().Select(a => a.Name!);

    private static bool IsFramework(string name) =>
        name is "netstandard" or "mscorlib" || name.StartsWith("System", StringComparison.Ordinal);

    [Fact]
    public void Domain_references_nothing_but_the_base_class_library()
    {
        var offenders = ReferencedNames(Domain).Where(n => !IsFramework(n)).ToList();

        Assert.Empty(offenders);
    }

    [Fact]
    public void Contracts_reference_nothing_but_the_base_class_library()
    {
        Assert.DoesNotContain(ReferencedNames(Contracts), n => !IsFramework(n));
    }

    [Fact]
    public void Application_does_not_know_about_infrastructure_or_the_web()
    {
        var names = ReferencedNames(Application).ToList();

        Assert.DoesNotContain("Sentinel.Infrastructure", names);
        Assert.DoesNotContain("Sentinel.Api", names);
        Assert.DoesNotContain(names, n => n.StartsWith("Microsoft.AspNetCore", StringComparison.Ordinal));
        Assert.DoesNotContain(names, n => n.StartsWith("Microsoft.EntityFrameworkCore.SqlServer", StringComparison.Ordinal));
    }

    [Fact]
    public void Infrastructure_does_not_know_about_the_web()
    {
        Assert.DoesNotContain("Sentinel.Api", ReferencedNames(Infrastructure));
    }

    [Fact]
    public void Handlers_are_internal_so_only_the_dispatcher_can_call_them()
    {
        var publicHandlers = Application.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract)
            .Where(t => t.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequestHandler<,>)))
            .Where(t => t.IsPublic)
            .Select(t => t.FullName)
            .ToList();

        Assert.Empty(publicHandlers);
    }

    [Fact]
    public void Every_request_has_exactly_one_handler()
    {
        var requestTypes = Application.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false })
            .Where(t => t.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequest<>)))
            .ToList();

        var handledTypes = Application.GetTypes()
            .SelectMany(t => t.GetInterfaces())
            .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequestHandler<,>))
            .Select(i => i.GetGenericArguments()[0])
            .ToList();

        Assert.NotEmpty(requestTypes);
        Assert.All(requestTypes, r => Assert.Equal(1, handledTypes.Count(h => h == r)));
    }

    [Fact]
    public void Requests_live_in_a_folder_of_their_own_named_after_the_feature()
    {
        // Vertical slices: Sentinel.Application.<Feature>.<UseCase>.<Request>.
        var misplaced = Application.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false })
            .Where(t => t.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequest<>)))
            .Where(t => t.Namespace!.Split('.').Length != 4)
            .Select(t => t.FullName)
            .ToList();

        Assert.Empty(misplaced);
    }

    [Fact]
    public void The_api_references_the_layers_it_composes()
    {
        var names = ReferencedNames(Api).ToList();

        Assert.Contains("Sentinel.Application", names);
        Assert.Contains("Sentinel.Infrastructure", names);
    }
}
