# JARVIS na komputerze w tle, dowolne modele i pierwszy satelita ESP32-C3

Autor: Qwerex56 (spisane przez Claude) · Data: 2026-11-27 · Wersja docelowa: 3.1.0 (serwer) + HomeApps.Desktop 1.0 ·
Status: gotowa do debaty

Plan z decyzjami: `plans/features/jarvis-desktop-rework.md` (pliki projektu), decyzje R1-R10 przyjęte 2026-11-27.
Repozytoria: HomeApps (wymagania 2.6-2.9, 2.11) i `../HomeApps.Desktop` (2.1-2.5, 2.10). Przyjęte z planu: aplikacja
komputerowa na Avalonii (.NET) po prototypie, w repo HomeApps.Desktop, nowy folder obok Godota, Godot usuwany po
parytecie; kolejność platform macOS → Windows → Linux → Android.

## 1. Cel biznesowy (CO ma powstać)

**Dla kogo:** osoba z kontem (JARVIS osoby na jej komputerze), admin serwera (wybór modelu), admin domu (urządzenia).
**Problem:** aplikacja JARVIS na komputerze jest dziś oknem z hologramem; nie działa jak cichy towarzysz w tle, nie
ma historii rozmów, JARVIS odpowiada na wszystko i działa tylko na jednym modelu, a urządzenia na baterii nie
dostają ustawień z domu.
**Efekt:** JARVIS słucha w tle (na słowo albo ciągle), pokazuje się tylko ikoną w trayu / na pasku menu, ma log
rozmów, eksperymentalne narzędzia PC, odmawia spraw spoza domu, działa na dowolnym modelu (także lokalnym), a tani
ESP32-C3 wysyła dane pogodowe i odbiera „jak często” z aplikacji; pomiary energii mówią, który tryb zasilania wybrać.

## 2. Wymagania funkcjonalne i kryteria akceptacji (Given / When / Then)

### 2.1 Głos w tle: tryb słowa wybudzającego (domyślny)

- **Given** aplikacja na komputerze, tryb „słowo wybudzające”, odblokowana sesja
- **When** ktoś mówi „Jarvis, dodaj mleko do zakupów”
- **Then** ikona przechodzi w „słucham”, mowa jest rozpoznana lokalnie, do JARVIS-a idzie tylko tekst, odpowiedź
  jest czytana na głos; przez 8 s po odpowiedzi można mówić bez słowa
- **Given** brak słowa wybudzającego
- **Then** nic nie jest rozpoznawane ani wysyłane

### 2.2 Głos w tle: tryb ciągłego nasłuchiwania

- **Given** tryb „ciągłe nasłuchiwanie”
- **When** ktoś mówi do JARVIS-a bez jego imienia („zgaś lampkę w salonie”)
- **Then** komputer lokalnie ocenia z kontekstu ostatnich wypowiedzi, czy to do JARVIS-a, i dopiero wtedy wysyła tekst
- **Given** rozmowa domowników albo telewizor w tle
- **Then** JARVIS milczy; liczba fałszywych aktywacji jest widoczna w ustawieniach
- **Given** słabszy komputer
- **Then** lokalną ocenę modelem można wyłączyć (zostają reguły i tryb rozmowy)

### 2.3 Głos: zasady wspólne obu trybów

- **Given** JARVIS mówi
- **Then** jego własny głos nie jest brany za polecenie, a „stop” / „cisza” przerywa mowę
- **Given** ekran zablokowany albo mikrofon wyciszony skrótem
- **Then** nasłuch jest wstrzymany i ikona to pokazuje
- **Given** prośba o zmianę (nie odczyt) z mowy
- **Then** potwierdza się ją na ekranie (powiadomienie Tak/Nie), nie samym głosem
- **Then** audio nigdy nie opuszcza komputera

### 2.4 Ikona w trayu / na pasku menu jako jedyny wskaźnik (bez overlaya)

