# Pierwszy test urządzenia smart home na ESP32-C3

Status: **DRAFT** (2026-11-27), do decyzji właściciela. Sam plan, bez kodu.
Powiązane: `specs/household-jarvis-devices.md` (H-0..H-9 zrobione), `docs/devices-protocol.md`, `docs/devices.md`,
`firmware/homeapps-device/` (komponent ESP-IDF z H-6), `firmware/examples/weather-station/`.
Repo: **HomeApps** (reguła: urządzenia i firmware zostają w rdzeniu).

## 1. Cel

Pierwsze prawdziwe urządzenie na prawdziwej płytce. Tani ESP32-C3:

1. paruje się z domem tak, jak opisuje protokół (punkt dostępowy + kod parowania),
2. wysyła **losowe dane pogodowe** (temperatura, wilgotność, ciśnienie),
3. **odbiera proste ustawienie**: co ile sekund ma raportować,
4. pokazuje, jak **tryby oszczędzania energii** wpływają na to, co widać w aplikacji: online/offline, „milczy”,
   opóźnienie zmiany ustawienia, czas wybudzenia, a na końcu pobór prądu.

Wynik testu to nie tylko „działa”, ale **tabela pomiarów** i zalecenia, które trafią do `docs/devices-protocol.md`
(sekcja „Tryby zasilania”) jako wskazówki dla kolejnych urządzeń.

Słowo „satelita” w tym planie znaczy po prostu „małe urządzenie”. W protokole to urządzenie ma cel `smart_home`
(rola `weather_station`), a nie `assistant`, bo nie jest uchem ani ustami JARVIS-a.

## 2. Co już mamy (przegląd 2026-11-27)

Master od `c2c3f9b` do `e2196cf` (PR #77, poprawki 3.0.1: konflikt zapisu tablicy HomeMix, karty potwierdzeń
JARVIS-a) nie dotyka Devices ani firmware. Gałąź `smart-devices/main` jest już w masterze (H-0..H-9 zamknięte).

Gotowe i wystarczające do testu, **bez zmian na serwerze**:

| Potrzeba | Co jest |
| --- | --- |
| Protokół | Własny (D4): WebSocket `/devices/link` dla urządzeń zawsze połączonych, `POST /devices/report` dla śpiących. **Nie MQTT, nie ESPHome.** |
| Parowanie | Kod 8 znaków od admina (10 min), punkt dostępowy `HomeApps-XXXX` ze stroną `192.168.4.1` (D10). |
| Opis urządzenia | Deskryptor `homeapps.device/1`: sensory, kontrolki, zdarzenia, `heartbeat`, `sleepy`. |
| Status | Ramka `status` (bateria, napięcie, RSSI, uptime, firmware, błąd); „milczy” po 2,5 okresu, powiadomienie adminów. |
| UI | Karta urządzenia: online/offline/milczy, ostatnio widziane, bateria, RSSI, sensory, kontrolki, historia zdarzeń. |
| Firmware | Komponent `homeapps-device` dla S3/C3/C6 (setup, parowanie, link, `/report`, tokeny, backoff), CI buduje przykłady na C3. |

**Luka:** urządzenie śpiące (`sleepy: true`) dziś **nie dostaje żadnych poleceń** (`set` tylko przez otwarty link,
brak kolejki; w UI kontrolki śpiącego urządzenia są zablokowane). A „odbiera jak często ma sprawdzać” w trybie
deep sleep wymaga właśnie tego. Patrz decyzja **E2**.

## 3. Protokół (rekomendacja)

Bez nowego protokołu. Ten sam firmware w dwóch rodzinach trybów:

- **Tryby „połączone”** (pełna moc, modem sleep, auto light sleep): WebSocket `/devices/link`. Ustawienie
  przychodzi jako ramka `set`, urządzenie odpowiada `set_result` i nowym `state`. Działa dziś.
- **Tryby „śpiące”** (deep sleep): urządzenie budzi się, wysyła `state` + `event` + `status` jednym
  `POST /devices/report`, zasypia. Ustawienie dostaje w odpowiedzi na raport (po E2).

## 4. Urządzenie testowe: deskryptor

