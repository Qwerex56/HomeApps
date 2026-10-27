using Accounts.Application.Abstractions;
using Accounts.Domain.Common;
using Accounts.Domain.Users;
using Accounts.Infrastructure.Persistence;

namespace Accounts.Infrastructure.Tests;

/// <summary>Each test marks its users with its own word and searches for it, so tests sharing the database never meet.</summary>
public sealed class DirectoryTests(PostgresFixture database) : IClassFixture<PostgresFixture>
{
    private readonly FixedTime _time = new(A.Now);

    private async Task Save(params User[] users)
    {
        await using var db = database.CreateContext();
        var repository = new UserRepository(db);
        foreach (var user in users)
        {
            repository.Add(user);
        }

        await db.SaveChangesAsync();
    }

    private async Task<UserPage> List(string search, UserSort sort = UserSort.DisplayName, bool descending = false,
        int page = 1, int pageSize = 20, AccountStatus? status = null, ServerRole? role = null)
    {
        await using var db = database.CreateContext();
        return await new UserDirectory(db).ListAsync(new UserListFilter(status, role, search, page, pageSize, sort, descending), default);
    }

    [Fact]
    public async Task Search_is_case_insensitive_on_name_and_email()
    {
        await Save(
            A.User(_time, displayName: "Kasia Szukana"),
            A.User(_time, email: A.Unique("szukana.mail"), displayName: "Ktoś"),
            A.User(_time, displayName: "Inna"));

        var page = await List("SZUKANA");

        Assert.Equal(2, page.Total);
        Assert.Equal(["Kasia Szukana", "Ktoś"], page.Items.Select(i => i.DisplayName));
    }

    [Fact]
    public async Task Percent_and_underscore_match_only_themselves()
    {
        await Save(A.User(_time, displayName: "Zniżka 100%"), A.User(_time, displayName: "Zniżka 1000"), A.User(_time, displayName: "Zniżka_x"));

        Assert.Equal(["Zniżka 100%"], (await List("ka 100%")).Items.Select(i => i.DisplayName));
        Assert.Equal(["Zniżka_x"], (await List("ka_")).Items.Select(i => i.DisplayName));
    }

    [Fact]
    public async Task Filters_sort_and_pages()
    {
        var admin = A.User(_time, displayName: "Sortowany A", role: ServerRole.Admin);
        var owner = A.User(_time, displayName: "Sortowany B", role: ServerRole.Owner);
        var user = A.User(_time, displayName: "Sortowany C");
        var disabled = A.User(_time, displayName: "Sortowany D");
        disabled.Disable(A.Owner, null, _time);
        await Save(admin, owner, user, disabled);

        // By rank, not alphabetically: User < Admin < Owner.
        var byRole = (await List("Sortowany", UserSort.Role)).Items.Select(i => i.DisplayName).ToList();
        Assert.Equal(["Sortowany C", "Sortowany D"], byRole.Take(2).Order());
        Assert.Equal(["Sortowany A", "Sortowany B"], byRole.Skip(2));
        Assert.Equal(["Sortowany B", "Sortowany A"],
            (await List("Sortowany", UserSort.Role, descending: true)).Items.Take(2).Select(i => i.DisplayName));
        Assert.Equal(["Sortowany D"], (await List("Sortowany", status: AccountStatus.Disabled)).Items.Select(i => i.DisplayName));
        Assert.Equal(["Sortowany A"], (await List("Sortowany", role: ServerRole.Admin)).Items.Select(i => i.DisplayName));

        var second = await List("Sortowany", pageSize: 3, page: 2);
        Assert.Equal(4, second.Total);
        Assert.Equal(["Sortowany D"], second.Items.Select(i => i.DisplayName));
        var item = Assert.Single(second.Items);
        Assert.Equal(disabled.Email.Value, item.Email);
        Assert.Equal(AccountStatus.Disabled, item.Status);
    }

    [Fact]
    public async Task History_is_newest_first_with_limit_and_cursor()
    {
        var user = A.User(_time);
        for (var i = 1; i <= 4; i++)
        {
            _time.Now = A.Now.AddMinutes(i);
            user.UpdateProfile(new Actor(user.Id, user.Role), A.Profile($"Adam {i}"), _time);
        }

        await Save(user);
        await using var db = database.CreateContext();
        var directory = new UserDirectory(db);

        var newest = await directory.HistoryAsync(user.Id, null, 2, default);
        var older = await directory.HistoryAsync(user.Id, newest[^1].At, 10, default);

        Assert.Equal([A.Now.AddMinutes(4), A.Now.AddMinutes(3)], newest.Select(h => h.At));
        Assert.Equal([A.Now.AddMinutes(2), A.Now.AddMinutes(1), A.Now], older.Select(h => h.At));
        Assert.Equal("DisplayName", newest[0].Details);
    }

    [Fact]
    public async Task Display_names_skip_unknown_ids()
    {
        var user = A.User(_time, displayName: "Nazwa Do Odczytu");
        await Save(user);
        await using var db = database.CreateContext();

        var names = await new UserDirectory(db).DisplayNamesAsync([user.Id, Guid.NewGuid()], default);

        Assert.Equal("Nazwa Do Odczytu", Assert.Single(names).Value);
    }
}
