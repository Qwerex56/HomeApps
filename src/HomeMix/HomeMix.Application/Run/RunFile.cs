namespace HomeMix.Application.Run;

/// <summary>A file of an installed add-on, with the network origins its sandbox may reach.</summary>
public sealed record RunFile(Stream Content, string Path, IReadOnlyList<string> Network);
