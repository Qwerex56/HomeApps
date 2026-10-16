using System.Reflection;
using NetArchTest.Rules;
using ServiceName.Domain;

namespace ServiceName.IntegrationTests;

/// <summary>Fails the build when a layer references the wrong way. Dependencies point inward only.</summary>
public sealed class ArchitectureTests
{
    private static readonly Assembly Domain = DomainAssembly.Assembly;
    private static readonly Assembly Application = typeof(ServiceName.Application.DependencyInjection).Assembly;
    private static readonly Assembly Infrastructure = typeof(ServiceName.Infrastructure.DependencyInjection).Assembly;

    private static readonly string[] Frameworks =
    [
        "Microsoft.EntityFrameworkCore",
        "Microsoft.AspNetCore",
        "MassTransit",
        "Npgsql",
    ];

    [Fact]
    public void Domain_depends_on_no_other_layer_and_no_framework()
    {
        AssertNoDependencies(Domain, [
            "ServiceName.Application", "ServiceName.Infrastructure", "ServiceName.Api", "MediatR", "FluentValidation", .. Frameworks]);
    }

    [Fact]
    public void Application_depends_on_neither_Infrastructure_nor_Api_nor_frameworks()
    {
        AssertNoDependencies(Application, ["ServiceName.Infrastructure", "ServiceName.Api", .. Frameworks]);
    }

    [Fact]
    public void Infrastructure_does_not_depend_on_Api()
    {
        AssertNoDependencies(Infrastructure, ["ServiceName.Api", "Microsoft.AspNetCore.Mvc", "Microsoft.AspNetCore.Routing"]);
    }

    private static void AssertNoDependencies(Assembly assembly, string[] forbidden)
    {
        var result = Types.InAssembly(assembly).ShouldNot().HaveDependencyOnAny(forbidden).GetResult();

        Assert.True(
            result.IsSuccessful,
            $"{assembly.GetName().Name} must not reference {string.Join(", ", forbidden)}. Offending types: "
                + string.Join(", ", result.FailingTypeNames ?? []));
    }
}
