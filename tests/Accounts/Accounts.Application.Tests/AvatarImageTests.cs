using Accounts.Application.Common;

namespace Accounts.Application.Tests;

public sealed class AvatarImageTests
{
    [Fact]
    public void Sniff_RecognisesJpegPngAndWebpByTheirFirstBytes()
    {
        Assert.Equal("image/jpeg", AvatarImage.Sniff([0xFF, 0xD8, 0xFF, 0xE0]));
        Assert.Equal("image/png", AvatarImage.Sniff([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0]));
        Assert.Equal("image/webp", AvatarImage.Sniff("RIFF\0\0\0\0WEBPVP8 "u8));
    }

    [Fact]
    public void Sniff_RejectsEverythingElse()
    {
        Assert.Null(AvatarImage.Sniff("GIF89a"u8));
        Assert.Null(AvatarImage.Sniff("<svg></svg>"u8));
        Assert.Null(AvatarImage.Sniff([]));
    }
}
