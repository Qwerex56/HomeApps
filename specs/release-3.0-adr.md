# ADR: HomeApps 3.0

Spec: [`specs/release-3.0.md`](release-3.0.md) · Data: 2026-11-25 · Status: `accepted` · Target repo: `../HomeApps`
Wersja docelowa: 3.0.0. Pluginy i dodatki (`HomeApps.Todo`, `HomeApps.Chat`, `HomeApps.Notes`, `HomeApps.Desktop`) mają osobne zmiany w swoich repozytoriach (wersjonowanie API, `@home-apps/*`); ich budowa to osobne `/run-feature --repo`.

## 0. Odpowiedzi właściciela (2026-11-25)
- **Okno zgodności `/v1` (2.2):** cały cykl 3.x; stare trasy działają obok `/v1` do wydania 4.0, z nagłówkami `Deprecation: true` i `Sunset`.
- **Historia po usunięciu urządzenia (2.10):** dane zostają do końca zwykłej retencji; bez ręcznego kasowania w UI.
- **Strefa istniejących domów (2.11):** domyślnie z konta właściciela domu (nie stałe Europe/Warsaw). Zmiana względem szkicu, patrz Decyzja 5 poniżej.
- Poprawki po Devil's Advocate (1 runda, 6 HIGH, 4 MED) są zastosowane; druga runda DA nie była robiona (patrz sekcja 3, ryzyka otwarte).

## Etapy (tabela zbiorcza)

Źródło: pola „Etap” w sekcji 2. Uruchamianie: `/run-feature @specs/release-3.0.md --base master "Stage N"`. Etapy idą po kolei (0, 1, 2, 3, 4); wymagania w jednym etapie oznaczone „równolegle” nie zależą od siebie, ale i tak trafiają do jednej gałęzi `feat/release-3.0-stage-N`.

| Etap | Wymagania | Zakres | Zależy od |
|---|---|---|---|
| Stage 0 | 2.2, 2.1, 2.17 (front) | Wersjonowanie `/v1` w BuildingBlocks i wszystkich serwisach (podwójna trasa, wyjątki, nagłówki `Deprecation`/`Sunset`), front i klienci wewnętrzni na ścieżki `/v1`; wersja 3.0.0 wszędzie, sprawdzenie sekretów przy starcie; `@home-apps/*` w froncie | master |
| Stage 1 | 2.9, 2.10, 2.11, 2.13, 2.14, 2.15, 2.20 (backend) | Gość widzi osoby w domu; usuwanie urządzeń; strefa domu (Households, Contracts, Accounts); centrum powiadomień (Notifications); nazwy i szukanie rozmów JARVIS; awatar z pliku (Accounts); rozbicie plików na folder per komenda (Auth, Accounts, Households) | Stage 0 |
| Stage 2 | 2.3, 2.16, 2.18 | „O serwerze” (agregacja `/health/version`); wygasanie notatek (HomeMix `ExpiresAt`); odporność na niedostępny plugin i lokalny dev | Stage 0, 1 |
| Stage 3 | 2.5–2.8, 2.12, 2.20 (testy frontu) | Siatka i paleta widżetów; „Aplikacje domu”; testy komponentów stron z nieodwracalnymi akcjami | Stage 0, 1 |
| Stage 4 | 2.4, 2.19 | Workflow GitHub Actions (wydanie), dokumentacja (`docs/`, CHANGELOG „Upgrading”, wdrożenie 2.4 → 3.0) | Stage 0-3 |

Uwagi dla Coordinatora:
- Etapy mają jedną gałąź i jeden PR każdy. Stage 1 jest największy; jeśli recenzja będzie za duża, podziel go w obrębie etapu (np. 2.20 osobno), nie przenoś wymagań między etapami.
- Stage 1 dotyka tych samych plików Households, które 2.20 rozbija: developer zaczyna od rozbicia (2.20), dopiero potem zmiany 2.9 i 2.11.
- Wymagania 2.2, 2.17 i 2.18 mają też część w repozytoriach `../HomeApps.Todo`, `.Chat`, `.Notes`, `.Desktop`. Ten ADR opisuje kontrakt; ich zmiany to osobne przebiegi `/run-feature ... --repo ../HomeApps.<Nazwa>` po Stage 0.
- Poza tabelą: nie ma etapu dla odpowiedzi właściciela; wszystkie jego decyzje są w sekcji 0.

## 1. Resolved Questions & Architectural Decisions (Decyzje i uzasadnienia)

