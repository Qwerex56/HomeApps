# HomeApps

Serce inteligentnego domu: samodzielnie hostowany system dla rodziny albo grupy osób, z kontami, logowaniem i
wspólnymi domami. Na nim powstają kolejne aplikacje (pluginy: pogoda, newsfeed, listy zakupów) i asystent głosowy
JARVIS, który korzysta z tych samych usług przez narzędzia MCP.

Wersja 2.0.0 to rdzeń systemu, napisany od nowa ([CHANGELOG.md](CHANGELOG.md)).

## Co potrafi

- **Logowanie (Auth):** hasło albo Google, tokeny JWT (RS256, klucze publiczne w JWKS), refresh token w ciasteczku
  HttpOnly z rotacją i wykrywaniem kradzieży, lista zalogowanych urządzeń, wylogowanie wszędzie, reset hasła i zmiana
  emaila potwierdzane mailem. Rejestracja tylko z zaproszenia; pierwszy właściciel serwera dostaje je przy starcie.
- **Konta (Accounts):** profil, historia konta, samodzielne wyłączenie i przywrócenie konta, panel administratora
  (wyszukiwanie, edycja, wyłączanie, role User / Admin / Owner, trwałe usunięcie na żądanie).
- **Domy (Households):** dowolna liczba domów na konto, role Owner / Admin / Member / Guest, zaproszenia kodem lub
  linkiem, stały link dla gości, przekazanie własności (także automatyczne), archiwum.
- **Powiadomienia (Notifications):** Web Push na telefon i komputer, także przy zamkniętej aplikacji; godziny ciszy,
  ukrywanie treści na ekranie blokady, wyciszanie domu albo jednej aplikacji. Pluginy wysyłają je przez `INotifier`.
- **Aplikacja (Vue 3, PWA):** wszystko powyżej w przeglądarce i na telefonie, po polsku, jasny i ciemny motyw, tryb
  tabletu na ścianę (`/wall`).

## Jak to jest zbudowane

Cztery mikroserwisy .NET 10 (Minimal APIs, MediatR, FluentValidation, FluentResults, EF Core + PostgreSQL), które
rozmawiają przez RabbitMQ (MassTransit, outbox). Każdy ma bogatą domenę i podział Domain / Application /
Infrastructure / Api. Wspólny kod to mały kit w `src/BuildingBlocks` i kontrakty wiadomości w `src/Contracts`.
Przed wszystkim stoi Traefik; front to Vue 3 + TypeScript, PrimeVue i Tailwind.

- [docs/how-a-service-is-built.md](docs/how-a-service-is-built.md): jak wygląda serwis i jak dodać kolejny
- [docs/plugins.md](docs/plugins.md): pluginy poza tym repo (manifest, uprawnienia, paczki kitu)
- [docs/homemix.md](docs/homemix.md): dodatki HomeMix wgrywane z aplikacji na dom albo konto (piaskownica, most, JARVIS)
- [docs/jarvis.md](docs/jarvis.md): JARVIS, the MCP gateway `/jarvis/mcp` and connecting Claude Code
- [docs/plugin-todo.md](docs/plugin-todo.md): podłączenie pluginu Todo do działającej instancji i jak go sprawdzić
- [docs/plugin-chat.md](docs/plugin-chat.md): podłączenie pluginu Czat (rozmowy, zdjęcia, filmy) i jego kopie zapasowe
- [docs/adr](docs/adr/README.md): decyzje architektoniczne
- [docs/auth.md](docs/auth.md), [docs/accounts.md](docs/accounts.md), [docs/households.md](docs/households.md),
  [docs/notifications.md](docs/notifications.md),
  [docs/web.md](docs/web.md): poszczególne części

## Uruchomienie

**Na produkcji** (jeden serwer, Docker Compose, HTTPS z Let's Encrypt):

```bash
cp .env.prod.example .env      # uzupełnij wartości oznaczone "required"
scripts/generate-signing-key.sh
docker compose -f compose.prod.yaml up -d --build
```

Bez publicznego adresu (CGNAT) stos wychodzi do internetu przez Cloudflare Tunnel, bez otwierania portów: dodaj
`CLOUDFLARE_TUNNEL_TOKEN` do `.env` i uruchom `docker compose -f compose.prod.yaml -f compose.tunnel.yaml up -d --build`.

Szczegóły, kopie zapasowe, aktualizacje i testy wydania: [docs/deployment.md](docs/deployment.md).

**Do pracy nad kodem:** infrastruktura w `compose.dev.yaml`, serwisy z Ridera albo w kontenerach, front z vite pod
https://app.localhost. Opis: [docs/working-with-the-repo.md](docs/working-with-the-repo.md).

## Wykorzystanie AI

Projekt powstaje ze wsparciem modelu językowego. Kod, testy i dokumentację pisze w dużej części Claude (Anthropic),
który pracuje jako narzędzie pod moim nadzorem: architekturę, zakres funkcji i decyzje projektowe ustalam ja,
a każdą zmianę przeglądam i scalam sam.
