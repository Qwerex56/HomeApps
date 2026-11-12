using System.ComponentModel;
using Microsoft.AspNetCore.Http;
using ModelContextProtocol.Server;

namespace Jarvis.Api.Tests;

[McpServerToolType]
internal sealed class HouseholdsTools(IHttpContextAccessor accessor)
{
    [McpServerTool(Name = "my_households", ReadOnly = true)]
    [Description("Says who is asking.")]
    public string MyHouseholds() => Who.Of(accessor.HttpContext!.Request.Headers.Authorization.ToString());
}