Model `qwerex56/c3-test-v1`, cel `smart_home`, rola `weather_station`, capabilities `weather` (+ `power`, jeśli
płytka ma baterię).

| Pole | Nazwa | Schemat / uwagi |
| --- | --- | --- |
| sensor | `temperature` | number, °C, losowo 18..26 z płynnym dryfem (nie skacze co odczyt) |
| sensor | `humidity` | number 0..100, % |
| sensor | `pressure` | number, hPa, 990..1030 |
| sensor | `battery` | tylko z baterią (ADC); bez baterii pomijamy, nie losujemy |
| kontrolka | `interval_s` | integer, `minimum` 10, `maximum` = `heartbeat` (patrz E3), widget `select` (10, 30, 60, 300, 900) |
| sensor | `mode` | string enum `awake`/`modem`/`light`/`deep`: tryb wgrany w tej kompilacji, żeby było go widać na karcie |
| zdarzenie | `cycle` | dane jednego cyklu, to jest **dziennik pomiarów** (niżej) |
| zdarzenie | `boot` | powód startu (`power_on`, `deep_sleep`, `brownout`, `panic`, `watchdog`) |

Zdarzenie `cycle` (raz na raport) niesie: `n` (licznik w pamięci RTC), `wake_ms` (od wybudzenia do wysłania),
`wifi_ms`, `token_ms`, `send_ms`, `retries`, `rssi`. Historia zdarzeń trzymana jest 30 dni, czyli to wystarczy
na cały test, a skrypt wyciągnie ją do CSV (E-4). `jarvis`: „Testowa stacja pogodowa na ESP32-C3. Dane są losowe,
nie podawaj ich jako prawdziwej pogody.”

## 5. Parowanie i środowisko

1. Admin domu dodaje urządzenie w zakładce JARVIS („C3 · deep 5 min”), dostaje kod.
2. Po pierwszym wgraniu C3 otwiera `HomeApps-XXXX`; hasło punktu w logu z portu szeregowego.
3. Na `192.168.4.1`: Wi-Fi 2,4 GHz, adres HomeApps, kod. Urządzenie paruje się i pojawia na liście.

Ważne na pierwszą płytkę:

- **Szyfrowanie NVS wyłączone** w aplikacji testowej. Przykłady mają je włączone z kluczem w eFuse, a wypalenie
  bloku eFuse jest nieodwracalne (E7).
- **TLS:** prod `home-apps.dev` stoi za Cloudflare, więc wystarczy domyślny pakiet Mozilla. Lokalnie: TOFU z
  odciskiem z logu albo `HOMEAPPS_INSECURE_HTTP` tylko w dev.
- **Każdy scenariusz uśpienia = osobne parowanie** (osobne urządzenie w aplikacji), bo `sleepy` i `heartbeat` to
  pola deskryptora, a urządzenie śpiące nie wysyła `hello`, więc nie podmieni deskryptora (E4). Przy okazji karty
  stoją obok siebie i łatwo je porównać.

## 6. Widoczność w UI

Większość już jest na karcie urządzenia. Do zrobienia tylko przy E2:

- kontrolka śpiącego urządzenia przestaje być zablokowana; po zmianie pokazuje „Zastosuje się przy następnym
  wybudzeniu (ok. 14:35)” na podstawie ostatniego raportu i okresu,
- po raporcie, w którym urządzenie potwierdziło nową wartość w `state`, znacznik znika.

Co obserwujemy w czasie testu (bez nowego kodu): kropka online/offline/milczy, „ostatnio widziane”, RSSI, historia
zdarzeń `cycle`/`boot`, powiadomienia adminów „urządzenie milczy”.

## 7. Scenariusze pomiarowe

Każdy scenariusz trwa **co najmniej 8 godzin** (najlepiej noc), ta sama płytka, to samo miejsce, ten sam router.
Interwał raportu 60 s, chyba że napisano inaczej.

