using Microsoft.EntityFrameworkCore;
using Sentinel.Application.Abstractions.Ports;
using Sentinel.Domain.Access;
using Sentinel.Domain.Pages;
using Sentinel.Domain.Roles;
using Sentinel.Domain.Security;
using Sentinel.Domain.Users;

namespace Sentinel.Infrastructure.Persistence.Seeding;

/// <summary>
/// Fills an empty development database with a small company whose sign-in
/// histories each tell one story the security page should recognise. All
/// times are relative to "now", so the demo looks fresh whenever it runs.
/// </summary>
/// <remarks>
/// The seeder uses the domain factories, not raw inserts, so seed data obeys
/// the same invariants as real data. A seeder that bypasses the domain is how
/// a development database ends up containing states production never can.
/// </remarks>
public sealed class DevelopmentDataSeeder(SentinelDbContext db, IClock clock, IGeoLocator geoLocator)
{
    private static class Agents
    {
        public const string ChromeWindows = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/141.0.0.0 Safari/537.36";
        public const string EdgeWindows = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/141.0.0.0 Safari/537.36 Edg/141.0.0.0";
        public const string ChromeMac = "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/141.0.0.0 Safari/537.36";
        public const string SafariIphone = "Mozilla/5.0 (iPhone; CPU iPhone OS 18_6 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.6 Mobile/15E148 Safari/604.1";
        public const string FirefoxLinux = "Mozilla/5.0 (X11; Linux x86_64; rv:143.0) Gecko/20100101 Firefox/143.0";
        public const string PythonRequests = "python-requests/2.32.3";
    }

    private static class Ips
    {
        public const string Lagos = "102.89.34.";
        public const string Abuja = "105.112.18.";
        public const string London = "81.2.69.";
        public const string Frankfurt = "185.220.101.";
        public const string Amsterdam = "145.53.12.";
        public const string Singapore = "116.88.40.";
        public const string NewYork = "72.229.28.";
    }

    private readonly List<SignInAttempt> _attempts = [];
    private DateTime _now;

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (await db.Users.AnyAsync(cancellationToken))
            return;

