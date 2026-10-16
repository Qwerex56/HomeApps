using FluentResults;
using HomeApps.BuildingBlocks.Api.Results;
using HomeApps.BuildingBlocks.Application;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace HomeApps.BuildingBlocks.Tests;

public sealed class ResultHttpMapperTests
{
    [Fact]
    public void Any_error_maps_to_400_with_its_message()
    {
        var httpResult = Result.Fail("Household is full.").ToHttpResult();

        var problem = Assert.IsType<ProblemHttpResult>(httpResult);
        Assert.Equal(StatusCodes.Status400BadRequest, problem.StatusCode);
        Assert.Equal("Household is full.", problem.ProblemDetails.Detail);
    }

    [Fact]
    public void Field_errors_map_to_a_validation_problem()
    {
        var httpResult = Result.Fail(ValidationErrors.ForField("Name", "Required.")).ToHttpResult();

        var problem = Assert.IsType<ValidationProblem>(httpResult);
        Assert.Equal(StatusCodes.Status400BadRequest, problem.StatusCode);
        Assert.Equal(["Required."], problem.ProblemDetails.Errors["Name"]);
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
