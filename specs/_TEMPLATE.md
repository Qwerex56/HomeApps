<!--
Szablon specyfikacji HomeApps. Opisuje CO ma powstać i jak to sprawdzimy, nigdy JAK to zaprogramować
(bez klas, tabel, endpointów: to ustala /spec-debate w pliku <nazwa>-adr.md).
Nazwa pliku: specs/<nazwa-w-kebab-case>.md, np. specs/household-shopping-list.md. Usuń te komentarze w gotowym pliku.
Dalej: /spec-debate @specs/<nazwa>.md → /run-feature @specs/<nazwa>.md
-->

# [Nazwa funkcji]

Autor: <imię> · Data: <RRRR-MM-DD> · Wersja docelowa: <np. 2.5.0> · Status: szkic | gotowa do debaty | zaakceptowana

## 1. Cel biznesowy (CO ma powstać)

<!-- 3–6 zdań: jaki problem domowników rozwiązujemy, dla kogo (rola w domu / na serwerze) i po czym poznamy, że się udało. -->

**Dla kogo:** <np. członkowie domu (Member+), goście tylko odczyt>
**Problem:** …
**Efekt:** …

## 2. Wymagania funkcjonalne i kryteria akceptacji (Given / When / Then)

<!-- Każde wymaganie ma numer (2.1, 2.2 …), bo testy i review się do nich odwołują. Jedno zachowanie = jedno kryterium.
     Pisz z perspektywy użytkownika i jego roli. Dopisz też kryteria "negatywne": czego komu NIE wolno. -->

### 2.1 <Krótka nazwa wymagania>

- **Given** <stan początkowy, rola użytkownika>
- **When** <akcja>
- **Then** <obserwowalny wynik>

### 2.2 <…>

- **Given** …
- **When** …
- **Then** …

## 3. Przepływ użytkownika i oczekiwany UX

<!-- Kroki ekran po ekranie, gdzie w aplikacji to się pojawia (zakładka domu, konto, tablica /wall, JARVIS),
     co widzi każda rola, komunikaty po polsku, stany pusty / ładowanie / błąd, telefon i tablet ścienny. -->

1. …
2. …

**Komunikaty i teksty:** …
**JARVIS:** <czy i co asystent może z tym zrobić w imieniu domownika; "nie dotyczy">

## 4. Na co trzeba uważać (edge-case'y, uprawnienia, wydajność, integracje)

<!-- Uzupełnij, ile wiesz; resztę dopyta zespół w /spec-debate. -->

- **Uprawnienia:** <kto widzi, kto zmienia; gość; osoba spoza domu; admin serwera>
- **Edge-case'y:** <pusta lista, duplikaty, równoczesne zmiany, wygasłe zaproszenia, wyłączone konto, odejście z domu>
- **Prywatność i dane:** <co zostaje po usunięciu/wyłączeniu konta lub odejściu z domu>
- **Wydajność / skala:** <ile elementów realnie, czy potrzebne odświeżanie na żywo>
- **Integracje:** <inne serwisy, pluginy, HomeMix, urządzenia, powiadomienia>

## 5. Poza zakresem (Out of scope)

<!-- Co świadomie NIE wchodzi do tej wersji (chroni przed rozrostem zakresu; reviewer to sprawdza). -->

- …
