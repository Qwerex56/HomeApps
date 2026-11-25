# Wydanie HomeApps 3.0

Autor: Qwerex56 (zebrał Claude) · Data: 2026-11-25 · Wersja docelowa: 3.0.0 · Status: gotowa do debaty

Źródła: `plans/features/release-3.0.md`, `plans/features/roadmap-after-2.4.md`. Jedno wdrożenie na koniec (decyzja właściciela 2026-11-25: bez `rc1`). Spec opisuje CO i po czym poznamy, że się udało; JAK ustala `/spec-debate` w `specs/release-3.0-adr.md`. Wymagania, które dotykają kontraktu API (2.2, 2.3), debatują architekci w pierwszej kolejności.

**Zakres (właściciel 2026-11-25):** 3.0 to dokładnie ta spec. Reszta roadmapy (Planer, Zakupy, HomeMix 2.5, historia odczytów Devices, ESP) idzie w osobnych wersjach.

## 1. Cel biznesowy (CO ma powstać)

Produkcja stoi na v2.4.1, a master jest o cały plan dalej: urządzenia i JARVIS domu, głos, nowe sekrety, 14 migracji. 3.0 zbiera to w jedno wydanie razem z poprawkami API i frontu zgłoszonymi przez właściciela i da się je wdrożyć bez zgadywania: kroki, kopie zapasowe i sprawdzenie po starcie są opisane i przetestowane.

**Dla kogo:** właściciel serwera (wdrożenie i wersjonowanie), członkowie domu i goście (front), autorzy pluginów i dodatków (kontrakt API).
**Problem:** duży skok wersji bez instrukcji, brak wersji API (każda zmiana kontraktu łamie klientów), kilka uciążliwych braków w widżetach, urządzeniach i widoku gości.
**Efekt:** aktualizacja 2.4.1 → 3.0 przechodzi według instrukcji na stosie smoke i na produkcji; API ma wersję, od której można utrzymywać zgodność; widżety i urządzenia zachowują się zgodnie z punktami 2.5-2.9.

## 2. Wymagania funkcjonalne i kryteria akceptacji (Given / When / Then)

### 2.1 Aktualizacja 2.4.1 → 3.0 według instrukcji

- **Given** serwer na v2.4.1 z danymi (konta, domy, dodatki HomeMix, czat, todo)
- **When** właściciel wykonuje kroki z `docs/deployment.md` (sekcja „2.4 → 3.0”): kopia zapasowa, nowe wartości w `.env`, pobranie wersji, `up -d --build`
- **Then** wszystkie serwisy startują, migracje przechodzą, dane z 2.4.1 są nienaruszone, a lista kontrolna „co sprawdzić po starcie” przechodzi
- **Given** brakuje nowego wymaganego sekretu (`DEVICES_CLIENT_SECRET`, `HOMEMIX_CLIENT_SECRET`)
- **When** właściciel uruchamia aktualizację
- **Then** start kończy się czytelnym komunikatem o brakującej zmiennej, zanim cokolwiek zmieni się w bazach
- Instrukcja zawiera nowe zmienne, nowy serwis `devices` z bazą `homeapps_devices`, trasę `/jarvis/voice`, opcjonalny `JARVIS_VOICE_URL` i sposób wycofania (przywrócenie kopii).
- CHANGELOG 3.0 ma blok „Upgrading from 2.4.x” z tą samą listą.
- Wersja 3.0.0 jest we wszystkich miejscach (kod, front, obrazy w compose, pakiety).

### 2.2 Wersjonowanie API (ostatni moment)

- **Given** serwisy i pluginy wystawiają dziś API bez wersji
- **When** 3.0 wychodzi
- **Then** każde publiczne API ma prefiks wersji w ścieżce (`/v1`), manifest pluginu wskazuje ścieżki z wersją, front, JARVIS, Desktop i pluginy Todo/Chat używają nowych ścieżek
- **Given** stary klient (np. Desktop sprzed 3.0) woła ścieżkę bez prefiksu
- **When** żądanie dociera do serwera
- **Then** zachowanie jest ustalone w ADR (okno zgodności albo czytelny błąd z informacją o aktualizacji), nigdy cichy 404
- Zasada na przyszłość i sposób zmiany wersji są opisane w `docs/` (co jest zmianą łamiącą).
- Pliki nie-API (UI pluginu, health, dokument OAuth) zostają tam, gdzie są, jeśli ADR tak postanowi; lista wyjątków jest jawna.