| # | Tryb | Transport | Ustawienia ESP-IDF | Czego się spodziewamy |
| --- | --- | --- | --- | --- |
| P1 | Pełna moc (punkt odniesienia) | WS | `WIFI_PS_NONE` | stale online, `set` < 1 s, ~80-100 mA |
| P2 | Modem sleep (domyślny) | WS | `WIFI_PS_MIN_MODEM` | stale online, `set` < 1 s, mniej prądu |
| P3 | Modem sleep max + auto light sleep | WS | `WIFI_PS_MAX_MODEM`, `listen_interval` 3 i 10, `CONFIG_PM_ENABLE`, tickless idle | online, ale `set` opóźniony o wielokrotność DTIM; ryzyko zrywania linku |
| P4 | Deep sleep krótki | `/report` | `esp_deep_sleep`, 60 s | „offline” zawsze (to poprawne), nigdy „milczy”; ustawienie przy następnym wybudzeniu |
| P5 | Deep sleep długi | `/report` | 300 s, potem 900 s | jak P4; liczy się czas wybudzenia i prąd średni |
| P6 | Zmiana interwału w locie | oba | P2 i P5, zmiana 60 → 10 → 300 z aplikacji | opóźnienie zastosowania, brak fałszywego „milczy” po wydłużeniu |
| P7 | Zakłócenia | oba | wyłączenie routera na 5 min, restart Devices, zasięg graniczny (RSSI < -80) | powrót bez ręcznej pomocy, backoff zgodny z protokołem |

Mierzymy:

- **Stałość połączenia:** liczba rozłączeń linku na godzinę (WS), liczba fałszywych „milczy”, odsetek udanych raportów.
- **Opóźnienia:** klik w aplikacji → `set_result` (WS), klik → zastosowanie (sleepy), wybudzenie → raport przyjęty.
- **Czas aktywności** (z `cycle`): Wi-Fi, token, wysyłka. Uwaga: komponent pobiera **świeży token przy każdym
  raporcie**, czyli dwa połączenia HTTPS z TLS na wybudzenie. To prawdopodobnie największy koszt w P4/P5 i
  kandydat do optymalizacji (token w pamięci RTC do wygaśnięcia), ale najpierw zmierzmy.
- **Prąd** (E6): średni w każdym scenariuszu i szacowany czas pracy na ogniwie 2000 mAh.

Ryzyka do sprawdzenia w P1-P3: Cloudflare zamyka bezczynny WebSocket po ok. 100 s, więc okres `status` musi
być krótszy (60 s jest OK). Na płytkach typu SuperMini dioda zasilania i układ USB zjadają więcej prądu niż sam C3
w deep sleep; dla liczb bateryjnych trzeba ją odciąć albo uznać pomiar za względny.

## 8. Etapy

| Etap | Co | Serwer | Kto testuje |
| --- | --- | --- | --- |
| **E-0** Przygotowanie | Lista sprzętu, ESP-IDF 5.4 na Macu, nowa aplikacja `firmware/tests/c3-power-lab/` (osobno od przykładów, bez NVS encryption), build w CI dla C3 | brak | właściciel: flash na płytkę |
| **E-1** Tryby połączone | Losowe dane, `interval_s` przez `set`, zdarzenia `cycle`/`boot`, tryb wybierany w Kconfig (`awake`/`modem`/`light`); scenariusze P1-P3 | brak | właściciel: noc na każdy tryb |
| **E-2** Ustawienia dla śpiących | Ostatnia wartość kontrolki czeka na serwerze i trafia do urządzenia w odpowiedzi na `/devices/report`; odblokowane kontrolki i znacznik w UI; opis w `devices-protocol.md`; obsługa w komponencie | Devices + Web + firmware | pipeline: spec → `/spec-debate` → `/run-feature` |
| **E-3** Tryby śpiące | Tryb `deep` w aplikacji testowej, przyjmowanie ustawienia przy wybudzeniu; scenariusze P4-P6 | brak (po E-2) | właściciel |
| **E-4** Pomiary i wnioski | Skrypt `tools/devices/export-events.py` (historia zdarzeń → CSV), P7, tabela wyników, sekcja „Tryby zasilania” w `docs/devices-protocol.md`, ewentualnie zmiany domyślnych wartości komponentu | brak | Claude zbiera, właściciel akceptuje |

E-1 nie czeka na E-2: tryby połączone dają pierwsze wyniki od razu. Jeśli E2 wypadnie na „A”, E-2 znika, a P4-P6
idą bez zmiany ustawień w locie.

