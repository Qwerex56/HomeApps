using System.Net;
using Auth.Application.Events;

namespace Auth.Infrastructure.Emails;

/// <summary>The seven Auth emails, in Polish (Q12). Each is a few sentences and one link into the Vue app.</summary>
internal static class EmailTemplates
{
    public static EmailMessage Render(AuthEmailRequested email, Uri frontendBaseUrl)
    {
        ArgumentNullException.ThrowIfNull(email);
        ArgumentNullException.ThrowIfNull(frontendBaseUrl);

        var link = Link(email, frontendBaseUrl);
        var (subject, lines) = email.Template switch
        {
            AuthEmailTemplate.Invitation => ("Zaproszenie do HomeApps",
                new[]
                {
                    $"{email.InvitedByEmail ?? "Ktoś"} zaprasza Cię do HomeApps.",
                    "Kliknij link, żeby założyć konto hasłem albo przez Google. Link jest ważny 7 dni.",
                    $"Kod zaproszenia: {email.InvitationCode}",
                }),
            AuthEmailTemplate.BootstrapInvitation => ("Załóż konto właściciela HomeApps",
                new[]
                {
                    "To pierwsze uruchomienie HomeApps. Ten adres jest w konfiguracji jako właściciel serwera.",
                    "Kliknij link, żeby założyć konto. Link jest ważny 7 dni.",
                }),
            AuthEmailTemplate.PasswordReset => ("Reset hasła w HomeApps",
                new[]
                {
                    "Ktoś poprosił o reset hasła do Twojego konta.",
                    "Kliknij link, żeby ustawić nowe hasło. Link jest ważny godzinę.",
                    "Jeśli to nie Ty, zignoruj tę wiadomość: hasło się nie zmieni.",
                }),
            AuthEmailTemplate.PasswordChange => ("Potwierdź zmianę hasła w HomeApps",
                new[]
                {
                    "Poproszono o zmianę hasła do Twojego konta. Nowe hasło zacznie działać dopiero po kliknięciu linku.",
                    "Po potwierdzeniu wylogujemy Cię na wszystkich urządzeniach. Link jest ważny godzinę.",
                    "Jeśli to nie Ty, zignoruj tę wiadomość i zmień hasło.",
                }),
            AuthEmailTemplate.EmailChange => ("Potwierdź nowy adres email w HomeApps",
                new[]
                {
                    "Ten adres ma się stać nowym adresem Twojego konta HomeApps.",
                    "Kliknij link, żeby to potwierdzić. Link jest ważny godzinę.",
                    "Jeśli to nie Ty, zignoruj tę wiadomość: nic się nie zmieni.",
                }),
            AuthEmailTemplate.AccountReactivated => ("Twoje konto HomeApps zostało przywrócone",
                new[]
                {
                    "Twoje konto zostało właśnie przywrócone po zalogowaniu.",
                    "Jeśli to nie Ty, zmień hasło i napisz do administratora serwera.",
                }),
            AuthEmailTemplate.HouseholdOwnershipReceived => ($"Przejęcie domu „{email.HouseholdName}” w HomeApps",
                new[]
                {
                    email.PreviousOwnerErased
                        ? $"Konto dotychczasowego właściciela domu „{email.HouseholdName}” zostało usunięte."
                        : $"Dotychczasowy właściciel domu „{email.HouseholdName}” stracił dostęp do konta.",
                    "Od teraz to Ty zarządzasz tym domem jako właściciel: z aktywnych osób masz w nim najwyższą rolę i najdłuższy staż.",
                    "Własność możesz przekazać dalej w ustawieniach domu.",
                }),
            _ => throw new ArgumentOutOfRangeException(nameof(email), email.Template, "Unknown email template."),
        };

        var text = string.Join("\n\n", link is null ? lines : [.. lines, link.ToString()]);
        var html = string.Concat(lines.Select(l => $"<p>{WebUtility.HtmlEncode(l)}</p>"))
            + (link is null ? string.Empty : $"<p><a href=\"{WebUtility.HtmlEncode(link.ToString())}\">{WebUtility.HtmlEncode(LinkLabel(email.Template))}</a></p>");
        return new EmailMessage(email.To, subject, text, html);
    }

    /// <summary>Where the link from the email lands in the Vue app.</summary>
    public static Uri? Link(AuthEmailRequested email, Uri frontendBaseUrl)
    {
        ArgumentNullException.ThrowIfNull(email);

        if (email.Template == AuthEmailTemplate.HouseholdOwnershipReceived && email.HouseholdId is { } household)
        {
            return new Uri(new Uri(frontendBaseUrl.ToString().TrimEnd('/') + "/"), $"households/{household}");
        }

        if (email.Token is null)
        {
            return null;
        }

        var token = Uri.EscapeDataString(email.Token);
        var path = email.Template switch
        {
            AuthEmailTemplate.Invitation or AuthEmailTemplate.BootstrapInvitation => $"invite/{token}",
            AuthEmailTemplate.PasswordReset => $"reset-password?token={token}",
            AuthEmailTemplate.PasswordChange => $"confirm-password?token={token}",
            AuthEmailTemplate.EmailChange => $"confirm-email?token={token}",
            _ => null,
        };
        return path is null ? null : new Uri(new Uri(frontendBaseUrl.ToString().TrimEnd('/') + "/"), path);
    }

    private static string LinkLabel(AuthEmailTemplate template) => template switch
    {
        AuthEmailTemplate.Invitation or AuthEmailTemplate.BootstrapInvitation => "Załóż konto",
        AuthEmailTemplate.PasswordReset => "Ustaw nowe hasło",
        AuthEmailTemplate.HouseholdOwnershipReceived => "Otwórz dom",
        _ => "Potwierdź",
    };
}
