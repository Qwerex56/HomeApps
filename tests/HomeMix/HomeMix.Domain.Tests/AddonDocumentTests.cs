using HomeApps.BuildingBlocks.Domain.Errors;
using HomeMix.Domain.Data;

namespace HomeMix.Domain.Tests;

public sealed class AddonDocumentTests
{
    private static readonly Guid Adam = Guid.NewGuid();
    private static readonly DateTimeOffset Now = new(2026, 11, 12, 12, 0, 0, TimeSpan.Zero);

    private static AddonDocument Document(DataScope scope = DataScope.Household, Guid? owner = null) =>
        AddonDocument.Create(Guid.NewGuid(), scope, owner, "sessions", "a1", "{}", 2, Adam, Now);

    [Fact]
    public void A_new_document_is_version_one()
    {
        var document = Document();

        Assert.Equal(1, document.Version);
        Assert.Null(document.OwnerUserId);
    }

    [Fact]
    public void Shared_documents_have_no_owner_even_when_one_is_given() =>
        Assert.Null(Document(DataScope.Household, Adam).OwnerUserId);

    [Fact]
    public void Replacing_moves_the_version_on()
    {
        var document = Document();

        Assert.True(document.Replace("[1]", 3, 1, Adam, Now).IsSuccess);
        Assert.True(document.Replace("[2]", 3, null, Adam, Now).IsSuccess);

        Assert.Equal(3, document.Version);
        Assert.Equal("[2]", document.Value);
    }

    [Fact]
    public void A_stale_version_is_refused()
    {
        var document = Document();
        document.Replace("[1]", 3, 1, Adam, Now);

        var result = document.Replace("[2]", 3, 1, Adam, Now);

        Assert.Equal("homemix.conflict", Assert.Single(result.Errors).Metadata[AppError.CodeKey]);
        Assert.Equal("[1]", document.Value);
    }

    [Theory]
    [InlineData("sessions", true)]
    [InlineData("2026-11.notes_v2", true)]
    [InlineData("-x", false)]
    [InlineData("a/b", false)]
    [InlineData("", false)]
    public void Names_fit_a_url_segment(string name, bool valid) => Assert.Equal(valid, DataNames.IsValid(name));
}