Wg reguły z planu urządzeń: każdy etap na własnej gałęzi, PR do właściciela, master tylko przez PR.

## 9. Kryteria sukcesu

1. Od wgrania firmware do urządzenia na liście w aplikacji: **≤ 5 minut**, bez czytania kodu (tylko log i strona setup).
2. Dane widać na karcie: w P1-P3 **≤ 2 s** od pomiaru, w P4-P5 przy każdym wybudzeniu.
3. Zmiana `interval_s`: w P1-P2 zastosowana **≤ 2 s**, w P3 **≤ 5 s**, w P4-P5 **przy pierwszym wybudzeniu** po zmianie.
4. W żadnym scenariuszu przez 8 h **zero fałszywych „milczy”**; w P1-P2 **≤ 1 rozłączenie linku na godzinę**.
5. P7: po każdym zakłóceniu urządzenie wraca samo w ciągu **≤ 3 minut** od powrotu sieci/serwera.
6. P4: cykl wybudzenia (start → raport przyjęty) zmierzony; cel **< 3 s** na dobrym Wi-Fi.
7. Gotowa tabela wyników z zaleceniem: który tryb do czujników na baterii, który do urządzeń na zasilaczu.

## 10. Otwarte decyzje (z rekomendacją)

- **E1 Płytka.** Jaką płytkę C3 masz? *Rekomendacja:* ESP32-C3 DevKitM-1 albo SuperMini; do pomiarów prądu
  druga sztuka z odciętą diodą zasilania.
- **E2 Jak śpiące urządzenie dostaje ustawienie?**
  A: wcale (zmiana interwału tylko w trybach połączonych, zero pracy na serwerze);
  **B (rekomendacja): ostatnia wartość per kontrolka czeka na serwerze i wraca w odpowiedzi na raport** (to nie
  kolejka poleceń, tylko „stan żądany”; urządzenie potwierdza nowym `state`);
  C: osobne `GET` co wybudzenie (dodatkowe połączenie TLS, więcej prądu).
  B domyka też odłożony punkt „kolejka poleceń dla uśpionych urządzeń”. Kształt odpowiedzi (dziś tablica wyników)
  ustali `/spec-debate`.
- **E3 Interwał a heartbeat.** Jeśli urządzenie raportuje rzadziej niż `heartbeat`, serwer uzna je za milczące.
  *Rekomendacja:* `maximum` kontrolki = `heartbeat` z deskryptora (serwer bez zmian). Alternatywa: serwer
  traktuje standardową kontrolkę `report_interval` jako okres heartbeatu (więcej pracy, ale czulsze wykrywanie).
- **E4 Przełączanie trybów.** *Rekomendacja:* tryb w Kconfig, każdy scenariusz to osobne parowanie i osobne
  urządzenie w aplikacji. Alternatywa: tryb zmieniany z aplikacji w locie, co wymaga podmiany deskryptora bez `hello`.
- **E5 Gdzie testujemy.** *Rekomendacja:* E-1 najpierw lokalny dev (szybkie poprawki), potem P1-P7 na prod
  `home-apps.dev`, bo Cloudflare Tunnel jest częścią prawdziwej ścieżki (limity bezczynnego WS).
- **E6 Pomiar prądu.** *Rekomendacja:* tani miernik USB (pomiar względny) + czasy z `cycle`; Nordic PPK2 albo
  INA219 tylko jeśli chcesz realnych µA w deep sleep i prognozy baterii.
- **E7 Szyfrowanie NVS.** *Rekomendacja:* wyłączone na płytce testowej (eFuse jest nieodwracalne).
- **E8 Dane.** *Rekomendacja:* losowe z dryfem, jak prosiłeś; opcjonalnie DHT11 za przełącznikiem Kconfig, skoro
  i tak go masz.
- **E9 Token przy każdym raporcie.** *Rekomendacja:* najpierw zmierzyć w P4; optymalizacja (token w RTC) jako
  osobny mały etap tylko jeśli koszt wyjdzie duży.

## 11. Poza zakresem

Prawdziwe czujniki pogodowe i obudowa, BLE/telefon jako most (to plan aplikacji desktop/telefonu), audio i wake
word, OTA, szyfrowanie flash i secure boot, akcje urządzeń w komponencie.
