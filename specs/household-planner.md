# Planer domu (kalendarz + zadania, rozbudowa pluginu Todo)

Autor: Qwerex56 · Data: 2026-11-22 · Wersja docelowa: do ustalenia · Status: szkic

Powstało z planu `plans/features/planner-plugin.md` (decyzja K1: planer to rozbudowa pluginu Todo, nie nowy plugin). Odpowiedzi na pytania z sekcji 4 są założone zgodnie z rekomendacją; oznaczone jako **[do potwierdzenia]**.

## 1. Cel biznesowy (CO ma powstać)

Domownicy planują dziś rzeczy w kilku miejscach: kalendarz w telefonie, kartka na lodówce, lista zadań w HomeApps. Planer łączy to w jeden domowy kalendarz: wspólne wydarzenia rodziny (wizyta u lekarza, trening, wyjazd), wydarzenia prywatne i zadania z terminem na jednej osi czasu, z przypomnieniami i z podglądem w kalendarzu w telefonie.

**Dla kogo:** członkowie domu (Member i wyżej) tworzą i zmieniają wydarzenia; Guest tylko czyta wybrane (patrz 2.9); Admin domu porządkuje wydarzenia domu.
**Problem:** rodzina nie widzi na raz „kto, co i kiedy”, a terminy zadań i wydarzeń żyją osobno; kalendarz w telefonie nie zna spraw domu.
**Efekt:** w ciągu kilku sekund widać, co dzieje się w domu dziś, w tym tygodniu i w tym miesiącu; wydarzenia domu pojawiają się w kalendarzu w telefonie bez ręcznego przepisywania; nikt nie zapomina o terminie, bo dostaje przypomnienie.

## 2. Wymagania funkcjonalne i kryteria akceptacji (Given / When / Then)

### 2.1 Dodanie wydarzenia domu

- **Given** jestem Member (lub wyżej) w domu z włączonym Planerem
- **When** dodaję wydarzenie z tytułem, datą i godziną od–do (albo „cały dzień”), miejscem, opisem i wybranymi osobami z domu
- **Then** wydarzenie jest widoczne dla wszystkich domowników z prawem czytania w kalendarzu, z oznaczeniem uczestników; bez wybranych osób dotyczy „całego domu”
- **Given** tytuł jest pusty albo koniec jest przed początkiem
- **When** próbuję zapisać
- **Then** dostaję czytelny komunikat po polsku i nic się nie zapisuje

### 2.2 Wydarzenie prywatne

- **Given** jestem Member (lub wyżej)
- **When** oznaczę wydarzenie jako prywatne
- **Then** widzę je tylko ja; nikt inny w domu, także Admin domu, Owner domu i administrator serwera, go nie widzi ani nie dostaje o nim przypomnień

### 2.3 Wydarzenia cykliczne

- **Given** tworzę lub edytuję wydarzenie
- **When** ustawiam powtarzanie (codziennie, co tydzień w wybrane dni, co miesiąc, co rok, co N, do daty albo N razy albo bez końca)
- **Then** kalendarz pokazuje kolejne wystąpienia
- **Given** edytuję lub usuwam jedno wystąpienie serii
- **When** wybieram zakres: „tylko to”, „to i następne” albo „wszystkie”
- **Then** zmiana dotyczy tylko wybranego zakresu; pozostałe wystąpienia zostają bez zmian
- **Given** wydarzenie jest ustawione na godzinę (np. wtorki 16:00)
- **When** w kraju zmienia się czas z letniego na zimowy
- **Then** wydarzenie nadal zaczyna się o 16:00 czasu lokalnego

### 2.4 Widoki kalendarza

- **Given** jestem w domu z włączonym Planerem
- **When** otwieram stronę „Kalendarz”
- **Then** mogę przełączać widoki: miesiąc, tydzień, dzień i lista (agenda); na telefonie domyślnie lista; widzę wydarzenia domu, swoje prywatne i zadania z terminem; mogę filtrować po osobie i włączać lub wyłączać warstwy (wydarzenia, zadania, kalendarze importowane)
- **Given** w oknie czasu nic się nie dzieje
- **When** je oglądam
- **Then** widzę czytelny stan pusty z podpowiedzią „Dodaj wydarzenie”

