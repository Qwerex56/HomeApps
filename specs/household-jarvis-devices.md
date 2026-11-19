# JARVIS domu i urządzenia smart home

Autor: Qwerex56 · Data: 2026-11-19 · Wersja docelowa: 2.5.0 · Status: zaakceptowana, w realizacji (H-0 do H-4 zrobione)

Pełny plan z uzasadnieniem i decyzjami D1–D14: `plans/features/household-jarvis-devices.md` (w projekcie Claude).
Protokół urządzeń: `docs/devices-protocol.md`, katalog capabilities i ról: `docs/device-capabilities.md`.

## 0. Stan realizacji

Gałąź integracyjna: **`smart-devices/main`** (do `master` całość wraca raz, na końcu). Kolejne etapy:
`/run-feature @specs/household-jarvis-devices.md --base smart-devices/main`, po jednym etapie (H-n) na raz.

| Etap | Wymagania | Stan | Scalenie |
| --- | --- | --- | --- |
| H-0 Sprzątanie: satelita przestaje być członkiem domu | 2.1 | ✅ 2026-11-18 | 36e92a3 |
| H-1 JARVIS domu w Households | 2.1 | ✅ 2026-11-18 | 6ca5c47 |
| H-2 Uprawnienia z zakładki, sufit dla JARVIS-a osoby | 2.2 | ✅ 2026-11-18 | b7b628b |
| H-3a Cel, opis urządzenia, capabilities, dowolne nazwy | 2.3, 2.4, 2.5 | ✅ 2026-11-19 | b0b6e83 |
| H-3b Stan, sterowanie, zdarzenia, raport, heartbeat, SSE | 2.6, 2.7, 2.8 | ✅ 2026-11-19 | fabc728 |
| H-3c JARVIS zna urządzenia domu (narzędzia, prompt) | 2.9 | ✅ 2026-11-19 | c7f8b52 |
| H-3d „JARVIS może sterować” i „Pytaj” per urządzenie | 2.10 | ✅ 2026-11-19 | 32f78ae |
| H-4a Urządzenia dla pluginów i dodatków HomeMix | 2.11 | ✅ 2026-11-19 | 6c50445 |
| H-4b Wyzwalacze dodatków na serwerze | 2.12 | ✅ 2026-11-19 | 223f73c |
| H-4c Utwardzenie serwera (z ADR) | 2.2, 2.6, 2.9, 2.10 | ✅ 2026-11-19 | PR #63 |
| H-5 Mac jako urządzenie testowe | 2.13 | ○ następny (wymaga Maca) | |
| H-6 Komponent ESP-IDF `homeapps-device` | 2.14 | ✅ 2026-11-19 | |
| H-7 Narzędzia komputera | 2.15 | ○ | |
| H-8 Ciągłe nasłuchiwanie | 2.16 | ○ | |
| H-9 Zakładka „JARVIS” w aplikacji | 2.17 | ○ na koniec | |

**Do sprawdzenia lokalnie przed H-5:** testy Api, Infrastructure i Integration (wymagają Dockera, w chmurze nie
były uruchamiane) oraz nowy sekret `HOMEMIX_CLIENT_SECRET` w `.env`.

**Odłożone z H-0..H-4** (świadomie, patrz też sekcja 5): podgląd stanu na żywo i wybór urządzenia w widżecie (H-9),
akcje urządzeń przez `devices.control` w dodatku, powiadomienia domowników o zdarzeniach z `notify`, kolejka poleceń
dla uśpionych urządzeń.

**Otwarte decyzje:** brak. D10 (Wi-Fi i kod parowania) zamknięte w ADR: punkt dostępowy ze stroną konfiguracji,
BLE ewentualnie później.

## 1. Cel biznesowy (CO ma powstać)

Dziś JARVIS udaje domownika: każda satelita to osobny „członek domu” z własnymi prawami, a admin nie ma jednego
miejsca, w którym powie, co JARVIS w domu może. Urządzenie umie tylko być satelitą, więc przekaźnik, termometr czy
roleta nie mają stanu, sterowania ani celu. Chcemy rdzenia domu inteligentnego: jeden JARVIS domu z uprawnieniami
ustawianymi przez admina oraz urządzenia, które same opisują, co umieją, i z których korzysta aplikacja, JARVIS,
pluginy i dodatki HomeMix bez pisania osobnej integracji dla każdego urządzenia.

