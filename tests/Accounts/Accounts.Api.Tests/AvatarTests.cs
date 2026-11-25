using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Accounts.Api.Tests;

/// <summary>2.15: a picture is uploaded from a file (JPG, PNG or WebP, at most 2 MB), served to everyone and refused with a readable code otherwise.</summary>
public sealed class AvatarTests(AccountsApiFactory api) : IClassFixture<AccountsApiFactory>
{
    private static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 0];

    [Fact]
    public async Task A_png_is_uploaded_served_and_shows_up_in_the_profile()
    {
        var id = await api.RegisterAsync(AccountsApiFactory.Unique("awatar"), "Awatar");
        using var client = api.SignedIn(id);

        using var uploaded = await client.PostAsync("/users/v1/me/avatar", Upload(Png, "image/png", "a.png"));
        Assert.Equal(HttpStatusCode.OK, uploaded.StatusCode);
        var url = (await uploaded.ReadJsonAsync()).GetProperty("avatarUrl").GetString();
        Assert.StartsWith($"/users/v1/{id}/avatar?v=", url);

        using var served = await api.CreateClient().GetAsync($"/users/v1/{id}/avatar");
        Assert.Equal(HttpStatusCode.OK, served.StatusCode);
        Assert.Equal("image/png", served.Content.Headers.ContentType?.MediaType);
        Assert.Equal(Png, await served.Content.ReadAsByteArrayAsync());
        Assert.Contains("max-age=86400", served.Headers.CacheControl?.ToString());
        Assert.NotNull(served.Headers.ETag);
    }

    [Fact]
    public async Task A_file_that_is_not_an_image_is_refused_with_a_readable_code()
    {
        var id = await api.RegisterAsync(AccountsApiFactory.Unique("gif"), "Gif");
        using var client = api.SignedIn(id);

        using var response = await client.PostAsync("/users/v1/me/avatar", Upload("GIF89a"u8.ToArray(), "image/gif", "a.gif"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("profile.avatar_type_invalid", (await response.ReadJsonAsync()).GetProperty("code").GetString());
    }

    [Fact]
    public async Task A_file_over_two_megabytes_is_refused_with_a_readable_code()
    {
        var id = await api.RegisterAsync(AccountsApiFactory.Unique("duzy"), "Duzy");
        using var client = api.SignedIn(id);
        var big = new byte[2 * 1024 * 1024 + 1];
        Png.CopyTo(big, 0);

        using var response = await client.PostAsync("/users/v1/me/avatar", Upload(big, "image/png", "big.png"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("profile.avatar_too_large", (await response.ReadJsonAsync()).GetProperty("code").GetString());
    }

    [Fact]
    public async Task A_person_without_a_picture_gets_404_with_a_code()
    {
        var id = await api.RegisterAsync(AccountsApiFactory.Unique("bez"), "Bez");

        using var response = await api.CreateClient().GetAsync($"/users/v1/{id}/avatar");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("profile.avatar_not_found", (await response.ReadJsonAsync()).GetProperty("code").GetString());
    }

    private static MultipartFormDataContent Upload(byte[] bytes, string contentType, string fileName)
    {
        var file = new ByteArrayContent(bytes);
        file.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        var form = new MultipartFormDataContent();
        form.Add(file, "file", fileName);
        return form;
    }
}
