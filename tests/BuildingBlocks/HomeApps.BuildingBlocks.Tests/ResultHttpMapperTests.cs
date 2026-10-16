using FluentResults;
using HomeApps.BuildingBlocks.Api.Results;
using HomeApps.BuildingBlocks.Domain.Errors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace HomeApps.BuildingBlocks.Tests;

public sealed class ResultHttpMapperTests
{
    public static TheoryData<IError, int> Errors => new()
    {
        { new NotFoundError("x.not_found", "m"), StatusCodes.Status404NotFound },
        { new ConflictError("x.conflict", "m"), StatusCodes.Status409Conflict },
        { new ForbiddenError("x.forbidden", "m"), StatusCodes.Status403Forbidden },
        { new UnauthorizedError("x.unauthorized", "m"), StatusCodes.Status401Unauthorized },
        { ValidationError.ForField("Name", "m"), StatusCodes.Status400BadRequest },
        { new Error("untyped"), StatusCodes.Status422UnprocessableEntity },
    };

    [Theory]
    [MemberData(nameof(Errors))]
    public void Typed_errors_map_to_their_status(IError error, int expectedStatus)
    {
        var httpResult = Result.Fail(error).ToHttpResult();

        var status = Assert.IsAssignableFrom<IStatusCodeHttpResult>(httpResult).StatusCode;
        Assert.Equal(expectedStatus, status);
    }

    [Fact]
    public void Validation_errors_carry_field_failures_and_the_code()
    {
        var httpResult = Result.Fail(ValidationError.ForField("Name", "Required.")).ToHttpResult();

        var problem = Assert.IsType<ValidationProblem>(httpResult);
        Assert.Equal(["Required."], problem.ProblemDetails.Errors["Name"]);
        Assert.Equal("validation.failed", problem.ProblemDetails.Extensions["code"]);
    }

    [Fact]
    public void Success_maps_to_200_204_and_201()
    {
        Assert.IsType<NoContent>(Result.Ok().ToHttpResult());
        Assert.IsType<Ok<int>>(Result.Ok(5).ToHttpResult());

        var created = Assert.IsType<Created<int>>(Result.Ok(5).ToCreatedHttpResult(id => $"/things/{id}"));
        Assert.Equal("/things/5", created.Location);
    }
}