**Dla kogo:** admini domu (konfiguracja), domownicy (korzystanie i sterowanie), goście (podgląd), autorzy urządzeń i
dodatków (otwarty protokół).
**Problem:** JARVIS jako członek domu i urządzenia bez stanu i sterowania.
**Efekt:** admin w jednym miejscu ustawia JARVIS-a domu; nowe urządzenie po sparowaniu od razu ma kartę w aplikacji,
narzędzia JARVIS-a i jest widoczne dla dodatków; własne urządzenie ESP da się zrobić bez czytania kodu serwera.

## 2. Wymagania funkcjonalne i kryteria akceptacji (Given / When / Then)

### 2.1 JARVIS domu jest osobnym bytem, nie członkiem ✅ (H-0, H-1)

- **Given** dom z jedną lub kilkoma sparowanymi satelitami
- **When** domownik otwiera listę członków
- **Then** nie widzi na niej żadnej satelity ani „JARVIS-a”, a satelity nie liczą się do limitu członków

- **Given** dom z JARVIS-em domu ustawionym na „jak domownik”
- **When** satelita tego domu pyta plugin o swoją rolę
- **Then** dostaje rolę domownika i oznaczenie, że to asystent; nigdy roli admina ani właściciela

- **Given** dom z wyłączonym JARVIS-em domu
- **When** satelita tego domu próbuje działać
- **Then** nie dostaje dostępu do niczego w domu

- **Given** osoba mówi do satelity „tu Adrian” i podaje swój PIN głosowy
- **When** PIN jest poprawny
- **Then** tura działa w imieniu tej osoby, jak przed zmianą
- **gdy JARVIS domu jest wyłączony, satelita milczy także po PIN-ie**

### 2.2 Uprawnienia JARVIS-a domu i sufit dla JARVIS-a osoby ✅ (H-2)

- **Given** admin ustawił JARVIS-owi domu poziom „tylko odczyt” i dozwolone źródła (pluginy, HomeMix, urządzenia)
- **When** satelita pyta JARVIS-a o coś, co wymaga zmiany albo źródła spoza listy
- **Then** JARVIS nie ma takiego narzędzia i mówi, że w tym domu tego nie może

- **Given** osoba ma w domu rolę Member, a zakładka zabrania JARVIS-owi źródła „urządzenia”
- **When** JARVIS tej osoby (czat, aplikacja na komputerze) działa w tym domu
- **Then** nie zrobi więcej niż osoba ani więcej niż pozwala zakładka (sufit, D1)

- **Given** satelita albo dodatek próbuje obejść ustawienia (inna nazwa domu, metadane narzędzia, bezpośrednie
  wywołanie bramki)
- **When** wywołuje narzędzie spoza dozwolonych
- **Then** dostaje odmowę

### 2.3 Parowanie z opisem i celem urządzenia ✅ (H-3a)

- **Given** admin domu dodał urządzenie i dostał kod parowania
- **When** urządzenie paruje się kodem i przesyła opis `homeapps.device/1` (cel, model, wersja, kontrolki, czujniki,
  zdarzenia, akcje, capabilities, role, tekst dla JARVIS-a)
- **Then** urządzenie od razu pojawia się w domu z celem `assistant`, `smart_home` albo `computer` wziętym z opisu

- **Given** opis jest niepoprawny albo urządzenie obiecuje capability bez wymaganych pól
- **When** paruje się
- **Then** parowanie jest odrzucone z czytelnym powodem

- **Given** urządzenie po aktualizacji firmware wysyła nowy opis z nową wersją
- **When** łączy się ponownie
- **Then** dom widzi nowy opis i wersję

- **Given** urządzenie o celu `smart_home`
- **When** próbuje mówić przez kanał głosowy JARVIS-a
- **Then** dostaje odmowę (mowa tylko dla `assistant`)

### 2.4 Dowolne nazwy urządzeń ✅ backend (H-3a), ekran w H-9

- **Given** admin domu
- **When** nazywa urządzenie „Fotel taty w salonie” (do 60 znaków, polskie litery, spacje, emoji)
- **Then** nazwa jest przyjęta, a urządzenie dostaje stały klucz tworzony z nazwy (z numerem przy kolizji)

- **Given** w domu jest już „Lampka”
- **When** admin nazywa inne urządzenie „lampka”
- **Then** dostaje błąd, bo nazwy są unikalne w domu bez względu na wielkość liter

- **Given** urządzenie zmieniło nazwę
- **When** JARVIS i dodatki z niego korzystają
- **Then** klucz się nie zmienia i nic się nie psuje