### 2.3 „O serwerze” (F8)

- **Given** jestem administratorem serwera
- **When** otwieram „O serwerze” w panelu admina
- **Then** widzę wersję każdego serwisu, pluginów, wersję schematów baz i informację, czy jest nowsze wydanie na GitHubie

### 2.4 Automatyczny build wydania (GitHub Actions)

- **Given** właściciel wypycha gałąź wersji `V<x>.<y>.<z>` (jak `V2.2.0`)
- **When** workflow się uruchamia
- **Then** buduje całość (backend, test, front), buduje obrazy serwisów i publikuje paczkę wydania (artefakt lub wydanie na GitHubie) z numerem wersji z nazwy gałęzi
- **Given** wersja w kodzie nie zgadza się z nazwą gałęzi
- **When** workflow się uruchamia
- **Then** kończy się błędem z wyjaśnieniem
- Wynik to wydanie GitHub (Release) z paczką i notatkami z CHANGELOG (decyzja właściciela); serwer dalej buduje sam po `git pull`. Workflow nie wymaga sekretów poza tymi, które już są w repozytorium.

### 2.5 Widżety: zmiana rozmiaru (W1)

- **Given** edytuję układ Głównej lub tablicy
- **When** zmieniam rozmiar widżetu do niestandardowego
- **Then** zmienia się i szerokość, i wysokość w każdym widżecie (nie tylko wysokość), a widżet pokazuje treść dopasowaną do nowego rozmiaru

### 2.6 Widżety: lista dostępnych i ponowne dodanie (W2)

- **Given** jestem w edycji układu
- **When** usunę widżet z siatki
- **Then** widżet trafia na listę dostępnych obok siatki i mogę go przeciągnąć z powrotem na siatkę
- **Given** widżet jest już na siatce
- **When** patrzę na listę dostępnych
- **Then** nie ma go tam (albo jest oznaczony jako użyty, ADR rozstrzyga)

### 2.7 Dodatek bez widżetów (W7)

- **Given** dodatek HomeMix nie deklaruje żadnych widżetów
- **When** instaluję go i otwieram edycję układu
- **Then** nie powstają dla niego żadne widżety zastępcze; nie ma go na liście dostępnych

### 2.8 Jeden dostępny widżet (W8)

- **Given** w domu jest tylko jeden dostępny widżet
- **When** otwieram Główną
- **Then** widżet ma rozmiar średni, a pozostałe elementy układu skalują się wokół niego

### 2.9 Goście widzą osoby w domu (W6)

- **Given** jestem Guest w domu
- **When** otwieram menu główne domu
- **Then** widzę listę osób w domu (imię i rola), tak jak już widzę uczestników w czacie
- **Then** nie widzę adresów e-mail, zaproszeń ani akcji zarządzania (zmiana ról, usuwanie)

### 2.10 Usuwanie urządzeń (W5)

- **Given** jestem Admin domu, a urządzenie jest sparowane (włączone albo wyłączone)
- **When** wybieram „Usuń urządzenie” i potwierdzam
- **Then** urządzenie znika z listy i przestaje się łączyć; jego klucz przestaje działać (ten sam efekt co przy ponownym sparowaniu); dane historyczne zostają zgodnie z zasadami ADR
- **Given** jestem Member lub Guest
- **When** oglądam urządzenia
- **Then** nie widzę akcji usunięcia

### 2.11 Strefa czasowa domu (F2)

- **Given** dom nie ma jeszcze strefy
- **When** otwieram ustawienia domu
- **Then** strefa jest ustawiona domyślnie z konta właściciela domu, a Admin może ją zmienić, wybierając z listy stref IANA
- Strefa jest dostępna dla pluginów, HomeMix, JARVIS-a (np. „jutro o 7”) i tablicy.

