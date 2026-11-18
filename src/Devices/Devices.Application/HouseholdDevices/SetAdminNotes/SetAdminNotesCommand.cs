using HomeApps.BuildingBlocks.Application.Messaging;

namespace Devices.Application.HouseholdDevices.SetAdminNotes;

/// <summary>A household admin's notes for JARVIS about a device (D6), at most 500 characters; empty clears them. JARVIS reads them from H-3c.</summary>
public sealed record SetAdminNotesCommand(Guid DeviceId, string? Notes) : ICommand;
