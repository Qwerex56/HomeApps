using HomeApps.BuildingBlocks.Domain.Errors;
using HomeApps.BuildingBlocks.Mcp;

namespace HomeApps.BuildingBlocks.Tests;

public sealed class NameLookupTests
{
    private sealed record Item(Guid Id, string Name);

    private static readonly Item Home = new(Guid.NewGuid(), "Dom");
    private static readonly Item Cottage = new(Guid.NewGuid(), "Działka");
    private static readonly Item OtherHome = new(Guid.NewGuid(), "dom ");

    [Fact]
    public void A_name_matches_ignoring_case_and_spaces_and_an_id_matches_exactly()
    {
        Item[] items = [Home, Cottage];

        Assert.Equal(Home, NameLookup.Find(items, " DOM", i => i.Id, i => i.Name, "household").Value);
        Assert.Equal(Cottage, NameLookup.Find(items, Cottage.Id.ToString(), i => i.Id, i => i.Name, "household").Value);
    }

    [Fact]
    public void Several_matches_are_never_guessed()
    {
        var found = NameLookup.Find([Home, OtherHome], "Dom", i => i.Id, i => i.Name, "household");

        var error = Assert.IsType<AppError>(found.Errors[0]);
        Assert.Equal("household.ambiguous", error.Code);
        Assert.Contains(Home.Id.ToString(), error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void An_unknown_name_lists_what_there_is()
    {
        var found = NameLookup.Find([Home, Cottage], "Garaż", i => i.Id, i => i.Name, "household", "households_my_households");

        var error = Assert.IsType<AppError>(found.Errors[0]);
        Assert.Equal("household.not_found", error.Code);
        Assert.Contains("Dom, Działka", error.Message, StringComparison.Ordinal);
        Assert.Contains("households_my_households", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Nothing_named_means_the_only_one_or_asks_which()
    {
        var only = NameLookup.Find([Home], null, i => i.Id, i => i.Name, "household");
        var two = NameLookup.Find([Home, Cottage], " ", i => i.Id, i => i.Name, "household");

        Assert.Equal(Home, only.Value);
        var error = Assert.IsType<AppError>(two.Errors[0]);
        Assert.Equal("household.ambiguous", error.Code);
        Assert.Contains("Dom, Działka", error.Message, StringComparison.Ordinal);
    }
}