### 2.12 Aplikacje domu w jednym miejscu (F4)

- **Given** jestem Admin domu
- **When** otwieram „Aplikacje domu” w ustawieniach
- **Then** na jednej liście zarządzam pluginami (włącz/wyłącz) i dodatkami HomeMix (instaluj, aktualizuj, usuń, widżety)

### 2.13 Centrum powiadomień (F5)

- **Given** mam konto, a push jest wyłączony albo niedostępny
- **When** klikam dzwonek z licznikiem nieprzeczytanych
- **Then** widzę powiadomienia z ostatnich 30 dni i mogę je oznaczyć jako przeczytane

### 2.14 JARVIS: nazwy i wyszukiwanie rozmów (F10)

- **Given** mam rozmowy z JARVIS-em
- **When** zmieniam nazwę rozmowy albo wpisuję frazę w wyszukiwarce
- **Then** lista pokazuje nową nazwę i rozmowy zawierające frazę (tylko moje)

### 2.15 Awatar z pliku (F9)

- **Given** edytuję profil
- **When** wgrywam obrazek (do 2 MB; JPG, PNG, WebP)
- **Then** awatar pojawia się w aplikacji, czacie i na liście osób; zły typ lub rozmiar daje czytelny komunikat

### 2.16 Notatki: wygasłe są usuwane (F3)

- **Given** dodatek Notatki ma wygasłe notatki
- **When** upływa czas ich ważności
- **Then** znikają z danych (nie tylko z widoku), a kolekcja nie zbliża się do limitu dokumentów

### 2.17 Front: wspólne biblioteki (3a.3)

- **Given** pluginy Todo i Chat oraz dodatki HomeMix
- **When** budowane są na wersji 3.0
- **Then** używają jednej wersji SDK i wspólnych stylów, dystrybuowanych z npm w zakresie `@home-apps`, a lokalne paczki `.tgz` nie są potrzebne

### 2.18 Plugin w lokalnym devie bez 502 (A2)

- **Given** lokalny dev z `compose.dev.yaml` i pluginami Todo/Chat uruchomionymi w kontenerach
- **When** otwieram `https://app.localhost/plugins/chat/manifest` (i todo)
- **Then** dostaję manifest, nie 502; `docs/development.md` opisuje poprawną kolejność uruchamiania i diagnozę 502
- **Given** plugin jest wymieniony w `HOMEAPPS_PLUGINS`, ale jego kontener (i Rider) nie działa
- **When** otwieram aplikację
- **Then** pozostałe pluginy i cała aplikacja działają, a niedostępny plugin jest oznaczony jako niedostępny, bez błędów 502 w konsoli i bez zepsutego menu
- **Given** zmienił się master HomeApps (kontrakty, kit)
- **When** buduję plugin (Todo/Chat) z lokalnego feedu `~/.homeapps/packages`
- **Then** nieaktualna paczka nie przechodzi niezauważona: wersja kitu rośnie przy zmianie, a dokumentacja i błąd wskazują `scripts/pack.sh` (2026-11-25: Chat nie budował się, bo feed miał stary `HomeApps.Contracts` bez `Assistant`)
- *Ustalenie 2026-11-25: 502 na `/plugins/chat/manifest` wynikał z wyłączonego kontenera Chat (nic nie słucha też na porcie Ridera 5410); Todo w kontenerze działa. To zachowanie zamierzone Traefika, więc wymaganie dotyczy odporności frontu i diagnozy w dokumentacji.*

### 2.19 Dokumentacja (A3)

- **Given** nowa osoba lub agent czyta `docs/`
- **When** szuka opisu Devices, JARVIS-a domu, głosu, wersjonowania API, lokalnego deva z pluginami albo wdrożenia
- **Then** znajduje go od `docs/README.md` (spis i ścieżka czytania); opisy mówią o stanie, nie o nazwach etapów; repozytoria `HomeApps.*` linkują do `docs/` HomeApps zamiast kopii