- **Given** aplikacja działa w tle na Windows albo macOS
- **Then** ikona ma animowane stany: spoczynek, słucham, myślę, mówię, wyciszony, brak połączenia, czeka na
  potwierdzenie; ostatnia odpowiedź jest w podpowiedzi i na górze menu
- **Given** akcja czeka na potwierdzenie
- **Then** przychodzi systemowe powiadomienie z Tak / Nie
- **Given** ikona schowana przez system (przepełnienie traya, notch)
- **Then** przy pierwszym uruchomieniu aplikacja podpowiada, jak ją przypiąć
- Linux: ikona bez animacji wystarcza

### 2.5 Log / historia rozmów (opcjonalne)

- **Given** zalogowana osoba otwiera historię z menu ikony
- **Then** widzi rozmowę konta z JARVIS-em jak w web: kanał (głos, tekst, satelita), godzina, użyte narzędzia,
  wynik potwierdzeń; filtr po dniu i kanale, wyszukiwanie; oczekujące potwierdzenie można zatwierdzić
- **And** pole tekstowe jest dodatkiem na dole; zamknięcie okna nie wyłącza nasłuchu

### 2.6 JARVIS odmawia spraw spoza HomeApps

- **Given** prośba niezwiązana z HomeApps, pluginami, dodatkami HomeMix, urządzeniami domu ani (gdy włączone)
  komputerem osoby, np. „napisz wypracowanie”, „napisz kod w Pythonie”, „jaki jest kurs bitcoina”
- **Then** JARVIS odmawia jednym zdaniem, mówi, w czym może pomóc, i nie wywołuje narzędzi; tak samo w czacie,
  głosie i na satelicie
- **Given** powitanie, „co potrafisz?”, data/godzina, „tak/nie” przy potwierdzeniu
- **Then** to jest w zakresie, bez odmowy

### 2.7 Dowolny model dla JARVIS-a

- **Given** Owner serwera w konfiguracji
- **When** wybiera dostawcę zgodnego z API OpenAI i podaje adres (np. Ollama / LM Studio na swoim PC), model i
  opcjonalny klucz
- **Then** JARVIS rozmawia tym modelem z tymi samymi narzędziami, uprawnieniami, potwierdzeniami i zasadą 2.6;
  dotychczasowy Claude zostaje drugą opcją
- **Given** model niedostępny albo bez obsługi narzędzi
- **Then** jest to widoczne przy starcie / w teście konfiguracji, a osoba słyszy „JARVIS jest chwilowo niedostępny”
- Wymiana modelu nie wymaga przebudowy ani zmian w domach i kontach

### 2.8 Ustawienia w dół dla urządzeń na baterii

- **Given** urządzenie, które śpi (raportuje co jakiś czas, bez stałego łącza)
- **When** admin albo domownik zmienia jego kontrolkę w zakładce JARVIS (np. „Jak często” 60 s → 300 s)
- **Then** przy najbliższym raporcie urządzenie dostaje ostatnią ustawioną wartość i stosuje ją; zakładka pokazuje,
  że zmiana czeka na urządzenie, a po raporcie, że dotarła
- **Given** kilka zmian między raportami
- **Then** dociera tylko ostatnia (to nie jest kolejka poleceń)

### 2.9 Pierwszy satelita pogodowy na ESP32-C3 i pomiar energii

- **Given** płytka ESP32-C3 sparowana z domem kodem
- **When** budzi się co ustawiony okres i wysyła losową temperaturę, wilgotność, ciśnienie oraz baterię
- **Then** zakładka JARVIS domu pokazuje odczyty, a brak raportu przez 2,5 okresu oznacza urządzenie jako zaginione
- **Given** trzy tryby: głęboki sen z raportem, stałe połączenie z oszczędzaniem modemu, bez oszczędzania
- **Then** w dokumentacji jest tabela: średni prąd, czas od wybudzenia do wysłania, odsetek udanych raportów, czas
  powrotu po zaniku Wi-Fi, ile razy dom pokazał „zaginione”; i rekomendacja trybu dla urządzeń na baterii
