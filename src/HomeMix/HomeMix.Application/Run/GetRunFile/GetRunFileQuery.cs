using HomeApps.BuildingBlocks.Application.Messaging;

namespace HomeMix.Application.Run.GetRunFile;

/// <summary>
/// A file for the sandboxed frame, anonymously: the installation id is a UUID and files are only code, data needs a
/// token (docs/homemix.md, "The sandbox"). An empty <paramref name="Path"/> is the add-on's entry page.
/// </summary>
public sealed record GetRunFileQuery(Guid InstallationId, string Path) : IQuery<RunFile>;
