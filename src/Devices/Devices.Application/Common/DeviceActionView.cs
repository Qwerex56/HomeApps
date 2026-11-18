namespace Devices.Application.Common;

public sealed record DeviceActionView(string Name, string Description, bool ReadOnly, bool Confirm, bool AutoApprove);
