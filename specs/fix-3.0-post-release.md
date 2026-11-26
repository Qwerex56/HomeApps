# Poprawki po wdrożeniu 3.0: zgody JARVIS-a i układ widżetów

Autor: Qwerex56 (spisane przez Claude) · Data: 2026-11-26 · Wersja docelowa: 3.0.1 · Status: gotowa do debaty

## 1. Cel biznesowy (CO ma powstać)

Po wdrożeniu 3.0 właściciel zgłosił trzy błędy, które psują codzienne korzystanie z domu. Prośby JARVIS-a o zgodę
na wywołanie narzędzia nie pokazują, co się z nimi stało po decyzji, więc domownik nie wie, czy akcja poszła.
Widżety dodatków HomeMix wciąż nie dają się swobodnie poszerzać i zwężać, choć 3.0 obiecywało zmianę rozmiaru
w obu osiach (wymaganie 2.5 z `specs/release-3.0.md`). Zapisany układ widżetów potrafi wrócić do poprzedniego stanu,
mimo że aplikacja nie zgłasza błędu, więc praca włożona w ułożenie Głównej lub tablicy przepada.

**Dla kogo:** każdy domownik rozmawiający z JARVIS-em (punkt A); Owner i Admin domu edytujący układ Głównej i tablicy,
a wszyscy domownicy jako oglądający ten układ (punkty B i C).
**Problem:** prośba o zgodę „wisi” po decyzji; widżety dodatków nie zmieniają szerokości albo tylko w wąskim zakresie;
zapisany układ się nie utrzymuje.
**Efekt:** po decyzji każda prośba o zgodę od razu i wszędzie pokazuje wynik; widżet dodatku HomeMix zmienia szerokość
tak samo swobodnie jak wysokość; zapisany układ jest dokładnie tym, który zobaczy każdy po odświeżeniu, na każdym urządzeniu.

## 2. Wymagania funkcjonalne i kryteria akceptacji (Given / When / Then)

### A. Prośby JARVIS-a o zgodę

Zgłoszenie: na Macu poproszono JARVIS-a o zmianę głośności. JARVIS wrzucił na czat prośbę o zgodę na użycie narzędzia.
Po potwierdzeniu (i osobno po odrzuceniu) akcja została wykonana zgodnie z decyzją, ale wpis na czacie i w rozmowie
z JARVIS-em dalej pokazywał „Czeka na potwierdzenie”.

#### 2.1 Potwierdzenie aktualizuje wpis

- **Given** JARVIS wyświetlił prośbę o zgodę na wywołanie narzędzia (np. zmiana głośności na Macu) i wpis ma stan „Czeka na potwierdzenie”
- **When** osoba, która prosiła, klika „Potwierdź”
- **Then** w ciągu kilku sekund, bez odświeżania strony, wpis pokazuje „Zrobione” (albo „Nie udało się”, jeśli narzędzie zwróciło błąd), przyciski decyzji znikają, a JARVIS w rozmowie odnosi się do wyniku, a nie dalej czeka

#### 2.2 Odrzucenie aktualizuje wpis

- **Given** jak w 2.1
- **When** osoba klika „Anuluj” (odrzuca)
- **Then** wpis pokazuje „Anulowane”, przyciski decyzji znikają, akcja nie zostaje wykonana, a JARVIS przyjmuje do wiadomości, że jej nie zrobi

#### 2.3 Ten sam stan we wszystkich miejscach

- **Given** ta sama prośba o zgodę jest widoczna w kilku miejscach (wiadomość na czacie domu, rozmowa z JARVIS-em, powiadomienie, inna karta albo inne urządzenie tej samej osoby)
- **When** osoba podejmie decyzję w którymkolwiek z nich
- **Then** wszystkie te miejsca pokazują ten sam stan końcowy bez ręcznego odświeżania; nigdzie nie zostaje „Czeka na potwierdzenie”

#### 2.4 Stan po odświeżeniu i ponownym wejściu

