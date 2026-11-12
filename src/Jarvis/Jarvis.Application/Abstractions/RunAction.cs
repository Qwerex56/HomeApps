using Jarvis.Application.Common;

namespace Jarvis.Application.Abstractions;

/// <summary>A tool call that goes on the card; reads are not sent.</summary>
public sealed record RunAction(ActionView Action) : RunUpdate;
