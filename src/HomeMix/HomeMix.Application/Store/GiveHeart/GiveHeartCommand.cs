using HomeApps.BuildingBlocks.Application.Messaging;

namespace HomeMix.Application.Store.GiveHeart;

/// <summary>Gives the entry a "domek" (homemix-2.md 2.2): once per person, giving it again changes nothing.</summary>
public sealed record GiveHeartCommand(string Key) : ICommand;