- **Given** decyzja została podjęta
- **When** ktokolwiek z dostępem odświeży stronę, wejdzie do rozmowy później albo otworzy czat na innym urządzeniu
- **Then** widzi stan końcowy (Zrobione / Nie udało się / Anulowane / Wygasło), nigdy „Czeka na potwierdzenie”

#### 2.5 Wygaśnięcie

- **Given** prośba nie dostała decyzji w czasie jej ważności
- **When** czas minie
- **Then** wpis pokazuje „Wygasło, poproś jeszcze raz.”, przyciski znikają, a akcja nie zostaje wykonana

#### 2.6 Brak podwójnego wykonania (negatywne)

- **Given** decyzja już zapadła
- **When** ktoś kliknie przycisk decyzji na nieodświeżonym ekranie albo w drugim miejscu
- **Then** akcja nie wykonuje się drugi raz, a osoba widzi „Ta akcja już nie czeka na potwierdzenie.” i aktualny stan wpisu

#### 2.7 Tylko proszący decyduje (negatywne, bez zmian)

- **Given** inny domownik widzi prośbę o zgodę na czacie domu
- **When** próbuje ją potwierdzić lub odrzucić
- **Then** nie może; widzi tylko stan, który aktualizuje się tak samo jak u proszącego

#### 2.8 Dotyczy każdego narzędzia

- **Given** prośba o zgodę dotyczy dowolnego narzędzia wymagającego potwierdzenia (urządzenie, dodatek HomeMix, plugin), a nie tylko głośności na Macu
- **When** zapadnie decyzja
- **Then** zachowanie jest takie jak w 2.1-2.6

### B. Widżety dodatków HomeMix: szerokość

Zgłoszenie: widżety dodatków HomeMix w edycji układu wciąż nie dają się poszerzać ani zwężać albo tylko w małym zakresie,
choć wysokość zmienia się swobodnie. 3.0 zapowiadało zmianę rozmiaru w obu osiach dla każdego widżetu.

#### 2.9 Swobodna szerokość widżetu dodatku

- **Given** jestem Ownerem lub Adminem domu i edytuję układ Głównej albo tablicy, na siatce jest widżet dodatku HomeMix
- **When** przeciągam jego krawędź lub róg w poziomie
- **Then** widżet zmienia szerokość co jedną kolumnę, od 1 kolumny do pełnej szerokości siatki, tak samo płynnie jak wysokość i tak samo jak widżety pluginów i karta domu

#### 2.9a Granice zadeklarowane przez dodatek

- **Given** dodatek HomeMix deklaruje najmniejszy (i opcjonalnie największy) rozmiar swojego widżetu
- **When** zmieniam rozmiar tego widżetu w edycji układu
- **Then** nie da się go zmniejszyć poniżej minimum ani powiększyć ponad maksimum, a w tych granicach szerokość zmienia się swobodnie co jedną kolumnę; dodatek bez deklaracji ma zakres od 1 do 8 kolumn

#### 2.10 Szerokość i wysokość naraz

- **Given** jak w 2.9
- **When** ciągnę za róg widżetu na ukos
- **Then** zmieniają się obie osie naraz, a nowy rozmiar jest widoczny już w trakcie przeciągania

#### 2.11 Treść dopasowana do szerokości

- **Given** zmieniłem szerokość widżetu dodatku
- **When** puszczam krawędź i kończę edycję
- **Then** widżet pokazuje treść dopasowaną do nowego rozmiaru (bez obcięcia na środku i bez pustego pasa obok), w edycji i po zapisie

#### 2.12 Rozmiar zostaje po zapisie

- **Given** ustawiłem niestandardową szerokość widżetu dodatku i zapisałem układ
- **When** odświeżam stronę albo otwieram dom na innym urządzeniu o tej samej szerokości ekranu
- **Then** widżet ma dokładnie tę szerokość, którą ustawiłem (zobacz też C)

#### 2.13 Brak zmiany rozmiaru dla oglądających (negatywne, bez zmian)

- **Given** jestem Memberem lub Gościem
- **When** oglądam Główną albo tablicę
- **Then** nie mogę zmieniać rozmiaru widżetów

### C. Zapis układu widżetów

