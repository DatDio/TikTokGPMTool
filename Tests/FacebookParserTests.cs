using TikTokGPMTool.Models;
using TikTokGPMTool.Services;
using TikTokGPMTool.ViewModels;
using Xunit;
using System.Linq;

namespace TikTokGPMTool.Tests;

public sealed class FacebookParserTests
{
    [Theory]
    [InlineData("123456789", "123456789")]
    [InlineData("my.public.group", "my.public.group")]
    [InlineData("https://www.facebook.com/groups/123456789/members/", "123456789")]
    [InlineData("https://facebook.com/groups/my-group?ref=share", "my-group")]
    public void ParsesGroupInputs(string input, string expected)
    {
        Assert.True(FacebookGroupInput.TryParse(input, out var group));
        Assert.Equal(expected, group!.Key);
        Assert.Equal($"https://www.facebook.com/groups/{expected}/members", group.MembersUrl);
    }

    [Theory]
    [InlineData("https://www.facebook.com/profile.php?id=100012345678901", "100012345678901")]
    [InlineData("https://www.facebook.com/user/100012345678901/", "100012345678901")]
    [InlineData("https://www.facebook.com/100012345678901", "100012345678901")]
    [InlineData("https://www.facebook.com/some.username", null)]
    [InlineData("https://www.facebook.com/groups/123456789", null)]
    public void ExtractsOnlyNumericMemberUid(string input, string? expected) =>
        Assert.Equal(expected, FacebookGroupScanner.TryExtractUid(input));

    [Fact]
    public void GroupListRemovesDuplicatesAndInvalidLines()
    {
        var result = MainViewModel.ParseFacebookGroups("123456789\nhttps://facebook.com/groups/123456789/members\nhttps://example.com/groups/42\nmy-group");
        Assert.Equal(2, result.Count);
        Assert.Equal(["123456789", "my-group"], result.Select(x => x.Key));
    }
}
