using FluentResults;
using HomeApps.BuildingBlocks.Application;
using HomeApps.BuildingBlocks.Domain.Errors;
using HomeApps.BuildingBlocks.Mcp;
using ModelContextProtocol.Protocol;
using Result = FluentResults.Result;

namespace HomeApps.BuildingBlocks.Tests;

public sealed class ToolResultTests
{
    [Fact]
    public void Success_is_the_described_text()
    {
        var result = ToolResult.From(Result.Ok(3), n => $"{n} tasks");

        Assert.NotEqual(true, result.IsError);
        Assert.Equal("3 tasks", Text(result));
    }

    [Fact]
    public void Failures_keep_the_api_codes_and_name_bad_fields()
    {
        var result = ToolResult.From(
            Result.Fail(new AppError("household.forbidden", "Your role does not allow this.", ErrorKind.Forbidden))
                .WithError(ValidationErrors.ForField("Text", "Too long.")),
            "done");

        Assert.True(result.IsError);
        Assert.Equal(
            $"Not allowed (household.forbidden): Your role does not allow this.{Environment.NewLine}Invalid Text: Too long.",
            Text(result));
    }

    private static string Text(CallToolResult result) => Assert.IsType<TextContentBlock>(Assert.Single(result.Content)).Text;
}