Zgłoszenie: po zapisaniu układu (zapis kończy się bez błędu) układ często wraca do poprzedniego stanu.

#### 2.14 Zapisany układ zostaje

- **Given** jestem Ownerem lub Adminem domu, zmieniam układ Głównej lub tablicy (pozycje, rozmiary, dodanie widżetu z listy, odłożenie widżetu na listę) i zapisuję
- **When** aplikacja potwierdza zapis
- **Then** układ na ekranie zostaje taki, jaki ułożyłem: nie „skacze” z powrotem ani zaraz po zapisie, ani po chwili

#### 2.15 Ten sam układ po odświeżeniu i na innych ekranach

- **Given** zapisałem układ
- **When** odświeżam stronę, wylogowuję się i loguję, otwieram dom na innym urządzeniu albo patrzę na tablicę ścienną
- **Then** każdy widzi ostatnio zapisany układ (z uwzględnieniem dopasowania do mniejszego ekranu, które działa jak dotąd)

#### 2.16 Kolejne zapisy pod rząd

- **Given** zapisuję układ kilka razy pod rząd w krótkim odstępie (np. drobne poprawki po każdym zapisie)
- **When** wracam do układu po każdym zapisie i na końcu odświeżam
- **Then** widzę wynik ostatniego zapisu, nie któregoś wcześniejszego

#### 2.17 Sukces tylko wtedy, gdy zapis naprawdę jest

- **Given** zapis się nie udał albo został odrzucony (np. brak uprawnień, rozmiar spoza dopuszczalnego zakresu, utrata połączenia)
- **When** kończę edycję
- **Then** widzę komunikat po polsku, że układ nie został zapisany, a moje zmiany zostają na ekranie edycji, żebym mógł spróbować jeszcze raz; aplikacja nigdy nie pokazuje sukcesu, jeśli układ nie został trwale zapisany

#### 2.18 Zmiana przez inną osobę w tym samym czasie

- **Given** dwie osoby z uprawnieniami edytują ten sam układ naraz (lub zmienia się on na innym urządzeniu)
- **When** jedna zapisze, a druga dalej edytuje
- **Then** druga osoba przy próbie zapisu dostaje informację „Układ zmienił się w międzyczasie” i wybiera: nadpisać go swoją wersją albo wczytać nowszy układ (tracąc swoje zmiany)
- **And** jeśli wybierze nadpisanie, jej zapis wygrywa i wszyscy widzą jej układ; żadna z osób nie widzi cichego powrotu do starszego układu bez informacji

## 3. Przepływ użytkownika i oczekiwany UX

1. **Zgoda JARVIS-a:** domownik prosi JARVIS-a o akcję (czat domu, rozmowa z JARVIS-em, głos lub aplikacja na Macu). Pojawia się wpis „Czeka na potwierdzenie” z przyciskami „Potwierdź” i „Anuluj” (oraz „Nie pytaj”, jeśli jest dostępne) i czasem ważności „do {godzina}”. Po kliknięciu przyciski od razu blokują się (stan „w trakcie”), a po chwili wpis zmienia się na stan końcowy we wszystkich miejscach.
2. **Edycja układu:** Owner/Admin otwiera zakładkę Widżety albo edycję tablicy. Każdy widżet (karta domu, plugin, dodatek HomeMix) ma uchwyty do zmiany rozmiaru w obu osiach. Po „Zapisz” widzi potwierdzenie i dokładnie ten układ, który ułożył. Przy błędzie zostaje w edycji z komunikatem.
3. **Oglądanie:** Member, Gość i tablica ścienna zawsze widzą ostatnio zapisany układ.
4. **Telefon i tablet:** na wąskich ekranach układ dalej dopasowuje się automatycznie (jak w 2.4.1/3.0); poprawka nie zmienia tej zasady, tylko gwarantuje, że źródłem jest ostatni zapis.