- **Given** brak Wi-Fi w domu, a obok telefon z hotspotem
- **Then** ta sama płytka działa przez hotspot bez zmian w oprogramowaniu (test mostu telefonu, wariant zerowy)

### 2.10 Narzędzia PC (eksperymentalne)

- **Given** osoba włącza „Funkcje eksperymentalne” i wybrane narzędzia (domyślnie wszystkie wyłączone)
- **Then** jej JARVIS może: otworzyć aplikację, otworzyć stronę, sterować odtwarzaniem, głośnością, ustawić
  minutnik / powiadomienie, powiedzieć, co gra; zmiany z potwierdzeniem
- **Given** narzędzie niewłączone
- **Then** JARVIS go nie widzi; odczyt schowka, zrzut ekranu i skróty systemowe są poza tą wersją

### 2.11 Przygotowanie mostu telefonu (bez aplikacji na telefon)

- **Given** autor urządzenia na ESP32-C3
- **Then** istnieje opisany sposób przesyłania tych samych danych urządzenia przez Bluetooth LE do telefonu, który
  przekazuje je do domu bez zmiany tożsamości urządzenia, a komponent urządzenia ma wymienny sposób łączenia
  (Wi-Fi albo BLE); działająca aplikacja na telefon jest kolejną wersją

## 3. Przepływ użytkownika i oczekiwany UX

1. Instalacja aplikacji na Macu / Windows → logowanie (jak dziś) → ikona w trayu / na pasku menu, podpowiedź
   przypięcia, wybór trybu głosu (domyślnie słowo wybudzające), autostart.
2. Mowa → animacja ikony → odpowiedź głosem i w podpowiedzi ikony; zmiana → powiadomienie Tak / Nie.
3. Menu ikony: Historia, Wycisz, Tryb głosu, Ustawienia (modele lokalne, funkcje eksperymentalne), Wyloguj.
4. Zakładka JARVIS domu: kontrolki urządzenia na baterii pokazują „czeka na urządzenie” do najbliższego raportu.

**Komunikaty i teksty:** po polsku; odmowa w stylu „Pomagam tylko w sprawach domu w HomeApps, np. listy, plan,
urządzenia. W czym mogę pomóc?”.
**JARVIS:** dotyczy całego zakresu.

## 4. Na co trzeba uważać

- **Uprawnienia:** JARVIS osoby nigdy powyżej osoby (bez zmian); narzędzia PC tylko dla osoby na jej komputerze;
  wybór modelu tylko Owner serwera; zmiana kontrolki uśpionego urządzenia jak dziś (Member+).
- **Edge-case'y:** fałszywe aktywacje z TV; kilka komputerów osoby słyszy to samo zdanie (odpowiada jeden);
  model lokalny ignoruje narzędzia albo zasadę 2.6; urządzenie usunięte, gdy czeka na nie zmiana; ikona schowana.
- **Prywatność i dane:** mikrofon domyślnie w trybie słowa, wskaźnik zawsze widoczny, audio lokalnie, tekst 7 dni
  (jak dziś); przy modelu lokalnym dane nie wychodzą z domu.
- **Wydajność / skala:** tryb ciągły obciąża komputer (lokalne STT + model); tryb lekki na słabszym sprzęcie.
- **Integracje:** Jarvis, Devices, komponent ESP-IDF, HomeApps.Desktop, powiadomienia systemowe macOS / Windows.

## 5. Poza zakresem (Out of scope)

- Overlay i hologram na ekranie (zastąpione ikoną).
- Aplikacja na telefon i iOS jako most; obsługa auta (CAN/OBD).
- Schowek, zrzuty ekranu, skróty systemowe i lokalne serwery MCP jako narzędzia PC.
- Pełna kolejka poleceń dla uśpionych urządzeń (tylko ostatnia wartość).
- Rozpoznawanie głosu osoby, proaktywny JARVIS, trenowanie własnych modeli.
