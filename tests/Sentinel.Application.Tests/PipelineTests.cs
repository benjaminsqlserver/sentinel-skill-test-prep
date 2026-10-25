using Microsoft.EntityFrameworkCore;
using Sentinel.Application.Abstractions.Validation;
using Sentinel.Application.Roles.CreateRole;
using Sentinel.Application.Users.CreateUser;
using Sentinel.Application.Users.GetUsers;
using Sentinel.Domain.Common;

namespace Sentinel.Application.Tests;

public class PipelineTests
{
    [Fact]
    public async Task Invalid_requests_are_rejected_before_the_handler_runs()
    {
        await using var host = await TestHost.CreateAsync();

        var result = await host.Send(new CreateUserCommand("", "", "Africa/Lagos"));

        var error = Assert.IsType<ValidationError>(result.Error);
        Assert.Equal(["Email", "DisplayName"], error.Failures.Select(f => f.Property));
        Assert.Equal(0, await host.Query(db => db.Users.CountAsync()));
    }

    [Fact]
    public async Task Validation_failures_for_generic_results_have_the_right_type()
    {
        await using var host = await TestHost.CreateAsync();

        // Result<PagedResponse<...>>: exercises ResultFactory's reflection path.
        var result = await host.Send(new GetUsersQuery(null, Page: 0, PageSize: 1000));

        Assert.True(result.IsFailure);
        Assert.Equal(2, Assert.IsType<ValidationError>(result.Error).Failures.Count);
    }

    [Fact]
    public async Task A_valid_command_reaches_its_handler_and_persists()
    {
        await using var host = await TestHost.CreateAsync();

        var result = await host.Send(new CreateUserCommand("Ada@Example.com", "Ada", "Africa/Lagos"));

        Assert.True(result.IsSuccess);
        var saved = await host.Query(db => db.Users.SingleAsync());
        Assert.Equal("ada@example.com", saved.Email.Value);
        Assert.Equal(DateTimeKind.Utc, saved.CreatedAtUtc.Kind);
    }

    [Fact]
    public async Task Duplicate_emails_are_a_conflict_whatever_their_case()
    {
        await using var host = await TestHost.CreateAsync();
        await host.Send(new CreateUserCommand("ada@example.com", "Ada", "Africa/Lagos"));

        var result = await host.Send(new CreateUserCommand("ADA@example.com", "Ada again", "Africa/Lagos"));

        Assert.Equal(ErrorType.Conflict, result.Error!.Type);
    }

    [Fact]
    public async Task Duplicate_role_names_are_a_conflict_whatever_their_case()
    {
        await using var host = await TestHost.CreateAsync();
        await host.Send(new CreateRoleCommand("Auditor", null));

        var result = await host.Send(new CreateRoleCommand("auditor", null));

        Assert.Equal(ErrorType.Conflict, result.Error!.Type);
    }
}