**Komunikaty i teksty:** istniejące stany wpisu: „Czeka na potwierdzenie”, „Zrobione”, „Nie udało się”, „Anulowane”, „Wygasło, poproś jeszcze raz.”, „Ta akcja już nie czeka na potwierdzenie.”. Nowy komunikat dla 2.17, propozycja: „Nie udało się zapisać układu. Twoje zmiany są nadal tutaj, spróbuj jeszcze raz.”.
**JARVIS:** po decyzji JARVIS w rozmowie odpowiada zgodnie z wynikiem („Zrobione, głośność ustawiona na 40%” / „Dobrze, nie zmieniam”). JARVIS nie zmienia układu widżetów (bez zmian).

## 4. Na co trzeba uważać (edge-case'y, uprawnienia, wydajność, integracje)

- **Uprawnienia:** decyzję o zgodzie podejmuje tylko proszący; układ zmieniają tylko Owner i Admin domu (bez zmian).
- **Edge-case'y:** decyzja podjęta w powiadomieniu lub na innym urządzeniu niż to, na którym widać wpis; utrata połączenia w chwili decyzji i powrót; decyzja tuż przed wygaśnięciem; narzędzie na Macu wykonane, ale Mac rozłączony zaraz potem; rozmowa usunięta w trakcie oczekiwania. Widżet dodatku, który deklaruje własne rozmiary; dodatek odinstalowany, gdy jego widżet jest na siatce; widżet szerszy niż liczba kolumn na mniejszym ekranie; zapis zaraz po dodaniu lub odłożeniu widżetu; dwie otwarte karty edycji.
- **Prywatność i dane:** bez zmian w stosunku do 3.0.
- **Wydajność / skala:** aktualizacja stanu zgody ma być widoczna w kilka sekund; układ to kilkanaście widżetów na dom.
- **Integracje:** JARVIS (rozmowy), plugin Chat (wpisy JARVIS-a na czacie domu), powiadomienia, aplikacja Desktop na Macu (narzędzia urządzenia), HomeMix (widżety dodatków i zapis układu), tablica ścienna.

## 5. Poza zakresem (Out of scope)

- Nowe rodzaje zgód, zmiana zasad, które narzędzia wymagają potwierdzenia, i zmiana czasu ważności prośby.
- Nowe miejsca na widżety, nowe rozmiary siatki (3/5/8 kolumn zostaje) i zmiana zasad dopasowania do małych ekranów.
- Osobne układy dla telefonu i tabletu.
- Edycja układu przez JARVIS-a.
- Pozostałe punkty planu 3.0, które nie dotyczą tych trzech błędów.

## 6. Pytania otwarte do właściciela

- **P1 Wersja (rozstrzygnięte 2026-11-26):** osobna poprawka 3.0.1, jedno małe wydanie z trzema poprawkami.
- **P2 Miejsce decyzji (rozstrzygnięte 2026-11-26):** decyzję kliknięto na czacie domu (plugin Chat). Po odświeżeniu strony wpis pokazuje już właściwy stan, więc stan końcowy jest zapisywany poprawnie; brakuje tylko aktualizacji na żywo bez odświeżania (2.1-2.3). Scenariusz z czatem domu bez odświeżania jest obowiązkowym testem akceptacyjnym, a 2.4 ma dalej działać jak dziś.
- **P3 Granice szerokości dodatku (rozstrzygnięte 2026-11-26):** dodatek HomeMix może zadeklarować najmniejszy (i opcjonalnie największy) rozmiar widżetu; edytor go szanuje. W tych granicach szerokość jest swobodna od 1 do 8 kolumn, a bez deklaracji od 1 do 8 (patrz 2.9a).
- **P4 Równoczesna edycja (rozstrzygnięte 2026-11-26):** (a) ostatni zapis wygrywa, ale przed nadpisaniem cudzej zmiany osoba dostaje informację „Układ zmienił się w międzyczasie” i sama decyduje (patrz 2.18).
- **P5 Kiedy układ wraca (rozstrzygnięte 2026-11-26):** od razu po zapisie, jeszcze na tym samym ekranie. Scenariusz z 2.14 (zapis i obserwacja ekranu przez kilka sekund bez odświeżania) jest obowiązkowym testem akceptacyjnym.
