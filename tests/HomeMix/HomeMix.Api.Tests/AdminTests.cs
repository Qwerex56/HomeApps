using System.Net;
using System.Net.Http.Json;

namespace HomeMix.Api.Tests;

/// <summary>docs/homemix.md, "The Owner's view": every installation and the blocks, for the server role Owner only.</summary>
public sealed class AdminTests(HomeMixApiFactory api) : IClassFixture<HomeMixApiFactory>
{
    [Theory]
    [InlineData("User")]
    [InlineData("Admin")]
    public async Task Only_the_owner_sees_the_view(string role)
    {
        using var client = api.WithRole(Guid.NewGuid(), role);

        using var installations = await client.GetAsync("/homemix/api/admin/installations");
        using var block = await client.PutAsJsonAsync("/homemix/api/admin/blocks/pomodoro", new { reason = "no" });

        Assert.Equal(HttpStatusCode.Forbidden, installations.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, block.StatusCode);
    }

    [Fact]
    public async Task A_blocked_addon_stops_being_served_until_it_is_unblocked()
    {
        var admin = Guid.NewGuid();
        var household = api.Household(admin);
        using var admins = api.SignedIn(admin);
        var id = await admins.InstalledInAsync(household, AddonZip.Create(AddonZip.Manifest(id: "dice")));
        using var owner = api.WithRole(Guid.NewGuid(), "Owner");
        using var anonymous = api.CreateClient();

        using var blocked = await owner.PutAsJsonAsync("/homemix/api/admin/blocks/dice", new { reason = "  broken  " });
        var listed = await (await owner.GetAsync("/homemix/api/admin/installations")).ReadJsonAsync();
        using var run = await anonymous.GetAsync($"/homemix/run/{id}/");
        using var again = await admins.UploadAsync(AddonZip.Create(AddonZip.Manifest(id: "dice", version: "1.0.1")));
        using var unblocked = await owner.DeleteAsync("/homemix/api/admin/blocks/dice");
        using var runAgain = await anonymous.GetAsync($"/homemix/run/{id}/");

        Assert.Equal("broken", (await blocked.ReadJsonAsync()).GetProperty("reason").GetString());
        var mine = listed.EnumerateArray().Single(i => i.GetProperty("id").GetGuid() == id);
        Assert.True(mine.GetProperty("blocked").GetBoolean());
        Assert.Equal(household, mine.GetProperty("targetId").GetGuid());
        Assert.Equal(HttpStatusCode.NotFound, run.StatusCode);
        Assert.Equal("homemix.blocked", await again.CodeAsync());
        Assert.Equal(HttpStatusCode.NoContent, unblocked.StatusCode);
        Assert.Equal(HttpStatusCode.OK, runAgain.StatusCode);
    }

    [Fact]
    public async Task A_block_from_the_configuration_is_shown_but_not_lifted_here()
    {
        using var owner = api.WithRole(Guid.NewGuid(), "Owner");

        var blocks = await (await owner.GetAsync("/homemix/api/admin/blocks")).ReadJsonAsync();
        using var lift = await owner.DeleteAsync("/homemix/api/admin/blocks/evil");
        using var unknown = await owner.DeleteAsync("/homemix/api/admin/blocks/nothing-here");
        using var invalid = await owner.PutAsJsonAsync("/homemix/api/admin/blocks/Not%20an%20id", new { reason = "x" });

        var evil = blocks.EnumerateArray().Single(b => b.GetProperty("value").GetString() == "evil");
        Assert.True(evil.GetProperty("fromConfiguration").GetBoolean());
        Assert.Equal("homemix.block_in_configuration", await lift.CodeAsync());
        Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
    }
}