1. **Położenie prefiksu `/v1` i wzorzec Dual-Routing (Req 2.2):**
   * *Decyzja:* Prefiks wersji umieszczamy pod prefiksem routingu serwisów w Traefiku: `/<service>/v1/...` (np. `/auth/v1/...`, `/devices/v1/...`, `/notifications/v1/...`, `/homemix/api/v1/...`, `/jarvis/api/v1/...`, `/plugins/{id}/api/v1/...`) oraz `/<prefix>/v1/...` dla serwisów bez jednolitego prefiksu w Traefiku (`/users/v1/...`, `/admin/users/v1/...`, `/households/v1/...`, `/invites/v1/...`, `/admin/households/v1/...`).
   * *Mechanizm implementacji:* W [`HomeApps.BuildingBlocks.Api`](file:///Users/adrianczubaty/Documents/RiderProjects/HomeApps/src/BuildingBlocks/HomeApps.BuildingBlocks.Api/Endpoints/EndpointExtensions.cs#L25-L36) wprowadzamy helper `app.MapVersionedGroup(prefix)`. Zamiast polegać na pojedynczym prefiksie na poziomie serwisu (co zawodziło dla serwisów wieloprefiksowych jak `Accounts` czy `Households`), helper rejestruje dwie powiązane grupy:
     * Główną grupę: `/{prefix}/v1`
     * Grupę legacy: `/{prefix}` z middleware dodającym nagłówki HTTP `Deprecation: true` oraz `Sunset: Sat, 01 Jan 2028 00:00:00 GMT` (koniec linii 3.x).
     Każdy `IEndpoint` rejestruje trasy na zwróconym builderze wersji, co gwarantuje równoległe wystawienie obu ścieżek bez duplikacji kodu i bez ryzyka 404 dla znanych starych tras.
   * *Uzasadnienie:* Reguły Traefika w [`compose.prod.yaml`](file:///Users/adrianczubaty/Documents/RiderProjects/HomeApps/compose.prod.yaml#L126-L260) bazują na `PathPrefix`. Dual-routing wewnątrz grup endpointów zachowuje separację kontenerów i kompatybilność wsteczną dla starszych klientów (Desktop, urządzenia firmware) w całym cyklu wydań 3.x.
2. **Lista wyjątków od wersjonowania `/v1` (Req 2.2):**
   * *Decyzja:* Bez wersji `/v1` pozostają wyłącznie zasoby niebędące publicznym REST API:
     1. *OAuth/OIDC & Discovery:* `/.well-known/*`, `/auth/oauth/*`, `/auth/external/*` (standardy RFC 8414 / RFC 9728).
     2. *Health Probes:* `/health/live`, `/health/ready`, `/health/version`.
     3. *HomeMix Sandbox:* `/homemix/run/{installationId}/{path}` (zamknięty iframe CSP).
     4. *Manifesty i zasoby UI pluginów:* `/plugins/{id}/manifest`, `/plugins/{id}/ui/*`.
     5. *Protokoły dwukierunkowe i strumienie natywne:* `/devices/link` (WS), `/jarvis/voice` (WS), `/plugins/{id}/hub` (SignalR WS), `/homemix/hub` (WS).
     6. *Serwery MCP:* `/jarvis/mcp`, `/mcp`, `/plugins/{id}/mcp`.
   * *Uzasadnienie:* Zasoby te podlegają zewnętrznym specyfikacjom protokołów lub mechanizmom przeglądarkowym, w których prefiks wersji łamałby interoperacyjność.
3. **Uzgodnienie cyklu życia: Walidacja sekretów (Spec 2.1) vs Dual-Routing (Spec 2.2):**
   * *Decyzja:* Weryfikacja obecności `DEVICES_CLIENT_SECRET` i `HOMEMIX_CLIENT_SECRET` następuje poprzez `.AddOptions<TOptions>().ValidateDataAnnotations().ValidateOnStart()` na etapie `builder.Build()`. Jeśli zmienne są puste, aplikacja rzuca `OptionsValidationException` i przerywa proces z kodem błędu, zanim wykona się jakakolwiek migracja bazy danych (`Database.MigrateAsync()`). Dual-routing aktywuje się dopiero po poprawnym przejściu fazy startowej i podniesieniu middleware'u ASP.NET Core.
   * *Uzasadnienie:* Weryfikacja fail-fast chroni bazę danych przed częściowym zaaplikowaniem schematu przy błędnej konfiguracji, a dual-routing gwarantuje brak 404 w czasie działania serwisu.
4. **Architektura „O serwerze” (Req 2.3, F8):**
   * *Decyzja:* Komponentem agregującym jest `Accounts.Api` pod trasą `GET /admin/users/v1/server/about`. Każdy serwis w BuildingBlocks ([`HomeAppsApiExtensions.cs`](file:///Users/adrianczubaty/Documents/RiderProjects/HomeApps/src/BuildingBlocks/HomeApps.BuildingBlocks.Api/HomeAppsApiExtensions.cs#L78-L84)) wystawia endpoint `GET /health/version` zwracający wersję zestawu oraz listę zaaplikowanych migracji EF Core (`Database.GetAppliedMigrationsAsync()`). `Accounts` odpytuje pozostałe serwisy po sieci Docker (`http://auth:8080/health/version`, etc.), pobiera stan pluginów z `/plugins.json` i asynchronicznie (cache in-memory na 1 h z timeoutem 3 s na zapytanie GitHub API i fallbackiem na `isUpdateAvailable: null`) sprawdza najnowszy tag na GitHubie.
   * *Uzasadnienie:* Całkowity brak bezpośrednich połączeń między bazami danych serwisów; każdy serwis zachowuje suwerenność nad swoim schematem.
5. **Strefa czasowa domu (Req 2.11, F2), decyzja właściciela: z konta właściciela domu:**
   * *Decyzja:* `Household.TimeZone` jest nullable (ustawiana jawnie przez Admina). Strefa efektywna = `Household.TimeZone ?? ownerUserCopy.TimeZone ?? "Europe/Warsaw"`. Dzięki temu dom bez jawnej strefy podąża za strefą aktualnego właściciela, także po przekazaniu domu. Strefa konta już istnieje w Accounts (`Accounts.Domain/Users/TimeZoneId.cs`, `Profile.cs`), ale nie jest w `UserProfileUpdated` ani `UserCopy`: `UserProfileUpdated` dostaje opcjonalne `TimeZone` (zgodne wstecz), `UserCopy` pole `TimeZone`, a `Accounts` przy starcie 3.0 jednorazowo publikuje `UserProfileUpdated` dla istniejących kont, by wypełnić kopie w `Households` (bez joinów między bazami).
   * *Uzasadnienie:* Spec 2.11 i edge-case „strefa właściciela zmienia się po przekazaniu domu”. Brak cross-database queries. Europe/Warsaw tylko jako ostatnia rezerwa.
   * *Wpływ na blueprint 2.11 poniżej:* tam, gdzie jest `TimeZone = "Europe/Warsaw"` i migracja z wartością domyślną, obowiązuje ta decyzja: kolumna `time_zone` nullable bez domyślnej wartości; `HouseholdView.timeZone` zwraca strefę efektywną plus `timeZoneSource` (`household` | `owner` | `fallback`).
6. **Zgodność wsteczna kontraktu `MemberNameView` dla pluginów i widok gości (Req 2.9, W6):**
   * *Decyzja:* Zachowujemy pełną kompatybilność wsteczną istniejącego endpointu `GET /households/v1/{id:guid}/members/names` ([`MemberEndpoints.cs:34-37`](file:///Users/adrianczubaty/Documents/RiderProjects/HomeApps/src/Households/Households.Api/Endpoints/MemberEndpoints.cs#L34-L37)). Model `MemberNameView` zachowuje właściwość `string Name` (reprezentującą nickname lub displayName) i zostaje rozszerzony o właściwości `HouseholdRole? Role = null` oraz `bool IsActive = true`.
   * *Uzasadnienie:* Zapobiega złamaniu deserializacji w `IHouseholdDirectory` (`HomeApps.Plugins.Hosting`) oraz w zewnętrznych pluginach Todo/Chat (gdzie pole `Name` jest kluczowe), jednocześnie dostarczając role dla widoku gościa w [`src/Web/src/features/households/pages/MainTab.vue`](file:///Users/adrianczubaty/Documents/RiderProjects/HomeApps/src/Web/src/features/households/pages/MainTab.vue#L30), [`MembersTab.vue`](file:///Users/adrianczubaty/Documents/RiderProjects/HomeApps/src/Web/src/features/households/pages/MembersTab.vue) oraz [`SettingsTab.vue`](file:///Users/adrianczubaty/Documents/RiderProjects/HomeApps/src/Web/src/features/households/pages/SettingsTab.vue#L190-L195).
7. **Strategia magazynu awatarów i walidacja relatywnego adresu (Req 2.15, F9):**
   * *Decyzja:* Pliki awatarów zapisujemy w dedykowanym wolumenie Dockera `homeapps_avatars` montowanym w kontenerze `accounts` pod ścieżką `/var/data/avatars/{userId}.bin`. Zmieniamy implementację Value Objectu `AvatarUrl` w `Accounts.Domain.Users`: `AvatarUrl.Value` przechowuje `string` (obsługujący zarówno zewnętrzne `https://...`, jak i lokalne relatywne ścieżki `/users/v1/{userId}/avatar?v={timestamp}`). `POST /users/v1/me/avatar` zwraca adres z identyfikatorem użytkownika `{userId}` (nie z aliasem `/me`), co pozwala innym użytkownikom i pluginom prawidłowo pobierać awatar. Wolumen `homeapps_avatars` zostaje dopisany do `compose.prod.yaml` oraz procedur backupu/restore w `docs/deployment.md`.
   * *Uzasadnienie:* Umożliwia buforowanie awatarów w przeglądarkach i zapobiega sytuacji, w której relatywny adres `/me` wskazywałby na profil aktualnie zalogowanego odbiorcy czatu.
8. **Biblioteka siatki widżetów i wymiary (Korekta Frontend, W1, W2, W7, W8, Req 2.5–2.8):**
   * *Decyzja:* Siatka oparta jest o `grid-layout-plus` z zachowaniem `COLUMNS = 8` w [`layout.ts`](file:///Users/adrianczubaty/Documents/RiderProjects/HomeApps/src/Web/src/features/widgets/layout.ts#L8) i maksymalnej wysokości `max-h = 6`. Niezależna zmiana rozmiaru 2D (Req 2.5) zapisuje dokładne jednostki siatki `w` (1–8) i `h` (1–6) w stanie `BoardView`. Dla kompatybilności z backendem HomeMix funkcja `chooseWidget` mapuje szerokość na najmniejszy pasujący preset (`1-2: Small`, `3-5: Medium`, `6-8: Large`). Paleta dostępnych widżetów (Req 2.6) trafia do prawego wysuwanego drawera (`Drawer` PrimeVue) w `BoardEditor.vue`. Dla $N=1$ (Req 2.8) widżet otrzymuje rozmiar `Medium` (`w: 4`, `h: 3`) i jest centrowany w CSS.
   * *Uzasadnienie:* Zapewnia spójność pomiędzy swobodnym rozmiarem 2D w interfejsie a strukturą danych backendu.
9. **Odporność na 502 przy braku kontenera pluginu (Req 2.18, A2):**
   * *Decyzja:* W [`registry.ts`](file:///Users/adrianczubaty/Documents/RiderProjects/HomeApps/src/Web/src/features/plugins/registry.ts) błąd 502 lub network error przy pobieraniu manifestu nie zwraca `null` (co powodowało całkowite znikanie pluginu), lecz zwraca obiekt zastępczy `{ manifest: { id, name: id, ... }, module: null, status: 'unavailable' }`.
   * *Uzasadnienie:* Pozwala w `PluginsSettings.vue` oznaczyć wtyczkę jako niedostępną, nie zaśmieca konsoli pętlami ponawiania zapytań i zapobiega psuciu menu nawigacji.
10. **Wykorzystanie istniejącej obsługi usuwania urządzeń (Req 2.10, W5):**
    * *Decyzja:* Ponowne wykorzystanie istniejącego soft-delete w agregacie `Device`: `Disable(now)`, endpointu `DELETE /devices/v1/{deviceId}` (`RemoveDeviceHandler`), metody `IDeviceLinks.Drop(deviceId)` oraz eventu integracyjnego `HouseholdDeviceUnpaired(DeviceId, HouseholdId)`. Zamiast dodawać zduplikowane kolumny `IsRemoved`/`RemovedAt`, naprawiamy `GetHouseholdDevicesHandler.cs:24-29`, filtrując urządzenia wyłączone (`!d.IsDisabled`), co powoduje ich natychmiastowe zniknięcie z listy urządzeń domu zgodnie ze Spec 2.10. W `DeviceCard.vue` usuwamy stan `removed` i przycisk „Przywróć” dla satelitów domu.
    * *Uzasadnienie:* Eliminuje duplikację logiki soft-delete w domenie i bazie danych, w pełni realizując wymagania biznesowe.
11. **Kompletny cykl wygasania i czyszczenia dokumentów HomeMix (Req 2.16, F3):**
    * *Decyzja:* Model `AddonDocument` otrzymuje kolumnę `DateTimeOffset? ExpiresAt`. Kontrakt zapisu `PutDocumentRequest` oraz bridge SDK (`data.put`) zostają rozszerzone o opcjonalne pole `expiresAt`. Zapytania `GetDocument` oraz `ListDocuments` w repozytorium HomeMix jawnie filtrują rekordy z `ExpiresAt <= now`, a cykliczny job `PurgeRemovedDataHandler` usuwa fizycznie przeterminowane wiersze z bazy danych.
    * *Uzasadnienie:* Zapewnia natychmiastowe ukrycie wygasłych notatek przed użytkownikiem, niezależnie od interwału uruchamiania joba czyszczącego w tle.
12. **Centrum powiadomień in-app, polityka mutes, retencja i UserErased (Req 2.13, F5):**
    * *Decyzja:* Konsument zdarzenia `NotificationRequested` w module `Notifications` zapisuje encję `InAppNotification` w `NotificationsDbContext` niezależnie od tego, czy urządzenie odbiorcy jest aktywne (`DeliveryDecision.NoDevices`), co gwarantuje dostarczenie powiadomień przy wyłączonym Web Push. Zapis uwzględnia wyciszenia (`Mutes`) oraz deduplikację po `Tag` (nowe powiadomienie z tym samym tagiem zastępuje poprzednie nieprzeczytane). Zapytania pobierające filtrują powiadomienia do ostatnich 30 dni (`CreatedAt >= now - 30d`), a konsument zdarzenia integracyjnego `UserErased` czyści powiadomienia usuniętego użytkownika.
    * *Uzasadnienie:* Zapewnia realizację kryteriów Spec 2.13 bez naruszania prywatności i bez bloatu tabeli powiadomień.
13. **Spójność nazewnictwa paczek SDK (Req 2.17):**
    * *Decyzja:* Rozdzielamy i ujednolicamy zakresy npm: paczka dla dodatków HomeMix to `@home-apps/sdk`, natomiast biblioteka dla wtyczek systemowych to `@home-apps/plugin-sdk`. Wszystkie importy w `src/Web` oraz skrypt `scripts/pack.sh` zostają zaktualizowane do tych jednoznacznych nazw.
    * *Uzasadnienie:* Eliminuje kolizje typów i niejednoznaczności przy budowaniu wtyczek zewnętrznych.

---

## 2. Contract and Data Model + Developer Implementation Blueprint

### Requirement 2.2: Wersjonowanie API (/v1, dual-routing, wyjątki)
* **Owner component:** `HomeApps.BuildingBlocks.Api`, `HomeApps.Plugins.Hosting`, wszystkie serwisy w `src/`, `Web` (`features/*/api.ts`, `servicePaths.ts`), `tools/mac-device-emulator`.
* **Etap:** Stage 0 (Sequential – fundament dla pozostałych serwisów).
* **Endpoints & Dual-Routing:**
  * Wzorzec grup tras:
    * Primary: `GET/POST/PUT/DELETE /{service-or-feature}/v1/{route}`
    * Legacy (okno zgodności 3.x): `GET/POST/PUT/DELETE /{service-or-feature}/{route}`
  * Status & Error codes: Odpowiedzi tras legacy zawierają nagłówki `Deprecation: true` oraz `Sunset: Sat, 01 Jan 2028 00:00:00 GMT`. Brak 404 dla znanych starych tras.
* **Developer Implementation Blueprint:**
  * [`src/BuildingBlocks/HomeApps.BuildingBlocks.Api/Endpoints/EndpointExtensions.cs:25-36`](file:///Users/adrianczubaty/Documents/RiderProjects/HomeApps/src/BuildingBlocks/HomeApps.BuildingBlocks.Api/Endpoints/EndpointExtensions.cs#L25-L36):
    * Wprowadzić metodę rozszerzającą:
      ```csharp
      public static RouteGroupBuilder MapVersionedGroup(this IEndpointRouteBuilder app, string prefix)
      {
          var versioned = app.MapGroup($"{prefix}/v1");
          var legacy = app.MapGroup(prefix).AddEndpointFilter(async (context, next) =>
          {
              context.HttpContext.Response.Headers.Append("Deprecation", "true");
              context.HttpContext.Response.Headers.Append("Sunset", "Sat, 01 Jan 2028 00:00:00 GMT");
              return await next(context);
          });
          return versioned; // lub kompozyt rejestrujący równolegle w obu grupach
      }
      ```
    * Zaktualizować wszystkie klasy `IEndpoint` w `Auth`, `Accounts`, `Households`, `Devices`, `Notifications`, `HomeMix`, `Jarvis`, aby korzystały z `MapVersionedGroup`.
  * [`src/BuildingBlocks/HomeApps.Plugins/Manifest/PluginManifestDocument.cs:34`](file:///Users/adrianczubaty/Documents/RiderProjects/HomeApps/src/BuildingBlocks/HomeApps.Plugins/Manifest/PluginManifestDocument.cs#L34):
    * Zaktualizować domyślną ścieżkę API na `var api = $"{basePath}/api/v1";`.
  * [`src/BuildingBlocks/HomeApps.Plugins.Hosting/`](file:///Users/adrianczubaty/Documents/RiderProjects/HomeApps/src/BuildingBlocks/HomeApps.Plugins.Hosting):
    * Zaktualizować wewnętrzne klienty HTTP (`HouseholdsHttpAccess.cs`, `HouseholdsHttpDirectory.cs`, `DevicesHttpClient.cs`) oraz klientów w `Devices`, `HomeMix`, `Jarvis` do odpytywania tras `/v1`.
  * [`src/Web/src/app/servicePaths.ts:1-18`](file:///Users/adrianczubaty/Documents/RiderProjects/HomeApps/src/Web/src/app/servicePaths.ts#L1-L18):
    * Rozszerzyć wyrażenia regularne o opcjonalne `/v1` (np. `/^\/devices(\/v1)?(\/|$)/`).
  * Wszystkie pliki `src/Web/src/features/*/api.ts`:
    * Zaktualizować wywołania API na trasy z prefiksem `/v1`.
* **Inwarianty:** Brak cichego 404 dla starszych klientów w całym okresie wsparcia linii 3.x.

---

### Requirement 2.3: „O serwerze” (F8)
* **Owner component:** `Accounts.Api`, `Accounts.Application`, `HomeApps.BuildingBlocks.Api`, `Web` (`features/admin`).
* **Etap:** Stage 2 (Parallel po Stage 0).
* **Endpoints:**
  * `GET /health/version` (w każdym serwisie, Anonymous, wewnętrzny port kontenera 8080):
    * Response `200 OK`: `{ "service": "Households", "version": "3.0.0", "migrations": ["20261027..._Initial", "20261125..._HouseholdTimeZone"] }`.
  * `GET /admin/users/v1/server/about` (Accounts.Api, ServerRole: Admin, Owner):
    * Response `200 OK`:
      ```json
      {
        "currentVersion": "3.0.0",
        "latestVersion": "3.0.0",
        "isUpdateAvailable": false,
        "releaseUrl": "https://github.com/qwerex56/HomeApps/releases/tag/V3.0.0",
        "services": [
          { "name": "Auth", "version": "3.0.0", "appliedMigrations": 8, "lastMigration": "20261120_AddDelegations" },
          { "name": "Accounts", "version": "3.0.0", "appliedMigrations": 5, "lastMigration": "20261125_AvatarStorage" },
          { "name": "Households", "version": "3.0.0", "appliedMigrations": 7, "lastMigration": "20261125_HouseholdTimeZone" },
          { "name": "Devices", "version": "3.0.0", "appliedMigrations": 4, "lastMigration": "20261027_InitialDevices" },
          { "name": "Jarvis", "version": "3.0.0", "appliedMigrations": 6, "lastMigration": "20261125_ConversationSearch" },
          { "name": "Notifications", "version": "3.0.0", "appliedMigrations": 3, "lastMigration": "20261125_InAppNotifications" },
          { "name": "HomeMix", "version": "3.0.0", "appliedMigrations": 6, "lastMigration": "20261125_DocumentExpiration" }
        ],
        "plugins": [
          { "id": "todo", "version": "1.0.0", "status": "healthy" },
          { "id": "chat", "version": "1.0.0", "status": "healthy" }
        ]
      }
      ```
    * Błędy: `401 Unauthorized`, `403 auth.forbidden`.
* **Developer Implementation Blueprint:**
  * [`src/BuildingBlocks/HomeApps.BuildingBlocks.Api/HomeAppsApiExtensions.cs:78-85`](file:///Users/adrianczubaty/Documents/RiderProjects/HomeApps/src/BuildingBlocks/HomeApps.BuildingBlocks.Api/HomeAppsApiExtensions.cs#L78-L85):
    * Zarejestrować `app.MapGet("/health/version", ...)` pobierający listę z `db.Database.GetAppliedMigrationsAsync()`.
  * Folder `src/Accounts/Accounts.Application/Admin/GetServerAbout/`:
    * `GetServerAboutQuery.cs`, `GetServerAboutHandler.cs`, `ServerAboutView.cs`.
    * Zapytanie do GitHub Releases z 3-sekundowym timeoutem (`CancellationTokenSource(TimeSpan.FromSeconds(3))`) i bezpiecznym fallbackiem w przypadku braku połączenia internetowego (`isUpdateAvailable: null`).
  * [`src/Accounts/Accounts.Api/Endpoints/AdminEndpoints.cs:24-35`](file:///Users/adrianczubaty/Documents/RiderProjects/HomeApps/src/Accounts/Accounts.Api/Endpoints/AdminEndpoints.cs#L24-L35):
    * Zarejestrować `server/about` pod grupą wersji `users.MapGet("/server/about", ...)`.
  * [`src/Web/src/features/admin/pages/AdminPage.vue:9-18`](file:///Users/adrianczubaty/Documents/RiderProjects/HomeApps/src/Web/src/features/admin/pages/AdminPage.vue#L9-L18):
    * Dodać zakładkę `{ name: 'admin-about', label: 'admin.tabs.about', icon: 'pi pi-info-circle' }` oraz widok `ServerAboutPage.vue`.

---

### Requirement 2.1: Aktualizacja 2.4.1 → 3.0 według instrukcji
* **Owner component:** `Devices.Api`, `HomeMix.Api`, `Auth.Api`, `compose.prod.yaml`, `docs/deployment.md`.
* **Etap:** Stage 0 (Sequential).
* **Mechanizm walidacji:**
  * W [`src/Devices/Devices.Infrastructure/DependencyInjection.cs`](file:///Users/adrianczubaty/Documents/RiderProjects/HomeApps/src/Devices/Devices.Infrastructure/DependencyInjection.cs) oraz [`src/HomeMix/HomeMix.Infrastructure/DependencyInjection.cs`](file:///Users/adrianczubaty/Documents/RiderProjects/HomeApps/src/HomeMix/HomeMix.Infrastructure/DependencyInjection.cs):
    ```csharp
    services.AddOptions<DevicesOptions>()
        .BindConfiguration(DevicesOptions.SectionName)
        .ValidateDataAnnotations()
        .ValidateOnStart();
    ```
  * Adnotacja: `[Required(AllowEmptyStrings = false, ErrorMessage = "Missing required secret: DEVICES_CLIENT_SECRET")]`.
  * Rzucenie `OptionsValidationException` w fazie `builder.Build()`, zanim jakikolwiek kod wywoła migracje.
* **Developer Implementation Blueprint:**
  * W `compose.prod.yaml` zdefiniować wolumeny trwałego magazynu (w tym `homeapps_avatars`).
  * W `docs/deployment.md` opisać procedurę migracji 2.4 → 3.0, procedurę rollbacku oraz backup/restore wolumenu `homeapps_avatars`.

---

### Requirement 2.4: Automatyczny build wydania (GitHub Actions)
* **Owner component:** `.github/workflows/release.yaml`.
* **Etap:** Stage 4 (Sequential).
* **Inwarianty:**
  * Trigger: push gałęzi/tagu `V*.*.*` (np. `V3.0.0`).
  * Krok 1: Weryfikacja zgodności wersji w `Directory.Build.props` i `src/Web/package.json` z nazwą gałęzi.
  * Krok 2: `dotnet build HomeApps.slnx`, `dotnet test`, `npm run build` w `src/Web`.
  * Krok 3: Budowanie obrazów Dockera: `docker compose -f compose.prod.yaml build`.
  * Krok 4: Utworzenie GitHub Release z wyciągniętymi notatkami z `CHANGELOG.md`.

---

### Requirements 2.5–2.8: Widżety – wymiary, paleta i zachowanie siatki
* **Owner component:** `Web` ([`src/features/widgets/`](file:///Users/adrianczubaty/Documents/RiderProjects/HomeApps/src/Web/src/features/widgets), [`src/features/homemix/widgets.ts`](file:///Users/adrianczubaty/Documents/RiderProjects/HomeApps/src/Web/src/features/homemix/widgets.ts)).
* **Etap:** Stage 3 (Parallel z backendem).
* **Kontrakt i stany UI:**
  * **2.5 (W1):** Resize 2D w [`layout.ts`](file:///Users/adrianczubaty/Documents/RiderProjects/HomeApps/src/Web/src/features/widgets/layout.ts): niezależna zmiana szerokości `w` (1–8) i wysokości `h` (1–6). Kontener przekazuje wymiary w pikselach i jednostkach siatki do komponentu potomnego. Dla integracji z backendem HomeMix funkcja `chooseWidget` mapuje szerokość na: Small (1–2), Medium (3–5), Large (6–8).
  * **2.6 (W2):** Paleta dostępnych widżetów w wysuwanym drawerze [`BoardEditor.vue`](file:///Users/adrianczubaty/Documents/RiderProjects/HomeApps/src/Web/src/features/widgets/BoardEditor.vue) (`Drawer` PrimeVue). Usunięcie kafelka z siatki przenosi go do drawera. Drawer umożliwia przeciągnięcie go z powrotem (`drag-and-drop`) oraz alternatywny przycisk „+ Dodaj” (a11y). Widżet na siatce jest ukrywany na palecie lub oznaczany jako użyty.
  * **2.7 (W7):** Gdy w `homemix.json` wtyczka deklaruje `"widgets": []` (lub brak klucza), [`widgets.ts`](file:///Users/adrianczubaty/Documents/RiderProjects/HomeApps/src/Web/src/features/homemix/widgets.ts) filtruje pozycję – brak kafelków zastępczych i brak widżetów na palecie.
  * **2.8 (W8):** Gdy liczba dostępnych widżetów wynosi dokładnie 1, siatka wymusza rozmiar `Medium` (`w: 4`, `h: 3`), a `WidgetGrid.vue` centruje go horyzontalnie w CSS z zachowaniem responsywnego `max-width`.

---

### Requirement 2.9: Goście widzą osoby w domu (W6)
* **Owner component:** `Households.Api`, `Households.Application`, `Web` ([`src/features/households/pages/MainTab.vue`](file:///Users/adrianczubaty/Documents/RiderProjects/HomeApps/src/Web/src/features/households/pages/MainTab.vue#L30), [`MembersTab.vue`](file:///Users/adrianczubaty/Documents/RiderProjects/HomeApps/src/Web/src/features/households/pages/MembersTab.vue), [`SettingsTab.vue`](file:///Users/adrianczubaty/Documents/RiderProjects/HomeApps/src/Web/src/features/households/pages/SettingsTab.vue#L190-L195)).
* **Etap:** Stage 1 (Parallel).
* **Endpoints & Uprawnienia:**
  * Wykorzystanie istniejącego endpointu `GET /households/v1/{id:guid}/members/names` ([`MemberEndpoints.cs:34-37`](file:///Users/adrianczubaty/Documents/RiderProjects/HomeApps/src/Households/Households.Api/Endpoints/MemberEndpoints.cs#L34-L37)).
  * Dostęp: od roli `Guest` wzwyż.
  * Model [`MemberNameView.cs`](file:///Users/adrianczubaty/Documents/RiderProjects/HomeApps/src/Households/Households.Application/Members/ListMemberNames/MemberNameView.cs):
    ```csharp
    public sealed record MemberNameView(
        Guid UserId,
        string Name,
        string? AvatarUrl,
        HouseholdRole? Role = null,
        bool IsActive = true);
    ```
    *Gwarantuje brak regresji dla pluginów i `IHouseholdDirectory` (oczekujących pola `Name`), a jednocześnie dostarcza informację o roli dla frontendu.*
  * **Bezpieczeństwo:** brak pól email, brak metadanych zaproszeń i dat ważności innych kont.
* **UI States:**
  * W `MainTab.vue` i `MembersTab.vue`:
    * Gdy `currentRole === HouseholdRole.Guest`, lista wyświetla wyłącznie imię, awatar i badge roli.
    * Ukryta kolumna akcji (brak przycisków usuwania, zmiany roli, zapraszania, transferu własności).

---

### Requirement 2.10: Usuwanie urządzeń (W5)
* **Owner component:** `Devices.Api`, `Devices.Domain`, `Devices.Application`, `Web` ([`src/features/devices/components/DeviceCard.vue`](file:///Users/adrianczubaty/Documents/RiderProjects/HomeApps/src/Web/src/features/devices/components/DeviceCard.vue#L231-L248)).
* **Etap:** Stage 1 (Sequential w Devices).
* **Endpoint & Handler:**
  * Wykorzystanie istniejącego endpointu `DELETE /devices/v1/{deviceId:guid}` ([`MyDevicesEndpoints.cs:24-25`](file:///Users/adrianczubaty/Documents/RiderProjects/HomeApps/src/Devices/Devices.Api/Endpoints/MyDevicesEndpoints.cs#L24-L25)) i handlera [`RemoveDeviceHandler.cs:10-37`](file:///Users/adrianczubaty/Documents/RiderProjects/HomeApps/src/Devices/Devices.Application/MyDevices/RemoveDevice/RemoveDeviceHandler.cs#L10-L37).
  * Caller: `HouseholdRole.Admin` i `Owner`. `Member`/`Guest` otrzymują `403 devices.forbidden`.
  * Response: `204 NoContent` lub `404 devices.not_found`.
* **Data Model & Inwarianty:**
  * Brak nowych zduplikowanych kolumn `IsRemoved`/`RemovedAt` – wykorzystujemy istniejące `DisabledAt`, `IsDisabled` oraz metodę `device.Disable(now)`.
  * `links.Drop(device.Value.Id)` zamyka aktywne połączenie WebSocket.
  * Zdarzenie integracyjne: `HouseholdDeviceUnpaired(Guid DeviceId, Guid HouseholdId)` (kolejność parametrów: `DeviceId`, `HouseholdId`). Konsumentem jest `Households` (usuwa urządzenie z członków domu) oraz `Auth` (unieważnia tokeny).
  * **Korekta w zapytaniu:** W [`GetHouseholdDevicesHandler.cs:24-29`](file:///Users/adrianczubaty/Documents/RiderProjects/HomeApps/src/Devices/Devices.Application/HouseholdDevices/GetHouseholdDevices/GetHouseholdDevicesHandler.cs#L24-L29) dodajemy filtr wykluczający wyłączone urządzenia: `found = found.Where(d => !d.IsDisabled)`.
  * W `DeviceCard.vue`: usunięcie stanu `removed` i przycisku „Przywróć” dla satelitów domu. Urządzenie po usunięciu całkowicie znika z listy.
  * Potwierdzenie akcji przez `ConfirmDialog` z informacją o konieczności ponownego parowania.

---

### Requirement 2.11: Strefa czasowa domu (F2)
* **Owner component:** `Households.Domain`, `Households.Application`, `Households.Api`, `Households.Infrastructure`, `Web` (`features/households`).
* **Etap:** Stage 1 (Parallel).
* **Endpoints & Model:**
  * Agregat `Household`: właściwość `public string? TimeZone { get; private set; } // null = follow owner (see Decision 5)`.
  * Migracja: `20261125_HouseholdTimeZone` (kolumna `time_zone` nullable w `households.Households`, bez wartości domyślnej; plus `time_zone` nullable w `households.UserCopies`).
  * `PUT /households/v1/{id:guid}/timezone`
    * Request: `{ "timeZone": "Europe/Warsaw" }`.
    * Walidacja: `TimeZoneInfo.FindSystemTimeZoneById(timeZone)`. Błędna strefa: `400 ValidationProblem`.
    * Uprawnienia: `Admin` i `Owner`.
  * `HouseholdView` zwraca pole `timeZone`.
  * Event integracyjny: `HouseholdTimeZoneChanged(Guid HouseholdId, string TimeZone)`.
  * Propagacja: strefa czasowa jest wstrzykiwana do promptu systemowego JARVIS, włączana do `HomeMixContext` dla dodatków oraz przekazywana w nagłówku do zewnętrznych pluginów.

---

### Requirement 2.12: Aplikacje domu w jednym miejscu (F4)
* **Owner component:** `Web` ([`src/features/households/pages/SettingsTab.vue`](file:///Users/adrianczubaty/Documents/RiderProjects/HomeApps/src/Web/src/features/households/pages/SettingsTab.vue), `HouseholdAppsSection.vue`).
* **Etap:** Stage 3.
* **Kontrakt i integracja:**
  * Nowa sekcja w ustawieniach domu łącząca zarządzanie rozszerzeniami:
    1. *Pluginy systemowe:* `/plugins.json` + `GET /plugins/{id}/api/v1/households/{householdId}/enablement` z przełącznikiem aktywacji.
    2. *Dodatki HomeMix:* `GET /homemix/api/v1/households/{householdId}/installations` z akcjami: instaluj, aktualizuj, usuń, konfiguruj widżety.
  * Dostęp: Wyłącznie `Admin` i `Owner` domu.

---

### Requirement 2.13: Centrum powiadomień (F5)
* **Owner component:** `Notifications.Domain`, `Notifications.Application`, `Notifications.Api`, `Notifications.Infrastructure`, `Web` (`features/notifications`).
* **Etap:** Stage 1 (Sequential w Notifications).
* **Data Model & Migracja:**
  * Tabela `notifications.InAppNotifications`:
    * `Id` (`uuid`, PK), `UserId` (`uuid`, NOT NULL), `Source` (`varchar(50)`), `Category` (`varchar(50)`), `Tag` (`varchar(100)`, nullable), `Title` (`varchar(200)`), `Body` (`text`), `Url` (`varchar(500)`), `HouseholdId` (`uuid`), `CreatedAt` (`timestamptz`), `ReadAt` (`timestamptz`, nullable).
  * Indeksy:
    * `IX_InAppNotifications_UserId_CreatedAt` (`UserId`, `CreatedAt DESC`)
    * Indeks częściowy pod badge nieprzeczytanych: `CREATE INDEX IX_InAppNotifications_Unread ON notifications."InAppNotifications" ("UserId") WHERE "ReadAt" IS NULL;`
  * Migracja: `20261125_InAppNotifications`.
* **Logika Biznesowa & Integracja:**
  * Konsument `NotificationRequested`: zapisuje rekord w `InAppNotifications` **niezależnie** od tego, czy `Recipient` posiada zarejestrowane urządzenia push (`DeliveryDecision.NoDevices`).
  * Mutes: jeśli kategoria powiadomienia jest wyciszona przez użytkownika (`Recipient.IsMuted(category)`), powiadomienie in-app nie jest tworzone.
  * Tag replacement: jeśli podano `Tag`, poprzednie nieprzeczytane powiadomienie o tym samym tagu dla danego użytkownika zostaje oznaczone jako przeczytane/zastąpione.
  * Retencja: zapytania filtrują rekordy do ostatnich 30 dni (`CreatedAt >= now - 30d`).
  * Zdarzenie `UserErased`: konsument usuwa wszystkie powiadomienia in-app powiązane z usuniętym użytkownikiem.
* **Endpoints:**
  * `GET /notifications/v1/in-app?limit=50&unreadOnly=false` -> `NotificationListDto` `{ unreadCount: int, items: InAppNotificationView[] }`.
  * `POST /notifications/v1/in-app/{id:guid}/read` -> `204 NoContent`.
  * `POST /notifications/v1/in-app/read-all` -> `204 NoContent`.
* **UI Frontend:**
  * `NotificationBell.vue` w Topbarze z `Badge` (`v-if="unreadCount > 0"`). Kliknięcie otwiera drawer z listą powiadomień, akcją „Oznacz wszystkie jako przeczytane” oraz inwalidacją zapytań TanStack Query.

---

### Requirement 2.14: JARVIS: nazwy i wyszukiwanie rozmów (F10)
* **Owner component:** `Jarvis.Domain`, `Jarvis.Application`, `Jarvis.Api`, `Web` (`features/jarvis`).
* **Etap:** Stage 1 (Parallel).
* **Endpoints:**
  * `PATCH /jarvis/api/v1/conversations/{id:guid}/title`
    * Request: `{ "title": "Projekt kuchni" }` (max 80 znaków).
    * Błędy: `400 ValidationProblem`, `404 jarvis.conversation_not_found`, `403 jarvis.forbidden`.
  * `GET /jarvis/api/v1/conversations?q=kuchnia`
    * Parametr `q`: opcjonalny filtr tekstowy (max 100 znaków, escapowanie znaków specjalnych `%`/`_`), filtruje po tytule i treści wiadomości w konwersacjach użytkownika.
* **Developer Implementation Blueprint:**
  * [`src/Jarvis/Jarvis.Domain/Conversations/Conversation.cs`](file:///Users/adrianczubaty/Documents/RiderProjects/HomeApps/src/Jarvis/Jarvis.Domain/Conversations/Conversation.cs): Dodać metodę `Rename(string newTitle)`.
  * Folder `src/Jarvis/Jarvis.Application/Conversations/RenameConversation/`: Command, Validator, Handler.
  * Zaktualizować `ListConversationsQueryHandler` o wyszukiwanie tekstowe.

---

### Requirement 2.15: Awatar z pliku (F9)
* **Owner component:** `Accounts.Domain`, `Accounts.Application`, `Accounts.Api`, `Accounts.Infrastructure`, `compose.prod.yaml`, `docs/deployment.md`, `Web` (`features/account`).
* **Etap:** Stage 1 (Sequential w Accounts).
* **Data & Storage:**
  * Wolumen Dockera `homeapps_avatars` montowany w `/var/data/avatars`.
  * [`AvatarUrl.cs`](file:///Users/adrianczubaty/Documents/RiderProjects/HomeApps/src/Accounts/Accounts.Domain/Users/AvatarUrl.cs): Zmiana typu pola `Value` z `Uri` na `string`. Walidacja akceptuje zewnętrzne adresy `https://...` oraz relatywne ścieżki serwera `/users/v1/{userId}/avatar?v={timestamp}`.
* **Endpoints:**
  * `POST /users/v1/me/avatar`
    * Request: `multipart/form-data`, pole `file`.
    * Walidacja: rozmiar <= 2 MB (`2097152` bajtów), weryfikacja magic bytes dla JPEG, PNG, WebP.
    * Zapis pliku: `/var/data/avatars/{userId}.bin`.
    * Response: `200 OK` `{ "avatarUrl": "/users/v1/{userId}/avatar?v={timestamp}" }`.
    * Publikacja zdarzenia `UserProfileUpdated(userId, ..., avatarUrl)`.
  * `GET /users/v1/{userId:guid}/avatar`
    * Anonymous / Bearer.
    * Response: `200 OK` z nagłówkami `Content-Type`, `Cache-Control: public, max-age=86400, stale-while-revalidate=3600`, `ETag`. Brak awatara: `404 profile.avatar_not_found`.
* **UI Frontend:**
  * `AvatarUpload.vue`: walidacja klienta (2 MB, JPG/PNG/WebP), komunikaty błędów w Toast.
  * `UserAvatar.vue`: obsługa błędu 404 przez automatyczny fallback na inicjały.

---

### Requirement 2.16: Notatki: wygasłe są usuwane (F3)
* **Owner component:** `HomeMix.Domain`, `HomeMix.Application`, `HomeMix.Api`, `HomeMix.Infrastructure`, `@home-apps/sdk`, dodatek `HomeApps.Notes`.
* **Etap:** Stage 2 (Parallel).
* **Kontrakt zapisu i odczytu:**
  * [`PutDocumentRequest.cs`](file:///Users/adrianczubaty/Documents/RiderProjects/HomeApps/src/HomeMix/HomeMix.Api/Requests/PutDocumentRequest.cs):
    ```csharp
    public sealed record PutDocumentRequest(JsonElement Value, int? Version, DateTimeOffset? ExpiresAt = null);
    ```
  * `@home-apps/sdk` oraz `useMixHost.ts`: rozszerzenie metody `data.put(key, value, { version, expiresAt })`.
  * `IDocumentRepository` / `GetDocumentHandler` / `ListDocumentsHandler`: jawny warunek `Where(d => d.ExpiresAt == null || d.ExpiresAt > now)`, aby wygasłe dokumenty nigdy nie były zwracane do klienta.
* **Data Model & Migracja:**
  * Tabela `homemix.AddonDocuments`: kolumna `ExpiresAt` (`timestamptz`, nullable).
  * Indeks: `CREATE INDEX IX_AddonDocuments_ExpiresAt ON homemix."AddonDocuments" ("ExpiresAt") WHERE "ExpiresAt" IS NOT NULL;`.
  * Migracja: `20261125_DocumentExpiration`.
* **Cykliczne czyszczenie:**
  * [`PurgeRemovedDataHandler.cs`](file:///Users/adrianczubaty/Documents/RiderProjects/HomeApps/src/HomeMix/HomeMix.Application/Data/PurgeRemovedData/PurgeRemovedDataHandler.cs) wykonuje:
    `DELETE FROM homemix."AddonDocuments" WHERE "ExpiresAt" IS NOT NULL AND "ExpiresAt" <= NOW();`

---

### Requirement 2.17: Front: wspólne biblioteki (@home-apps)
* **Owner component:** Monorepo package registry / npm scopes.
* **Etap:** Stage 0 (Sequential dla frontendu).
* **Zakres:**
  * Rozdzielenie paczek: `@home-apps/sdk` dla dodatków HomeMix oraz `@home-apps/plugin-sdk` dla wtyczek systemowych.
  * Aktualizacja importów w `src/Web` oraz skryptu `scripts/pack.sh`.
  * Publikacja paczek z tokenami stylów do rejestru npm/GitHub Packages i usunięcie zależności `file:*.tgz` w repozytoriach `HomeApps.Todo`, `HomeApps.Chat`, `HomeApps.Notes`.

---

### Requirement 2.18: Plugin w lokalnym devie bez 502 (A2)
* **Owner component:** `Web` ([`src/features/plugins/registry.ts`](file:///Users/adrianczubaty/Documents/RiderProjects/HomeApps/src/Web/src/features/plugins/registry.ts)), `scripts/pack.sh`, `docs/development.md`.
* **Etap:** Stage 2.
* **Odporność:**
  * W `registry.ts`: gdy zapytanie `/plugins/{id}/manifest` zwróci błąd 502 lub network error, funkcja `loadPlugin` zwraca obiekt `{ manifest: { id, name: id, ... }, module: null, status: 'unavailable' }`.
  * Plugin jest oznaczony jako niedostępny w `PluginsSettings.vue`, pomijany w menu głównym, a w konsoli nie występują zapętlenia zapytań.
  * Skrypt `scripts/pack.sh` podbija wersję bazową pakietów NuGet `HomeApps.Contracts` i BuildingBlocks przy każdej zmianie kontraktów.

---

### Requirement 2.19: Dokumentacja (A3)
* **Owner component:** `docs/README.md`, `docs/*.md`.
* **Etap:** Stage 4.
* **Zakres:**
  * Zaktualizowany spis treści i ścieżka wejścia w [`docs/README.md`](file:///Users/adrianczubaty/Documents/RiderProjects/HomeApps/docs).
  * Kompleksowy opis stanu docelowego v3.0 (architektura Devices, JARVIS, Voice, wersjonowanie `/v1`, procedura upgrade).

---

### Requirement 2.20: Porządki bez zmiany zachowania (F7, F6)
* **Owner component:** `Auth.Application`, `Accounts.Application`, `Households.Application`, `Web`.
* **Etap:** Stage 1 (Backend refactor) & Stage 3 (Frontend tests).
* **Pełna lista wielokomendowych plików do rozbicia na foldery (Command/Validator/Handler):**
  1. [`src/Households/Households.Application/Households/Households.cs`](file:///Users/adrianczubaty/Documents/RiderProjects/HomeApps/src/Households/Households.Application/Households/Households.cs):
     * `CreateHousehold`, `ArchiveHousehold`, `RestoreHousehold`, `GetMyHouseholds`, `GetHousehold`, `UpdateHousehold`, `AcknowledgeOwnership`, `RegenerateGuestLink`, `DisableGuestLink`.
  2. [`src/Households/Households.Application/Members/Members.cs`](file:///Users/adrianczubaty/Documents/RiderProjects/HomeApps/src/Households/Households.Application/Members/Members.cs):
     * `ChangeMemberRole`, `LeaveHousehold`, `SetNickname`, `SetGuestAccess`, `RemoveMember`, `TransferOwnership`, `RemoveExpiredGuests`.
  3. [`src/Households/Households.Application/Invites/Invites.cs`](file:///Users/adrianczubaty/Documents/RiderProjects/HomeApps/src/Households/Households.Application/Invites/Invites.cs):
     * `CreateInvite`, `AcceptInvite`, `RevokeInvite`, `ListHouseholdInvites`, `GetInviteByCode`.
  4. [`src/Accounts/Accounts.Application/MyAccount/MyAccount.cs`](file:///Users/adrianczubaty/Documents/RiderProjects/HomeApps/src/Accounts/Accounts.Application/MyAccount/MyAccount.cs):
     * `GetMyProfile`, `UpdateProfile`, `DisableAccount`.
  5. [`src/Accounts/Accounts.Application/Admin/ManageUser.cs`](file:///Users/adrianczubaty/Documents/RiderProjects/HomeApps/src/Accounts/Accounts.Application/Admin/ManageUser.cs):
     * `DisableUser`, `EnableUser`, `UpdateUserProfile`.
  6. [`src/Auth/Auth.Application/Admin/Administration.cs`](file:///Users/adrianczubaty/Documents/RiderProjects/HomeApps/src/Auth/Auth.Application/Admin/Administration.cs):
     * `ListUsers`, `GetUser`, `ChangeUserRole`.
  * Wymóg: 100% zgodności istniejących testów jednostkowych i integracyjnych bez modyfikacji asercji.
* **Testy komponentów frontendu (F6):**
  * Testy Vitest dla akcji nieodwracalnych: przekazanie własności domu, zmiana roli na gościa, anulowanie zaproszenia, wyłączenie konta, potwierdzenie akcji JARVIS.

---

## 3. Risks & Test Scenarios

### Główne ryzyka architektoniczne
1. **Regresja starszych klientów API (Desktop/satelity):**
   * *Ryzyko:* Urządzenia lub Desktop odpytują trasy bez prefiksu wersji.
   * *Mitygacja:* Dual-routing helper w BuildingBlocks rejestruje ścieżki legacy z nagłówkiem `Deprecation: true`; testy integracyjne sprawdzają identyczne działanie obu wariantów.
2. **Niespójność migracji bazy przy aktualizacji 2.4.1 → 3.0:**
   * *Ryzyko:* Start aplikacji produkcyjnej z brakującymi nowymi sekretami.
   * *Mitygacja:* `ValidateOnStart()` przerywa start procesu przed migracją w przypadku braku wymaganych zmiennych; smoke-test na zanonimizowanym zrzucie bazy produkcyjnej.
3. **Przepełnienie dysku przez awatary użytkowników:**
   * *Ryzyko:* Wgrywanie wielu dużych plików binarnych.
   * *Mitygacja:* Twardy limit rozmiaru pliku do 2 MB, weryfikacja magic bytes, dedykowany wolumen Docker `homeapps_avatars`.
4. **Wyścig wielodostępny przy usuwaniu urządzenia podczas aktywnego połączenia WebSocket:**
   * *Ryzyko:* Satelita wysyła ramki w trakcie operacji usuwania przez administratora.
   * *Mitygacja:* `device.Disable(now)` + synchroniczne `links.Drop(deviceId)` natychmiast przerywają połączenie TCP/WS, a `HouseholdDeviceUnpaired` unieważnia tokeny w `Auth`.

### Kluczowe scenariusze testowe
1. **Dual-Routing & Deprecation Headers:**
   * Wywołanie `GET /devices/v1/households/{id}` -> `200 OK`.
   * Wywołanie `GET /devices/households/{id}` -> `200 OK` + nagłówki `Deprecation: true`, `Sunset`.
2. **Startup Secret Guard:**
   * Start `Devices` z pustym `DEVICES_CLIENT_SECRET` -> natychmiastowe zakończenie procesu z kodem błędu, brak wykonanych migracji w bazie danych.
3. **Usunięcie urządzenia:**
   * Given: Sparowane urządzenie aktywne na `/devices/link`.
   * When: Admin wykonuje `DELETE /devices/v1/{deviceId}`.
   * Then: WebSocket zostaje zamknięty, urządzenie znika z wyników `GET /devices/v1/households/{id}`, a historia telemetryczna w `/events` pozostaje w bazie.
4. **Centrum powiadomień bez pusha:**
   * Given: Powiadomienie wysłane do użytkownika bez zarejestrowanych urządzeń push (`NoDevices`).
   * When: Użytkownik odpytuje `GET /notifications/v1/in-app`.
   * Then: Powiadomienie jest widoczne na liście, `unreadCount` wynosi 1; po wywołaniu `POST /read-all` licznik spada do 0.
5. **Wygasanie notatek HomeMix:**
   * Wstawienie notatki z `ExpiresAt = now - 1 min`. Zapytanie `GET /homemix/api/v1/...` natychmiast nie zwraca rekordu, a uruchomienie joba `PurgeRemovedDataHandler` fizycznie usuwa wiersz z bazy.

---

## 4. Rejected & Deferred Scope

* **Wersjonowanie API w nagłówkach HTTP (Header-based versioning):** Odrzucone na poziomie specyfikacji biznesowej (decyzja właściciela: wersjonowanie realizowane jest wyłącznie przez prefiks ścieżki `/v1`).
* **Nowe moduły i klocki z roadmapy (Planer, Zakupy, HomeMix H1–H5, historia odczytów Devices, ESP):** Wyłączone z wydania 3.0. Trafią do osobnych wydań (3.1+).
* **Wielojęzyczność UI ponad język polski:** Odrzucona w 3.0 – komunikaty i interfejs pozostają wyłącznie w języku polskim (`pl.json`).
* **End-to-End Encryption w czacie:** Pozostaje w modelu szyfrowanych kopii zapasowych (zgodnie z planem C14).
* **Rejestr obrazów Docker w CI/CD (GitHub Container Registry):** Odłożony – publikacja dotyczy pakietu GitHub Release; lokalny serwer buduje obrazy po `git pull`.

---

## 5. Pytania biznesowe: rozstrzygnięte
Patrz sekcja 0 (okno /v1: cały cykl 3.x; historia urządzeń: do końca retencji; strefa: z konta właściciela). Brak otwartych pytań dla właściciela.

---

### Changelog rewizji (Revision v3 po DA Review)
- **Req 2.2 (HIGH-1):** Zastąpiono nieskuteczny routing globalny mechanizmem `MapVersionedGroup(prefix)` w `BuildingBlocks.Api` dla wszystkich klas `IEndpoint`.
- **Req 2.9 (HIGH-2):** Zachowano właściwość `Name` w `MemberNameView` (ochrona kontraktu pluginów i `IHouseholdDirectory`), dodano opcjonalne pole `Role`; skorygowano odwołania UI na `MainTab.vue`, `MembersTab.vue` i `SettingsTab.vue`.
- **Req 2.10 (HIGH-3):** Wyeliminowano zbędną duplikację kolumn i endpointów – wykorzystano istniejące `device.Disable(now)`, `links.Drop()`, `DELETE /devices/{deviceId}` oraz poprawiono kolejność argumentów w `HouseholdDeviceUnpaired(DeviceId, HouseholdId)`; odfiltrowano `!d.IsDisabled` w `GetHouseholdDevicesHandler`.
- **Req 2.15 & 2.1 (HIGH-4):** Zmieniono typ `AvatarUrl.Value` na `string` i wprowadzono ścieżkę `/users/v1/{userId}/avatar?v={timestamp}` zamiast relatywnego `/me`; dodano wolumen `homeapps_avatars` do `compose.prod.yaml` i procedur backupu w `docs/deployment.md`.
- **Req 2.16 (HIGH-5):** Dodano pole `ExpiresAt` do kontraktu `PutDocumentRequest` i SDK bridge; dodano natychmiastowe filtrowanie wygasłych dokumentów w `IDocumentRepository` przed uruchomieniem joba `PurgeRemovedDataHandler`.
- **Req 2.13 (HIGH-6):** Zdefiniowano zapis `InAppNotification` w obsłudze `NotificationRequested` nawet przy `DeliveryDecision.NoDevices`, uwzględniono `Mutes`, deduplikację po `Tag`, 30-dniową retencję oraz czyszczenie przy `UserErased`.
- **Req 2.11 (MED-7):** Wycofano niemożliwy cross-database join w migracji `Households` na rzecz wartości domyślnej `"Europe/Warsaw"`; dodano propagację strefy do promptu JARVIS, `HomeMixContext` i nagłówków pluginów.
- **Req 2.17 & 2.18 (MED-8):** Ujednolicono nazewnictwo `@home-apps/sdk` i `@home-apps/plugin-sdk`; dodano odporność w `registry.ts` zwracającą status `unavailable` zamiast `null` przy błędzie 502.
- **Req 2.5–2.8 & 2.2 (MED-9):** Doprecyzowano wymiary siatki (8 kolumn, wysokość 1–6) oraz mapowanie presetów dla HomeMix; dodano klientów wewnętrznych (`PluginManifestDocument.cs`, `HouseholdsHttpAccess`, `DevicesHttpClient`, emulator) do Stage 0.
- **Req 2.20 & 2.4 (MED-10):** Uzupełniono pełną listę klas do rozbicia w `Households.cs`, `Members.cs`, `Invites.cs`, `MyAccount.cs`, `ManageUser.cs`, `Administration.cs` oraz dodano krok budowania obrazów Dockera w `.github/workflows/release.yaml`.