        // Whole minutes: the demo reads better without microseconds.
        var now = clock.UtcNow;
        _now = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, 0, DateTimeKind.Utc);

        var roles = SeedRoles();
        SeedPages();
        SeedUsers(roles);

        db.SignInAttempts.AddRange(_attempts);
        await db.SaveChangesAsync(cancellationToken);
    }

    private Dictionary<string, Role> SeedRoles()
    {
        var definitions = new (string Name, string Description, string[] Permissions)[]
        {
            ("Administrator", "Full access to everything.", ["*"]),
            ("Finance Manager", "Finance reports, ledger export and billing.", ["reports.view", "reports.finance", "reports.finance.*", "billing.*"]),
            ("Finance Analyst", "Finance reports and billing, read only.", ["reports.view", "reports.finance", "billing.view"]),
            ("Operations", "Operational reporting.", ["reports.view", "reports.operations"]),
            ("Support Agent", "The support desk.", ["support.*"]),
            ("Auditor", "Every report, and read access to user administration.", ["reports.view", "reports.*", "admin.access", "admin.users"]),
        };

        var roles = new Dictionary<string, Role>();
        foreach (var (name, description, permissions) in definitions)
        {
            var role = Role.Create(name, description).Value;
            foreach (var permission in permissions)
                role.Grant(Permission.From(permission));

            roles[name] = role;
            db.Roles.Add(role);
        }

        return roles;
    }

    private void SeedPages()
    {
        Page Add(string path, string title, Page? parent = null, string? permission = null, bool isPublic = false, int order = 0)
        {
            var page = Page.Create(
                path, title, parent?.Id,
                permission is null ? null : Permission.From(permission),
                isPublic, order).Value;
            db.Pages.Add(page);
            return page;
        }

        var home = Add("/", "Dashboard", order: 0);

        var account = Add("/account", "My account", home, order: 10);
        Add("/account/security", "Account security", account, order: 0);

        var reports = Add("/reports", "Reports", home, "reports.view", order: 20);
        var finance = Add("/reports/finance", "Finance", reports, "reports.finance", order: 0);
        Add("/reports/finance/export", "Ledger export", finance, "reports.finance.export", order: 0);
        Add("/reports/operations", "Operations", reports, "reports.operations", order: 10);

        Add("/billing", "Billing", home, "billing.view", order: 30);
        Add("/support", "Support desk", home, "support.tickets", order: 40);

        var admin = Add("/admin", "Administration", home, "admin.access", order: 90);
        Add("/admin/users", "Users", admin, "admin.users", order: 0);
        Add("/admin/roles", "Roles", admin, "admin.roles", order: 10);
        Add("/admin/access-explorer", "Access explorer", admin, "admin.users", order: 20);

        // Public pages sit outside the tree: they must work signed out.
        Add("/help", "Help centre", isPublic: true, order: 100);
        Add("/status", "System status", isPublic: true, order: 110);
    }

    private void SeedUsers(Dictionary<string, Role> roles)
    {
        User Add(string email, string name, string zone, params string[] roleNames)
        {
            var user = User.Create(EmailAddress.Create(email).Value, name, zone, _now.AddDays(-120)).Value;
            foreach (var roleName in roleNames)
                user.AssignRole(roles[roleName].Id, _now.AddDays(-120));
            db.Users.Add(user);
            return user;
        }

        // Ada: an administrator with an ordinary history and one new phone. Low.
        var ada = Add("ada.okafor@sentinel.dev", "Ada Okafor", "Africa/Lagos", "Administrator");
        Workdays(ada, Ips.Lagos, Agents.ChromeWindows, days: 60, localHour: 8.5);
        At(ada, _now.AddDays(-3).AddHours(-2), Ips.Lagos, Agents.SafariIphone);
        At(ada, _now.AddHours(-2), Ips.Lagos, Agents.ChromeWindows);

        // Bola: Lagos, then London forty minutes later. Impossible travel. High.
        var bola = Add("bola.adeyemi@sentinel.dev", "Bola Adeyemi", "Africa/Lagos", "Finance Manager");
        Workdays(bola, Ips.Lagos, Agents.ChromeWindows, days: 60, localHour: 9, skipLastDays: 2);
        var lagosSignIn = _now.AddDays(-2).Date.AddHours(8); // 09:00 in Lagos
        At(bola, lagosSignIn, Ips.Lagos, Agents.ChromeWindows);
        At(bola, lagosSignIn.AddMinutes(40), Ips.London, Agents.SafariIphone);

        // Chen: eight failures from Frankfurt in nine minutes, none succeeded. Medium.
        var chen = Add("chen.wei@sentinel.dev", "Chen Wei", "Asia/Singapore", "Operations");
        Workdays(chen, Ips.Singapore, Agents.ChromeMac, days: 60, localHour: 9);
        Burst(chen, _now.AddDays(-1).Date.AddHours(3).AddMinutes(10), Ips.Frankfurt, Agents.FirefoxLinux, failures: 8, spacing: TimeSpan.FromSeconds(70));

        // Dana: a genuine trip from New York to London, new laptop too. Medium + Low.
        var dana = Add("dana.smith@sentinel.dev", "Dana Smith", "America/New_York", "Support Agent");
        Workdays(dana, Ips.NewYork, Agents.FirefoxLinux, days: 60, localHour: 9, skipLastDays: 5);
        for (var day = 5; day >= 1; day--)
            At(dana, _now.AddDays(-day).Date.AddHours(9), Ips.London, Agents.EdgeWindows);

        // Emeka: a contractor. Finance Manager, but explicitly denied the ledger export. Clean.
        var emeka = Add("emeka.nwosu@sentinel.dev", "Emeka Nwosu", "Europe/London", "Finance Manager");
        emeka.SetOverride(Permission.From("reports.finance.export"), PermissionEffect.Deny);
        Workdays(emeka, Ips.London, Agents.ChromeWindows, days: 60, localHour: 9);

        // Femi: an auditor in Abuja. Clean.
        var femi = Add("femi.bello@sentinel.dev", "Femi Bello", "Africa/Lagos", "Auditor");
        Workdays(femi, Ips.Abuja, Agents.EdgeWindows, days: 60, localHour: 10);

        // Grace: left the company. Suspended; sees public pages only.
        var grace = Add("grace.eze@sentinel.dev", "Grace Eze", "Africa/Lagos", "Support Agent");
        Workdays(grace, Ips.Lagos, Agents.ChromeWindows, days: 60, localHour: 8, skipLastDays: 30);
        grace.Suspend("Left the company.");

        // Tunde: runs integration scripts every four hours, so has more than
        // 500 sign-ins in ninety days. Yesterday someone in Amsterdam guessed
        // the password. High. This is the account in ticket SUP-4219.
        var tunde = Add("tunde.bakare@sentinel.dev", "Tunde Bakare", "Africa/Lagos", "Operations");
        Every(tunde, Ips.Lagos, Agents.PythonRequests, days: 90, interval: TimeSpan.FromHours(4));
        var attack = _now.AddDays(-1).Date.AddHours(21).AddMinutes(5);
        Burst(tunde, attack, Ips.Amsterdam, Agents.ChromeWindows, failures: 7, spacing: TimeSpan.FromSeconds(50));
        At(tunde, attack.AddMinutes(6.5), Ips.Amsterdam, Agents.ChromeWindows);
    }

    /// <summary>One successful sign-in per weekday at <paramref name="localHour"/> in the user's own zone.</summary>
    private void Workdays(User user, string ipPrefix, string agent, int days, double localHour, int skipLastDays = 0)
    {
        var zone = TimeZoneInfo.FindSystemTimeZoneById(user.TimeZoneId);
        // string.GetHashCode is randomised per process; seed from the characters
        // instead so the demo data is the same on every run.
        var random = new Random(user.Email.Value.Sum(c => c));

        for (var day = days; day > skipLastDays; day--)
        {
            var localDate = TimeZoneInfo.ConvertTimeFromUtc(_now, zone).Date.AddDays(-day);
            if (localDate.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
                continue;

            var local = localDate.AddHours(localHour).AddMinutes(random.Next(-20, 25));
            At(user, TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(local, DateTimeKind.Unspecified), zone), ipPrefix, agent, random);
        }
    }

    private void Every(User user, string ipPrefix, string agent, int days, TimeSpan interval)
    {
        for (var at = _now.AddDays(-days); at < _now; at += interval)
            At(user, at, ipPrefix, agent);
    }

    private void Burst(User user, DateTime startUtc, string ipPrefix, string agent, int failures, TimeSpan spacing)
    {
        for (var i = 0; i < failures; i++)
            At(user, startUtc + spacing * i, ipPrefix, agent, succeeded: false);
    }

    private void At(User user, DateTime occurredAtUtc, string ipPrefix, string agent, Random? random = null, bool succeeded = true)
    {
        if (occurredAtUtc >= _now)
            return;

        var ip = ipPrefix + ((random?.Next(2, 250)) ?? 17);
        var attempt = SignInAttempt.Record(
            user.Id,
            DateTime.SpecifyKind(occurredAtUtc, DateTimeKind.Utc),
            ip,
            geoLocator.Locate(ip),
            agent,
            succeeded,
            succeeded ? null : SignInFailureReason.WrongPassword).Value;

        _attempts.Add(attempt);
    }
}