### 2.5 Capabilities i role ✅ (H-3a)

- **Given** urządzenie zgłasza capability z katalogu HomeApps (np. `weather`, `switch` z id „lampka”)
- **When** dodatek albo JARVIS szuka urządzeń po capability albo roli
- **Then** znajduje je bez względu na autora urządzenia, a pola mają stałe nazwy z katalogu

- **Given** urządzenie przynosi własną capability z przestrzenią nazw autora (`qwerex56/akwarium`) z definicją
- **When** paruje się jako pierwsze z nią w instancji
- **Then** capability rejestruje się dla całej instancji; inny autor nie może jej przejąć, a nowsza wersja może ją
  tylko rozszerzyć

- **Given** urządzenie zgłasza nieznaną capability bez definicji
- **When** paruje się
- **Then** parowanie się udaje, a ta capability jest pomijana z ostrzeżeniem

### 2.6 Stan, sterowanie i zdarzenia ✅ (H-3b)

- **Given** urządzenie online
- **When** wysyła nowy stan
- **Then** dom widzi ostatnią wartość i czas odczytu, a otwarta zakładka dostaje zmianę na żywo

- **Given** domownik (Member+)
- **When** ustawia kontrolkę urządzenia wartością zgodną ze schematem
- **Then** urządzenie dostaje polecenie, odpowiada wynikiem i nowym stanem

- **Given** wartość niezgodna ze schematem kontrolki, albo gość próbuje sterować
- **When** wysyła polecenie
- **Then** dostaje odmowę, a do urządzenia nic nie trafia

- **Given** urządzenie zgłasza zdarzenie (dzwonek, ruch)
- **When** zdarzenie dociera
- **Then** jest zapisane w historii (30 dni, zdarzenia z położeniem 7 dni), widzą je domownicy, goście nie

- **Given** urządzenie zalewa serwer ramkami albo wysyła śmieci
- **When** przekracza limity
- **Then** nadmiar jest odrzucany, a serwer działa dalej

### 2.7 Urządzenia na baterii ✅ (H-3b; kolejka poleceń odłożona)

- **Given** urządzenie bez stałego łącza
- **When** budzi się i wysyła jednym raportem stan, zdarzenia i status
- **Then** dom widzi je tak samo jak z urządzenia online

### 2.8 Raport zdrowia: zaginione i niska bateria ✅ (H-3b)

- **Given** urządzenie zadeklarowało okres raportu
- **When** raport nie przyjdzie przez 2,5 okresu
- **Then** urządzenie jest oznaczone jako zaginione, a admini domu dostają powiadomienie; powrót raportu kończy ten stan

- **Given** bateria spada poniżej progu (domyślnie 20%)
- **When** przychodzą kolejne raporty z niską baterią
- **Then** admini dostają jedno powiadomienie na spadek, nie przy każdym raporcie

### 2.9 JARVIS zna urządzenia domu ✅ (H-3c)

- **Given** dom z urządzeniami, którymi JARVIS może się zajmować
- **When** domownik rozmawia z JARVIS-em
- **Then** JARVIS ma zwięzłą sekcję „Urządzenia w tym domu” (nazwa, cel, stan, zdrowie, tekst autora i uwagi admina)
  oraz narzędzia odczytu, ustawiania i zdarzeń dla każdego urządzenia, a przy wielu urządzeniach narzędzie do
  doczytania szczegółów

- **Given** tekst od autora urządzenia próbuje zmienić zasady JARVIS-a
- **When** trafia do rozmowy
- **Then** jest traktowany jako dane, nie polecenie; uprawnienia i potwierdzenia działają niezależnie

- **Given** osoba należy do dwóch domów
- **When** JARVIS działa w jednym z nich
- **Then** nie widzi urządzeń drugiego domu

### 2.10 „JARVIS może sterować” i „Pytaj” per urządzenie ✅ backend (H-3d), ekran w H-9

- **Given** admin wyłączył przy urządzeniu „JARVIS może sterować”
- **When** domownik prosi JARVIS-a o zmianę na tym urządzeniu
- **Then** JARVIS może tylko odczytać stan, a sterować nie

- **Given** przy urządzeniu włączone „Pytaj”
- **When** JARVIS chce coś ustawić
- **Then** najpierw prosi domownika o potwierdzenie

### 2.11 Pluginy i dodatki HomeMix korzystają z urządzeń ✅ (H-4a)