### 2.5 Zadania na kalendarzu

- **Given** zadanie z Todo ma termin (z godziną albo bez)
- **When** otwieram kalendarz
- **Then** zadanie jest widoczne w odpowiednim dniu, odróżnione od wydarzeń (pole wyboru zamiast godziny)
- **When** odhaczam zadanie na kalendarzu
- **Then** jest odhaczone także na stronie „Zadania”, a przeciągnięcie zadania na inny dzień zmienia jego termin
- **Given** nie mam prawa zmieniać tego zadania (np. cudza lista prywatna, lista zarchiwizowana)
- **When** próbuję je odhaczyć lub przesunąć
- **Then** akcja jest niedostępna zgodnie z zasadami z Todo; cudze zadania z prywatnych list w ogóle nie są widoczne

### 2.6 Przypomnienia

- **Given** wydarzenie ma przypomnienie (np. 1 godzinę i 1 dzień przed)
- **When** nadchodzi czas przypomnienia
- **Then** uczestnicy (przy „cały dom”: każdy z prawem czytania) dostają powiadomienie push zgodnie ze swoimi ustawieniami powiadomień (wyciszenie, ciche godziny); spóźnione o więcej niż godzinę przepada
- **Given** zadanie ma termin z godziną albo jest przypisane do mnie na dziś
- **When** nadchodzi termin
- **Then** dostaję przypomnienie o zadaniu (to odłożone dotąd przypomnienie z Todo)
- **Given** wydarzenie zostało usunięte lub odwołane
- **When** nadchodzi czas przypomnienia
- **Then** nikt go nie dostaje
- **Given** każdy domownik
- **When** ustawia domyślne przypomnienia w ustawieniach Planera
- **Then** nowe wydarzenia i zadania, w których uczestniczy, dostają je automatycznie, a on może je zmienić per wydarzenie tylko dla siebie

### 2.7 Kalendarz w telefonie (podgląd)

