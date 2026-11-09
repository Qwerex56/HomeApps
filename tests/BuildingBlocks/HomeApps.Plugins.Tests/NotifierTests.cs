using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.Contracts.Notifications;
using HomeApps.Plugins.Manifest;
using HomeApps.Plugins.Notifications;
using NSubstitute;

namespace HomeApps.Plugins.Tests;

public sealed class NotifierTests
{
    private readonly IEventBus _bus = Substitute.For<IEventBus>();

    private Notifier Notifier => new(
        TestManifests.Valid with { Notifications = [new PluginNotificationCategory("reminder", new LocalizedText("Przypomnienia", "Reminders"))] },
        _bus);

    [Fact]
    public async Task The_plugin_id_is_the_source_and_people_are_named_once()
    {
        var anna = Guid.NewGuid();

        await Notifier.NotifyAsync(new PluginNotification([anna, anna], "reminder", "Zadania", "Wynieś śmieci", "/homes"), CancellationToken.None);

        await _bus.Received(1).PublishAsync(
            Arg.Is<NotificationRequested>(n => n.Source == "todo" && n.Category == "reminder" && n.UserIds.Count == 1 && n.UserIds[0] == anna),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Nobody_to_tell_sends_nothing()
    {
        await Notifier.NotifyAsync(new PluginNotification([], "reminder", "Zadania", "x", "/homes"), CancellationToken.None);

        await _bus.DidNotReceiveWithAnyArgs().PublishAsync<NotificationRequested>(default!, default);
    }

    [Fact]
    public async Task A_category_the_manifest_does_not_declare_is_a_bug() =>
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Notifier.NotifyAsync(new PluginNotification([Guid.NewGuid()], "spam", "x", "y", "/"), CancellationToken.None));
}