- **Given** plugin serwerowy działający w imieniu domownika
- **When** czyta albo ustawia urządzenie przez klienta z kitu pluginów
- **Then** ma dokładnie te uprawnienia co ta osoba i dostaje powiadomienia o zmianach stanu i zdarzeniach

- **Given** dodatek HomeMix wskazuje urządzenia po capabilities, rolach albo modelu
- **When** domownik instaluje go i zgadza się na `devices` (odczyt) albo `devices.control` (sterowanie)
- **Then** dodatek widzi tylko pasujące urządzenia tego domu i steruje tylko ze zgodą `devices.control`

- **Given** dodatek bez zgody `devices`
- **When** pyta o urządzenia
- **Then** dostaje odmowę

### 2.12 Wyzwalacze dodatków na serwerze ✅ (H-4b)

- **Given** dodatek deklaruje wyzwalacz („gdy `weather.temperature` < 19, ustaw grzanie na 2”) i jest zainstalowany
  w domu przez admina
- **When** urządzenie zgłasza pasujący stan albo zdarzenie, a nikt nie ma otwartej aplikacji
- **Then** skrypt wyzwalacza działa na serwerze w granicach zgód instalacji i uprawnień osoby, która ją zainstalowała,
  a zmiana jest w historii jako „ustawione przez dodatek X”

- **Given** wyzwalacz zmienia urządzenie, które samo go odpala
- **When** przychodzi zmiana wywołana przez ten dodatek
- **Then** wyzwalacz nie odpala się ponownie przez 5 s (ochrona przed pętlą)

- **Given** wyzwalacz przekracza limit uruchomień albo raz po raz kończy się błędem
- **When** to się powtarza
- **Then** jest wstrzymywany, a po serii błędów wyłączany z powiadomieniem admina; admin widzi wyzwalacze i może je
  włączyć albo wyłączyć

- **Given** usunięcie instalacji dodatku
- **When** instalacja znika
- **Then** znikają też stany jej wyzwalaczy

### 2.13 Mac jako urządzenie testowe ○ (H-5)

- **Given** Mac właściciela z HomeApps.Desktop
- **When** uruchamia most i emulator
- **Then** Mac występuje w domu jako cztery osobne urządzenia: satelita (`assistant`, mowa), smart home na stałym
  zasilaniu (np. głośność i wyciszenie Maca, udawana lampka, obciążenie CPU, raport co 60 s), smart home na baterii
  (raport co kilka minut z prawdziwą baterią Maca) i komputer (aplikacja z hologramem)

- **Given** emulator na baterii jest wyłączony
- **When** mija 2,5 okresu raportu
- **Then** urządzenie jest zaginione (sprawdzenie 2.8 na żywo)

### 2.14 Komponent ESP-IDF `homeapps-device` ✅ (H-6)

- **Given** autor urządzenia na ESP32-S3, C3 albo C6
- **When** dodaje komponent z `firmware/homeapps-device/` i deklaruje w kodzie kontrolki, czujniki i zdarzenia
- **Then** urządzenie samo łączy się z Wi-Fi, paruje kodem, wysyła opis, stan, zdarzenia i raport zdrowia, odbiera
  polecenia sprawdzone wg schematu i wraca po zerwaniu łącza

- **Given** pierwsze uruchomienie urządzenia
- **When** użytkownik konfiguruje je z telefonu (sposób wg D10)
- **Then** podaje sieć, hasło, adres HomeApps i kod parowania bez instalowania aplikacji

- **Given** repozytorium
- **When** buduje się przykłady (przełącznik C3, stacja pogodowa na baterii C6, znacznik obecności)
- **Then** budują się dla S3, C3 i C6, a komponent jest gotowy do publikacji w Rejestrze Komponentów Espressif

### 2.15 Narzędzia komputera ○ (H-7, dawne S-3)

- **Given** aplikacja na komputerze osoby (cel `computer`)
- **When** zgłasza wbudowane akcje w opisie
- **Then** JARVIS tej osoby może ich użyć, z potwierdzeniem tam, gdzie opis go wymaga; później lokalne serwery MCP z
  listą dozwolonych

### 2.16 Ciągłe nasłuchiwanie ○ (H-8, dawne S-4)

- **Given** aplikacja na komputerze z włączonym nasłuchiwaniem
- **When** ktoś mówi w pobliżu
- **Then** lokalnie rozpoznaje mowę, ocenia „czy to do mnie?” i dopiero wtedy rozmawia z JARVIS-em; własny głos JARVIS-a
  nie jest brany za polecenie