- **Given** jestem Member (lub wyżej) albo Guest (Guest tylko z tym, co sam widzi)
- **When** w ustawieniach Planera tworzę „link do kalendarza w telefonie” i wybieram, co ma zawierać (wydarzenia domu, moje prywatne, zadania: moje / wspólne / żadne)
- **Then** dostaję prywatny adres do subskrypcji (z kodem QR i instrukcją dla iPhone'a i dla Google Calendara), a po dodaniu go w telefonie widzę tam wydarzenia domu (powtarzane też jako serie), a zadania jako wydarzenia „☐ tytuł”; telefon odświeża je sam, więc zmiany pojawiają się z opóźnieniem zależnym od telefonu (od minut do kilkunastu godzin)
- **Given** mam utworzone linki
- **When** otwieram listę linków
- **Then** widzę, kiedy każdy był ostatnio pobrany, mogę go odwołać (przestaje działać natychmiast) i wygenerować nowy
- **Given** wyszedłem z domu albo moje konto zostało wyłączone
- **When** telefon pobiera link
- **Then** link przestaje zwracać dane
- **Given** ktoś bez mojego linku
- **When** próbuje zgadnąć adres
- **Then** nie dostaje niczego, a adres nie zdradza, czy taki link kiedykolwiek istniał
- **Uwaga:** zmiany w telefonie nie wracają do HomeApps (podgląd tylko do odczytu); pełna edycja z telefonu jest poza zakresem

### 2.8 Import mojego kalendarza z zewnątrz

- **Given** jestem Member (lub wyżej)
- **When** dodaję adres kalendarza zewnętrznego (np. praca, szkoła, święta) i wybieram widoczność dla domu: „tylko ja”, „dom widzi, że jestem zajęty” albo „dom widzi szczegóły”
- **Then** po chwili widzę te wydarzenia w kalendarzu (tylko do odczytu), a pozostali widzą je zgodnie z wybraną widocznością; odświeżają się co około pół godziny i na żądanie
- **Given** nie da się pobrać kalendarza
- **When** minie kilka dni błędów
- **Then** właściciel kalendarza dostaje powiadomienie, a do tego czasu widzi ostatnie znane dane z informacją, od kiedy są nieświeże
- **Given** adres wskazuje na zasób wewnętrzny serwera, a nie na zewnętrzny kalendarz
- **When** próbuję go dodać
- **Then** system odmawia i nie pobiera niczego
- **Given** importowane wydarzenia
- **When** generuję link do telefonu
- **Then** nie trafiają do linku (żeby nie tworzyć pętli telefon → HomeApps → telefon) i nie mają przypomnień z Planera

### 2.9 Uprawnienia i Guest

- **Given** jestem Guest
- **When** otwieram kalendarz
- **Then** widzę tylko wydarzenia domu dotyczące mnie lub całego domu, bez możliwości dodawania, edytowania ani importu; mogę utworzyć link do telefonu (2.7), który zawiera tylko to, co sam widzę
- **Given** jestem Guest i uczestniczę w wydarzeniu
- **When** nadchodzi przypomnienie
- **Then** dostaję je jak inni uczestnicy **[do potwierdzenia]**
- **Given** jestem Member
- **When** edytuję lub przesuwam wydarzenie domu
- **Then** mogę, jeśli jestem jego autorem lub uczestnikiem, a przy wydarzeniu „cały dom” każdy Member; usuwać mogą tylko autor i Admin domu; wydarzenie prywatne zmienia i usuwa tylko autor
- **Given** jestem Admin domu
- **When** edytuję lub usuwam dowolne wydarzenie domu
- **Then** mogę, ale cudzych prywatnych wydarzeń nie widzę

### 2.10 Odejście z domu i wyłączenie konta

- **Given** ktoś wychodzi z domu
- **When** jest uczestnikiem wydarzeń domu
- **Then** znika z listy uczestników tych wydarzeń; wydarzenia, które stworzył dla domu, zostają; jego prywatne wydarzenia i importowane kalendarze w tym domu znikają z widoku od razu i są usuwane po 30 dniach, chyba że wróci (jak prywatne listy zadań)
- **Given** konto zostaje wyłączone
- **When** dotyczą go przypomnienia i linki do telefonu
- **Then** przestają działać; po przywróceniu konta wracają linki, których nie odwołano

### 2.11 Tablica ścienna i JARVIS

- **Given** tablet ścienny pokazuje widżet „Dziś i jutro”
- **When** wchodzi ktoś postronny
- **Then** widzi wydarzenia domu i zadania na dziś, bez wydarzeń prywatnych i bez szczegółów importowanych kalendarzy
- **Given** JARVIS działa w imieniu domownika (po tej wersji, osobny etap)
- **When** domownik pyta „co mamy dziś”
- **Then** dostaje czytelną listę; JARVIS nie widzi więcej niż ten domownik, a usuwanie i odwoływanie wydarzeń wymaga potwierdzenia

### 2.12 Zadania cykliczne

- **Given** tworzę lub edytuję zadanie
- **When** ustawiam powtarzanie (jak w 2.3: codziennie, co tydzień w wybrane dni, co miesiąc, co rok, co N)
- **Then** po odhaczeniu zadania pojawia się jego następne wystąpienie z nowym terminem; historia wykonanych jest zachowana
- **Given** powtarzanie liczone **od terminu** (np. „co tydzień w środę: wynieść śmieci”) **[do potwierdzenia]**
- **When** nikt nie odhaczy zadania w terminie
- **Then** zadanie zostaje jako zaległe, a kolejne wystąpienia nie mnożą się (jest jedno zaległe, nie pięć) **[do potwierdzenia]**
- **Given** powtarzanie liczone **od wykonania** (np. „co 2 tygodnie po wymianie filtra”) **[do potwierdzenia]**
- **When** odhaczam zadanie
- **Then** następny termin to dzień wykonania plus ustawiony odstęp
- **Given** cykliczne zadanie ma przypisaną osobę
- **When** wychodzi ona z domu
- **Then** przypisanie znika, zadanie zostaje w serii
- **Given** edytuję lub usuwam cykliczne zadanie
- **When** wybieram zakres „tylko to”, „to i następne” albo „wszystkie”
- **Then** działa to jak przy wydarzeniach (2.3)

## 3. Przepływ użytkownika i oczekiwany UX

1. W menu domu obok „Zadania” jest „Kalendarz” (plugin w nazwie „Planer”).
2. Kalendarz: u góry przełącznik widoków i filtr osób, przycisk „+ Wydarzenie”. Klik w pusty dzień albo godzinę otwiera panel dodawania; klik w wydarzenie lub zadanie otwiera panel szczegółów z boku (na telefonie pełny ekran).
3. Panel wydarzenia: tytuł, cały dzień, od–do, powtarzanie, osoby, miejsce, opis, przypomnienia, „prywatne”.
4. Przeciągnięcie wydarzenia lub rozciągnięcie jego czasu zmienia godziny; przy serii aplikacja pyta o zakres zmiany.
5. Ustawienia Planera: „Telefon” (linki z kodem QR i instrukcją), „Moje kalendarze” (import), „Przypomnienia” (domyślne), pierwszy dzień tygodnia.
6. Strona domu i tablica `/wall`: widżet „Dziś i jutro”.

**Komunikaty i teksty:** po polsku i angielsku jak reszta aplikacji; kolory osób stałe dla danej osoby; czas pokazywany w strefie przeglądarki, a wydarzenia przechowują strefę, w której je utworzono.
**JARVIS:** w tej wersji nie dotyczy; zachowanie opisane w 2.11 jest wymaganiem na przyszłość.

## 4. Na co trzeba uważać (edge-case'y, uprawnienia, wydajność, integracje)

- **Uprawnienia:** jak w 2.2, 2.9; prywatnych wydarzeń nie widzi nikt poza autorem, także Owner i administrator serwera.
- **Edge-case'y:** równoczesna edycja tego samego wydarzenia przez dwie osoby; usunięcie serii z wyjątkami; wydarzenie przez północ i wielodniowe; wydarzenie w strefie innej niż domowników; przeniesienie zadania na dzień poza oknem widoku; link do telefonu przekazany osobie spoza domu; kalendarz zewnętrzny z ogromną liczbą wydarzeń lub z błędnym plikiem.
- **Prywatność i dane:** adresy importowanych kalendarzy są tajne (dają pełny wgląd) i nie mogą być widoczne dla nikogo poza właścicielem; linki do telefonu to hasła w adresie: tylko jedno pokazanie przy tworzeniu, odwołanie jednym kliknięciem.
- **Wydajność / skala:** dom to kilka osób i setki wydarzeń rocznie, w tym kilka serii bez końca; odświeżanie co około 30 s wystarczy (bez aktualizacji na żywo).
- **Integracje:** Todo (zadania i terminy), Notifications (przypomnienia), Households (członkostwo, role, imiona także dla Guesta), kalendarze w telefonach (iPhone, Google Calendar), tablica `/wall`, później JARVIS.

## 5. Poza zakresem (Out of scope)

- Edycja wydarzeń z poziomu telefonu (pełna synchronizacja dwukierunkowa, CalDAV): osobny etap po 1.0, jeśli w praktyce okaże się potrzebna.
- Integracja bezpośrednio z kontem Google lub Apple (logowanie do ich kalendarzy): odrzucone, wystarczą linki.
- Zaproszenia do wydarzeń mailem i odpowiedzi „przyjmuję / odrzucam”.
- Załączniki do wydarzeń, wyszukiwanie, przypinanie.
- Narzędzia JARVIS-a (zakres osobnego etapu JARVIS v1).
- Współdzielenie kalendarza z osobą spoza domu.
