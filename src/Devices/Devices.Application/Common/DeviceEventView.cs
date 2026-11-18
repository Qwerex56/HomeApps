using System.Text.Json;

namespace Devices.Application.Common;

public sealed record DeviceEventView(Guid Id, string Name, JsonElement? Data, DateTimeOffset OccurredAt, bool IsLocation);
