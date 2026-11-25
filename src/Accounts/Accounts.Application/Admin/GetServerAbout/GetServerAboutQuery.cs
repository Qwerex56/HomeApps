using HomeApps.BuildingBlocks.Application.Messaging;

namespace Accounts.Application.Admin.GetServerAbout;

/// <summary>"About the server" (Admin and Owner): HomeApps' version, the newest release, every service's version and migrations, the plugins.</summary>
public sealed record GetServerAboutQuery : IQuery<ServerAboutView>;