### 2.20 Porządki bez zmiany zachowania (F7, F6)

- Stare pliki z kilkoma komendami/zapytaniami w Auth, Accounts i Households są rozbite na folder per komenda (Command/Validator/Handler); wszystkie dotychczasowe testy przechodzą bez zmian w oczekiwaniach.
- Strony z nieodwracalnymi akcjami (role i przekazanie domu, zaproszenia, bezpieczeństwo konta, panel admina, JARVIS) mają testy komponentów.

## 3. Przepływ użytkownika i oczekiwany UX

1. **Właściciel serwera:** czyta CHANGELOG „Upgrading”, robi kopię, wpisuje nowe sekrety, aktualizuje, przechodzi listę kontrolną; w panelu admina „O serwerze” widzi 3.0.0 wszędzie.
2. **Admin domu:** w „Aplikacje domu” zarządza pluginami i dodatkami; w ustawieniach domu widzi strefę czasową; w zakładce JARVIS może usunąć urządzenie.
3. **Edycja Głównej:** po lewej lub obok siatki lista dostępnych widżetów, przeciąganie na siatkę, zmiana rozmiaru w obu osiach, usunięcie odkłada widżet na listę.
4. **Gość:** w menu głównym domu widzi osoby w domu, bez e-maili i zarządzania.
5. **Wszyscy:** dzwonek z licznikiem otwiera centrum powiadomień.

**Komunikaty i teksty:** po polsku, spójne z istniejącymi.
**JARVIS:** zna strefę domu (2.11); rozmowy da się nazywać i szukać (2.14); wywołania narzędzi idą ścieżkami z wersją (2.2).

## 4. Na co trzeba uważać (edge-case'y, uprawnienia, wydajność, integracje)

- **Uprawnienia:** gość widzi imię i rolę, nie e-mail; usunięcie urządzenia tylko Admin+; „O serwerze” tylko administrator serwera.
- **Edge-case'y:** wersjonowanie przy starych klientach (Desktop, bridge, ESP z firmware'em); usunięcie urządzenia w trakcie połączenia; widżet usunięty z siatki, gdy dodatek został odinstalowany; strefa właściciela domu zmienia się po przekazaniu domu; zła nazwa gałęzi wersji w workflow.
- **Dane i migracje:** 14 migracji przechodzi na kopii produkcyjnych danych z 2.4.1; wycofanie = przywrócenie kopii (migracje nie są odwracalne).
- **Wydajność / skala:** centrum powiadomień ograniczone do 30 dni; awatary i inne pliki z limitem rozmiaru.
- **Integracje:** manifest pluginów (wersja API), HomeMix (strefa, widżety), Desktop (ścieżki z wersją), firmware (ścieżki urządzeń).

## 5. Poza zakresem (Out of scope)

- Zmiany sklepu (właściciel 2026-11-25: na razie dobrze).
- Nowe projekty z roadmapy: Zakupy, rozbudowa Planera, historia odczytów Devices, nowe klocki HomeMix (H1-H5).
- Pełna lokalizacja domu (tylko strefa czasowa).
- Zmiana harnessu agentów (W3 jest już gotowe); ewentualne dalsze zmiany to osobna praca.
- Wersjonowanie przez nagłówek zamiast prefiksu (wybrany prefiks).

## 6. Otwarte pytania

1. ~~F7 i F9 w 3.0?~~ Rozstrzygnięte 2026-11-25 (właściciel): oba w 3.0.
2. Okno zgodności (2.2): kierunek właściciela 2026-11-25 to druga trasa w kodzie, czyli stare ścieżki zostają obok `/v1` na okres przejściowy (3.x). Architekci ustalają, gdzie stoi `/v1` (np. `/<serwis>/v1/...`), listę wyjątków i datę końca.
3. ~~Gdzie publikować paczkę (2.4)~~ Rozstrzygnięte 2026-11-25 (właściciel): wydanie GitHub (Release) z notatkami z CHANGELOG; obrazy w rejestrze to osobny krok później.