### 2.17 Zakładka „JARVIS” w aplikacji ○ (H-9)

- **Given** admin domu
- **When** otwiera zakładkę „JARVIS” (zastępuje „Urządzenia”)
- **Then** widzi i ustawia JARVIS-a domu (stan, nazwa, poziom, źródła, przechowywanie zdarzeń), dodaje urządzenie
  nazwą i kodem, a urządzenia widzi pogrupowane wg celu z ostatnim stanem, kontrolkami, zdarzeniami, zdrowiem,
  „JARVIS może sterować” i „Pytaj”

- **Given** domownik
- **When** otwiera zakładkę
- **Then** steruje kontrolkami i przegląda zdarzenia, ale nie zmienia konfiguracji; gość tylko patrzy, bez zdarzeń

- **Given** ekran zgody przy instalacji dodatku
- **When** dodatek prosi o `devices` albo `devices.control`
- **Then** zgoda jest opisana po polsku, a widżet urządzenia pozwala wybrać jedno z pasujących urządzeń

## 3. Przepływ użytkownika i oczekiwany UX

1. Admin domu otwiera zakładkę „JARVIS”: lewa kolumna to JARVIS domu, na górze „Dodaj urządzenie”, w środku urządzenia
   wg celu (JARVIS, smart home, Moje Komputery). Na telefonie lewa kolumna zjeżdża nad listę.
2. „Dodaj urządzenie”: nazwa, potem kod parowania do wpisania w urządzeniu. Po sparowaniu urządzenie pojawia się od
   razu, z celem, kontrolkami i czujnikami z opisu.
3. Karta urządzenia: online / offline / zaginione, bateria i sygnał, ostatni stan, kontrolki rysowane z typu, ostatnie
   zdarzenia, przełączniki „JARVIS może sterować” i „Pytaj”, uwagi dla JARVIS-a.
4. Dodatki HomeMix pokazują ładniejsze widżety; rdzeń zawsze ma domyślną kartę z opisu.

**Komunikaty i teksty:** po polsku; „Urządzenie zaginęło”, „Niska bateria (18%)”, „Ustawione przez dodatek X”.
**JARVIS:** czyta stan, steruje i przegląda zdarzenia urządzeń w granicach zakładki; nie zmienia konfiguracji urządzeń
ani domu. Samodzielne odzywanie się na zdarzenie („ktoś dzwoni do drzwi”) jest poza zakresem.

## 4. Na co trzeba uważać

- **Uprawnienia:** Admin+ konfiguruje wszystko; Member+ korzysta i steruje; Guest tylko patrzy, bez historii zdarzeń
  (D6, D9). JARVIS domu nigdy powyżej domownika; JARVIS osoby nigdy powyżej osoby i zakładki (D1).
- **Edge-case'y:** urządzenie zaginione albo uśpione w chwili polecenia; dwie zmiany tej samej kontrolki naraz; dodatek
  sterujący urządzeniem, które odpala jego wyzwalacz; urządzenie wysyłające nowy opis po aktualizacji.
- **Prywatność i dane:** zdarzenia 30 dni, z położeniem 7 dni; tekst z urządzenia to dane, nie polecenie.
- **Wydajność / skala:** dziesiątki urządzeń na dom; sekcja w prompcie do ok. 4000 znaków. Strumień zmian, limiter i
  atrybucja zmian są w pamięci jednej instancji Devices i HomeMix.
- **Integracje:** Households, Devices, Jarvis, HomeMix, Notifications, kit pluginów, HomeApps.Desktop, ESP-IDF.
- **Otwarte dla `/spec-debate` przy H-6:** D10 (Wi-Fi i kod parowania), szyfrowanie NVS, odświeżanie tokenu urządzenia.

## 5. Poza zakresem (Out of scope)

- ESPHome i inne obce formaty (ewentualnie osobny serwis-tłumacz później, D4).
- Proaktywny JARVIS odzywający się sam na zdarzenia.
- Historia odczytów stanu (tylko ostatni stan; historia zdarzeń jest).
- Uprawnienia per narzędzie (są poziom, źródła i per urządzenie, D2).
- Capabilities per dom i publiczny indeks capabilities (D14, później).
- W tej wersji odłożone: kolejka poleceń dla uśpionych urządzeń, akcje urządzeń przez `devices.control` w dodatkach,
  powiadomienia domowników o zdarzeniach z `notify`.
