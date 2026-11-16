using HomeApps.BuildingBlocks.Application.Messaging;

namespace HomeMix.Application.Store.TakeHeart;

/// <summary>Takes back one's "domek"; taking one that was never given changes nothing.</summary>
public sealed record TakeHeartCommand(string Key) : ICommand;
