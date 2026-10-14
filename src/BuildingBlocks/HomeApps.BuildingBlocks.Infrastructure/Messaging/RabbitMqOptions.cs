using System.ComponentModel.DataAnnotations;

namespace HomeApps.BuildingBlocks.Infrastructure.Messaging;

/// <summary>Configuration section <c>RabbitMq</c>.</summary>
public sealed class RabbitMqOptions
{
    public const string SectionName = "RabbitMq";

    [Required]
    public string Host { get; set; } = "localhost";

    [Range(1, 65535)]
    public ushort Port { get; set; } = 5672;

    [Required]
    public string VirtualHost { get; set; } = "/";

    [Required]
    public string Username { get; set; } = "guest";

    [Required]
    public string Password { get; set; } = "guest";
}
