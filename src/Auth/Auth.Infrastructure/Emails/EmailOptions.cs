using System.ComponentModel.DataAnnotations;

namespace Auth.Infrastructure.Emails;

/// <summary>Section <c>Auth:Email</c>. Without <see cref="Smtp"/> host, Development logs the emails and production does not start (Q11).</summary>
public sealed class EmailOptions
{
    public const string SectionName = "Auth:Email";

    /// <summary>The Vue app; links in emails point there, e.g. <c>https://home.example.com</c>.</summary>
    [Required]
    public Uri? FrontendBaseUrl { get; set; }

    [Required]
    [EmailAddress]
    public string From { get; set; } = "homeapps@localhost";

    public string FromName { get; set; } = "HomeApps";

    public SmtpOptions Smtp { get; set; } = new();
}

public sealed class SmtpOptions
{
    public string? Host { get; set; }

    public int Port { get; set; } = 587;

    public string? Username { get; set; }

    /// <summary>From a secret, never committed.</summary>
    public string? Password { get; set; }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(Host);
}
