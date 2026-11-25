using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Yalfan.Domain.Identity;
using Yalfan.Infrastructure.Persistence;
using static Yalfan.Api.IntegrationTests.Auth.AuthTestHelpers;

namespace Yalfan.Api.IntegrationTests.Identity;

/// <summary>
/// The owner accounts in the server's configuration (ADR 0026). A restart is a second API started
/// on the same database with the settings of the test. Each test configures accounts with numbers
/// of its own, as a third account, so the seeded ones stay as they are.
/// </summary>
public sealed class OwnerAccountsTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task Both_configured_owner_accounts_exist_with_their_numbers()
    {
        var owner = await UserHolding(ApiFactory.SeededOwnerMobile);
        var developer = await UserHolding(ApiFactory.SeededSecondOwnerMobile);

        Assert.Equal(Role.Owner, owner.Role);
        Assert.Equal("Seeded Owner", owner.FullName);
        Assert.Equal(
            [ApiFactory.SeededOwnerMobile, ApiFactory.SeededOwnerSecondMobile],
            owner.LoginNumbers.Select(n => n.MobileNumber.Value).Order());
        Assert.Equal(Role.Owner, developer.Role);
        Assert.Equal(ApiFactory.SeededSecondOwnerMobile, Assert.Single(developer.LoginNumbers).MobileNumber.Value);
    }

    [Fact]
    public async Task A_restart_never_switches_a_number_back_on()
    {
        var first = ApiFactory.NextMobile();
        var second = ApiFactory.NextMobile();
        Start(("Third Owner", [first, second]));
        var account = await UserHolding(first);
        using var owner = await factory.CreateClientAsAsync(Role.Owner);
        var numberId = account.LoginNumbers.Single(n => n.MobileNumber.Value == second).Id;
        (await owner.PostAsync($"/panel/users/{account.Id}/numbers/{numberId}/deactivate", null, Cancel)).EnsureSuccessStatusCode();

        Start(("Third Owner", [first, second]));

        var after = await UserHolding(first);
        Assert.False(after.LoginNumbers.Single(n => n.Id == numberId).IsActive);
        Assert.Equal(2, after.LoginNumbers.Count);
    }

    [Fact]
    public async Task A_number_added_to_the_configuration_joins_the_same_account()
    {
        var first = ApiFactory.NextMobile();
        var added = ApiFactory.NextMobile();
        Start(("Third Owner", [first]));
        var before = await UserHolding(first);

        Start(("Renamed In Configuration", [first, added]));

        var after = await UserHolding(added);
        Assert.Equal(before.Id, after.Id);
        Assert.Equal(2, after.LoginNumbers.Count);

        // The name in the configuration only names a new account; the panel corrects it later.
        Assert.Equal("Third Owner", after.FullName);
    }

    [Fact]
    public async Task A_number_held_by_a_manager_stops_the_start()
    {
        var managerMobile = await factory.CreateUserAsync(Role.Manager);

        Assert.ThrowsAny<Exception>(() => Start(("Third Owner", [managerMobile])));

        Assert.Equal(Role.Manager, (await UserHolding(managerMobile)).Role);
    }

    [Fact]
    public void Numbers_of_two_different_users_in_one_account_stop_the_start()
    {
        Assert.ThrowsAny<Exception>(() => Start(("Third Owner", [ApiFactory.SeededOwnerMobile, ApiFactory.SeededSecondOwnerMobile])));
    }

    [Fact]
    public void A_number_that_is_not_an_iranian_mobile_stops_the_start()
    {
        Assert.ThrowsAny<Exception>(() => Start(("Third Owner", ["12345"])));
    }

    [Fact]
    public void A_number_under_two_accounts_stops_the_start()
    {
        var shared = ApiFactory.NextMobile();

        Assert.ThrowsAny<Exception>(() => Start(("Third Owner", [shared]), ("Fourth Owner", [ApiFactory.NextMobile(), shared])));
    }

    [Fact]
    public void Blank_entries_are_ignored()
    {
        // What docker compose passes when the variables in .env are left empty.
        Start(("", ["", " "]));
    }

    [Fact]
    public void The_old_single_owner_setting_stops_the_start()
    {
        using var old = factory.WithWebHostBuilder(builder =>
            builder.UseSetting("Identity:SeedOwner:MobileNumber", ApiFactory.NextMobile()));

        Assert.ThrowsAny<Exception>(() => old.CreateClient());
    }

    /// <summary>Starts a second API on the same database with these accounts after the two seeded ones.</summary>
    private void Start(params (string FullName, string[] Mobiles)[] accounts)
    {
        using var restarted = factory.WithWebHostBuilder(builder =>
        {
            for (var a = 0; a < accounts.Length; a++)
            {
                builder.UseSetting($"Identity:Owners:{a + 2}:FullName", accounts[a].FullName);
                for (var i = 0; i < accounts[a].Mobiles.Length; i++)
                {
                    builder.UseSetting($"Identity:Owners:{a + 2}:MobileNumbers:{i}", accounts[a].Mobiles[i]);
                }
            }
        });

        using var client = restarted.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
    }

    private async Task<User> UserHolding(string mobile)
    {
        var id = await factory.UserIdOfAsync(mobile);
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await db.Users.AsNoTracking().SingleAsync(u => u.Id == id, Cancel);
    }
}
