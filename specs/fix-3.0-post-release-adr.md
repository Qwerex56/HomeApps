# ADR: Poprawki po wdrożeniu 3.0 (zgody JARVIS-a, szerokość widżetów, trwałość układu)

Spec: [specs/fix-3.0-post-release.md](fix-3.0-post-release.md) · Data: 2026-11-26 · Status: accepted
Target repo: `.` (HomeApps); Stage 2 i część Stage 5 w `../HomeApps.Chat` (`--repo ../HomeApps.Chat`)

## 1. Resolved questions

| Pytanie | Odpowiedź | Skutek |
|---|---|---|
| Nowy dodatek po ułożeniu tablicy: gdzie trafia widżet? | Nie dodaje się sam; na paletę, Owner/Admin dodaje go ręcznie | `arrange()` nie dokłada widżetów do skonfigurowanej tablicy (`updatedAt != null`) |
| Odinstalowany dodatek na zapisanej siatce | Kafelek znika, kolejny zapis czyści martwy klucz | bez placeholdera |
| Szeroki widżet na mniejszym ekranie, wygaśnięcie karty | Rozstrzygnięte przez spec (2.4.1/3.0 oraz 2.5) | bez zmian: zwijanie przy renderze, karta „Wygasło, poproś jeszcze raz.” |

Stages: Track A (Stage 1 -> 2: JARVIS, potem Chat) i Track B (Stage 3 -> 4: HomeMix, potem Web) są niezależne i mogą iść równolegle; Stage 5 to testy akceptacyjne (obowiązkowe P2 i P5).

---


## 0. Weryfikacja uwag Devil's Advocate (Weryfikacja w kodzie)

Wszystkie uwagi DA zostały zweryfikowane w kodzie źródłowym obu repozytoriów:

1. **[HIGH] Spec 2.3 / Q3 broken: direct `POST /jarvis/api/v1/confirmations/{id}` nie powiadamia czatu**  
   *Weryfikacja:* **Potwierdzone.** W `AssistantEndpoints.cs:55` i `DecideHandler.cs:12` brak wstrzyknięcia `IRunRepository` oraz `IEventBus`. `AssistantResponded` był publikowany wyłącznie w `AssistantConfirmedConsumer.cs:23-26`. Dodatkowo `RunFeed.Publish` w `DecideHandler` było martwym no-opem, gdyż `RunFeed.Close` zamyka strumień SSE zaraz po wygenerowaniu wstępnej odpowiedzi asystenta.  
   *Poprawka:* Przeniesiono publikację `AssistantResponded` do `DecideHandler`, gdy run dotyczy kanału zewnętrznego (`run.Channel == "chat"` lub zawiera referencję). Przy decyzji z UI `/jarvis` karta aktualizuje się lokalnie przez `setQueryData`, a drugi nieodświeżony ekran przy próbie decyzji otrzymuje 409 i odświeża stan.

2. **[HIGH] Spec 2.6 violated w `../HomeApps.Chat`: podwójna decyzja na nieodświeżonym ekranie**  
   *Weryfikacja:* **Potwierdzone.** `ConfirmAssistantActionHandler.cs:39` zwraca błąd `assistant.not_waiting`, który nie posiadał tłumaczenia w `src/Web/src/shared/i18n/pl.json` (istniało tylko `errors.jarvis.action_not_pending`). W `ConversationPane.vue:171` blok `catch` nie wywoływał `thread.refresh(message.id)`, pozostawiając wiszący stan. W `AssistantConfirmedConsumer.cs:17-20` nieudana komenda powodowała cichy `return` bez publikacji aktualnego stanu `ToolCall`, blokując stan `localInFlight`.  
   *Poprawka:* Dodano klucze błędów `errors.assistant.not_waiting` i `errors.assistant.not_asker` do `pl.json`, dodano `thread.refresh(message.id)` w `catch` w `ConversationPane.vue`, a w `AssistantConfirmedConsumer` obsłużono publikację aktualnego stanu akcji przy błędach `NotPending`/`Expired`.

3. **[HIGH] Spec 2.14 / P5 wyścig: zdarzenie SignalR `ChooseWidget` nadpisuje układ przed zakończeniem `BoardEditor`**  
   *Weryfikacja:* **Potwierdzone.** W `BoardEditor.vue:225` i `227` wywoływane jest `save([])` (Reset) i `save(layout)`. Poprzednia zmiana sygnatury na `save(force = false)` psuła przycisk Reset. Ponadto `boardsApi.save` emituje SignalR `board.changed`, co powodowało refetch z bazy przed ukończeniem pętli `homemixApi.chooseWidget` i przed wywołaniem `setQueryData`.  
   *Poprawka:* Zachowano parametr `items: Item[]` w `save(items: Item[], force = false)`, a `queryClient.setQueryData` jest wywoływane synchronicznie bezpośrednio po `boardsApi.save`.

4. **[MED] Spec 2.18: precyzja timestampów Postgres vs .NET oraz luka pierwszego zapisu nieskonfigurowanej tablicy**  
   *Weryfikacja:* **Potwierdzone.** Przy nieskonfigurowanej tablicy (`board == null`) zapis dwóch administratorów z `ExpectedUpdatedAt == null` powodował bezkonfliktowe nadpisanie przez drugiego usera. Ponadto porównanie `board.UpdatedAt != command.ExpectedUpdatedAt.Value` bez tolerancji mikrosekund i bez tokenu EF Core mogło powodować fałszywe konflikty.  
   *Poprawka:* W `SaveBoardHandler`: jeśli `board is not null && !command.Force`, brak `ExpectedUpdatedAt` lub różnica > 1 ms traktowana jest jako `BoardErrors.Conflict`. Skonfigurowano `.Property(b => b.UpdatedAt).IsConcurrencyToken()` w `WidgetBoardConfiguration.cs` i przechwytywanie `DbUpdateConcurrencyException`.

5. **[MED] Spec 2.14 / 2.16: reguła pojedynczego widżetu w `arrange()` cofa zapisany układ**  
   *Weryfikacja:* **Potwierdzone.** W `layout.ts:81-85` warunek `if (only && (!only.fixedWidth || only.widths.includes(SINGLE.w)))` bezwarunkowo wymuszał układ `x: 2, y: 0, w: 4, h: 3` (`SINGLE`), ignorując pozycję i rozmiar zapisany w bazie dla pojedynczego widżetu przy `isConfigured === true`.  
   *Poprawka:* Warunek `only` jest stosowany wyłącznie gdy tablica nie jest jeszcze skonfigurowana (`!isConfigured`) lub gdy `saved` nie zawiera danego widżetu.

6. **[MED] Spec 2.11: iframe dodatku nie aktualizuje rozmiaru po zmianie szerokości**  
   *Weryfikacja:* **Potwierdzone.** W `BoardEditor.vue:118` warunek `item.w !== w.w` porównywał szerokość kolumn z presetem bazowym, a nie rzeczywistą zmianę progu rozmiaru. W `WidgetTile.vue:50` komponent `MixFrame` nie posiadał klucza `:key="widget.size"`, przez co iframe nie odświeżał `HomeMixContext.widget.size`.  
   *Poprawka:* Porównanie `sizeFor(item.w) !== w.size` w edytorze oraz powiązanie `:key="widget.size"` na `MixFrame` w `WidgetTile.vue`.

7. **[LOW] Spec 2.1 / 3: Etykieta statusu w czacie i obsługa błędów w `useSubmit` w `BoardEditor.vue`**  
   *Weryfikacja:* **Potwierdzone.** W `Chat.Web/src/messages/pl.ts:111` status przetłumaczono jako `'Czeka'` zamiast `'Czeka na potwierdzenie'`. Przechwycenie błędu wewnątrz akcji w `useSubmit` powodowało zwrócenie `true` i natychmiastowe wyemitowanie `emit('done')` przy błędzie 409.  
   *Poprawka:* Zmieniono tłumaczenie na `'Czeka na potwierdzenie'` oraz przekazano callback `onProblem` do `submit(action, onProblem)`.

8. **[LOW] Równoległość etapów i obowiązkowe testy akceptacyjne P2 i P5**  
   *Weryfikacja:* **Potwierdzone.** Zespoły JARVIS+Chat oraz HomeMix+Web operują na rozłącznych modułach i repozytoriach. Specyfikacja §6 wprost narzuca P2 i P5 jako obowiązkowe testy akceptacyjne.  
   *Poprawka:* Wydzielono dwa niezależne, równoległe tory wdrożenia (Track A i Track B) oraz zdefiniowano P2 i P5 jako obowiązkowe automatyczne testy akceptacyjne.

---

## 1. Diagnoza przyczyn źródłowych (Root Cause Analysis)

1. **Utrata identyfikatora korelacji w JARVIS:** Metoda mapująca `Cards.Action(ActionView view)` w `src/Jarvis/Jarvis.Infrastructure/Messaging/Cards.cs` zerowała `view.Id` (`null`) dla statusów innych niż `NeedsConfirmation`. W rezultacie agregat `Message.cs` w Chat nie potrafił odnaleźć akcji po jej zatwierdzeniu/odrzuceniu i nie emitował sygnału SignalR `MessageEdited`.
2. **Brak notyfikacji o decyzji spoza czatu:** `DecideHandler.cs` po podjęciu decyzji w API JARVIS (`/confirmations/{id}`) nie publikował zdarzenia integracyjnego `AssistantResponded`, przez co czat i inne klienty nie dowiadywały się o decyzji.
3. **Wymuszona stała szerokość i błąd granic widżetów:** Flaga `fixedWidth: true` w `board.ts:77` oraz klamrowanie do wąskich presetów w `layout.ts:92-96` blokowały swobodną zmianę szerokości. Pusta lista deklaracji rozmiarów w manifeście powodowała ewaluację `Math.min(...[])` do `Infinity`.
4. **Brak rozróżnienia tablicy skonfigurowanej od nowej:** W `layout.ts:103-107` brakujące widżety były automatycznie dodawane na siatkę. Zdjęcie wszystkich widżetów na paletę (`saved = []`) było traktowane jak brak konfiguracji i powodowało samoczynne przywrócenie kafelków.
5. **Brak kontroli współbieżności w HomeMix:** `SaveBoardHandler.cs` bezwarunkowo nadpisywał stan tablicy bez sprawdzania wersji i znacznika czasu `UpdatedAt`.

---

## 2. Decyzje architektoniczne (Decisions Made)

1. **Niezmienny identyfikator korelacji w `Cards.Action`:** Zawsze przekazujemy `view.Id` jako `ConfirmationId` niezależnie od statusu akcji, ponieważ zewnętrzny kanał potrzebuje stałego klucza korelacji do aktualizacji wiersza.
2. **Publikacja zdarzenia integracyjnego z poziomu `DecideHandler`:** `DecideHandler` ładuje `AssistantRun` i publikuje `AssistantResponded` dla kanałów zewnętrznych, zapewniając synchronizację czatu niezależnie od tego, gdzie kliknięto decyzję.
3. **Odblokowanie swobodnej szerokości widżetów z fallbackiem `[1..8]`:** Usuwamy `fixedWidth: true` dla widżetów `mix`, gwarantując skok co 1 kolumnę w granicach zadeklarowanych przez dodatek lub domyślnie 1–8.
4. **Flaga `isConfigured` (`board.updatedAt != null`) w `arrange()`:** Pusta tablica po zdjęciu widżetów jest poprawnym stanem zapisanym i nie powoduje ponownego wpychania kafelków na siatkę.
5. **Optymistyczna współbieżność oparta o `ExpectedUpdatedAt` i token EF Core:** Zapobiega cichemu nadpisywaniu cudzych zmian przy równoczesnej edycji tablicy.
6. **Synchroniczna aktualizacja pamięci podręcznej TanStack Query przed wywołaniem API dodatków:** Wywołanie `queryClient.setQueryData` natychmiast po `boardsApi.save` eliminuje wyścig z powiadomieniem SignalR `board.changed`.
7. **Trwały stan `in-flight` i obsługa błędów decyzji w Chat UI:** Przyciski pozostają zablokowane aż do nadejścia zdarzenia `MessageEdited`, a w przypadku błędu 409 następuje odświeżenie karty.

---

## 3. Zakres odrzucony i odłożony (Rejected and Deferred Scope)

1. *Odrzucono:* Wstrzymywanie przeładowań iframe na zdarzenie `pointerup` w `BoardEditor.vue` — edytor w ogóle nie renderuje ramek iframe (używa lekkich kafelków zastępczych; ramki występują wyłącznie w `WidgetTile.vue`).
2. *Odrzucono:* Heurystykę `saved.length === 0` w `layout.ts` — zastąpiono ją stanem `isConfigured` (`updatedAt != null`), co zapobiega psuciu celowo opróżnionych tablic.
3. *Odłożono poza 3.0.1:* Pełny mechanizm operacji 2D CRDT dla współbieżnej edycji siatki w czasie rzeczywistym — prosta blokada optymistyczna z kodem 409 w zupełności spełnia wymaganie 2.18.
4. *Odłożono poza 3.0.1:* Osobne układy siatki konfigurowane niezależnie dla urządzeń mobilnych i tabletów.

---

## 4. Developer Implementation Blueprint

### A. Repozytorium: `HomeApps` (Rdzeń)

#### 1. JARVIS: Zachowanie identyfikatora korelacji i publikacja zdarzenia z `DecideHandler`
- **[`src/Jarvis/Jarvis.Infrastructure/Messaging/Cards.cs:10-15`](file:///Users/adrianczubaty/Documents/RiderProjects/HomeApps/src/Jarvis/Jarvis.Infrastructure/Messaging/Cards.cs#L10-L15):**
  ```csharp
  public static AssistantAction Action(ActionView view) => new(
      view.Description,
      view.Status.ToString(),
      null,
      view.Id, // ZAWSZE zachowujemy identyfikator akcji do korelacji w kanałach zewnętrznych
      view.ConfirmBy);
  ```
- **[`src/Jarvis/Jarvis.Application/Actions/Decide/DecideHandler.cs:9-37`](file:///Users/adrianczubaty/Documents/RiderProjects/HomeApps/src/Jarvis/Jarvis.Application/Actions/Decide/DecideHandler.cs#L9-L37):**
  Wstrzyknąć `IRunRepository runs` oraz `IEventBus events`. Po zapisaniu wyniku (`Complete` lub `Deny`) wyemitować `AssistantResponded` do kanału zewnętrznego:
  ```csharp
  internal sealed class DecideHandler(
      IToolCallRepository calls,
      IRunRepository runs,
      IToolExecutor executor,
      IEventBus events,
      TimeProvider time) : ICommandHandler<DecideCommand, ActionView>
  {
      public async Task<Result<ActionView>> Handle(DecideCommand command, CancellationToken cancellationToken)
      {
          var call = await calls.FindAsync(command.ActionId, cancellationToken);
          if (call is null) return Result.Fail(ToolCallErrors.NotFound);

          var allowed = call.CanDecide(command.UserId, time);
          if (allowed.IsFailed) return allowed;

          if (command.Approve)
          {
              var outcome = await executor.CallAsync(command.UserId, call.Tool, call.Arguments, cancellationToken);
              call.Complete(outcome.Text, outcome.Failed);
          }
          else
          {
              call.Deny();
          }

          var view = ActionView.From(call, time.GetUtcNow());

          if (call.RunId != Guid.Empty)
          {
              var run = await runs.FindAsync(call.RunId, cancellationToken);
              if (run is not null && !string.IsNullOrWhiteSpace(run.Channel) && run.Reference is not null)
              {
                  var action = Cards.Action(view);
                  await events.PublishAsync(
                      new AssistantResponded(call.Id, run.Channel, run.Reference, action.Description, [action]),
                      cancellationToken);
              }
          }

          return view;
      }
  }
  ```
- **[`src/Jarvis/Jarvis.Infrastructure/Messaging/AssistantConfirmedConsumer.cs`](file:///Users/adrianczubaty/Documents/RiderProjects/HomeApps/src/Jarvis/Jarvis.Infrastructure/Messaging/AssistantConfirmedConsumer.cs):**
  W przypadku błędu komendy (`NotPending` lub `Expired`) publikujemy aktualny stan akcji, zapobiegając zawieszeniu spinnera:
  ```csharp
  public sealed class AssistantConfirmedConsumer(
      ISender sender,
      IToolCallRepository calls,
      IEventBus events,
      TimeProvider time) : IConsumer<AssistantConfirmed>
  {
      public async Task Consume(ConsumeContext<AssistantConfirmed> context)
      {
          var confirmed = context.Message;
          var decided = await sender.Send(new DecideCommand(confirmed.UserId, confirmed.ConfirmationId, confirmed.Approved), context.CancellationToken);
          if (decided.IsSuccess)
          {
              // DecideHandler wyemitował już AssistantResponded.
              return;
          }

          // Jeśli akcja została już rozstrzygnięta lub wygasła, pobieramy obecny stan i wysyłamy do czatu:
          var call = await calls.FindAsync(confirmed.ConfirmationId, context.CancellationToken);
          if (call is not null)
          {
              var action = Cards.Action(ActionView.From(call, time.GetUtcNow()));
              await events.PublishAsync(
                  new AssistantResponded(confirmed.ConfirmationId, confirmed.Channel, confirmed.Reference, action.Description, [action]),
                  context.CancellationToken);
          }
      }
  }
  ```

#### 2. HomeMix: Optymistyczna współbieżność zapisu tablicy (Spec 2.18)
- **[`src/HomeMix/HomeMix.Domain/Boards/BoardErrors.cs`](file:///Users/adrianczubaty/Documents/RiderProjects/HomeApps/src/HomeMix/HomeMix.Domain/Boards/BoardErrors.cs):**
  ```csharp
  public static Error Conflict => new AppError("homemix.board_conflict", "The board was modified by someone else.", ErrorKind.Conflict);
  ```
- **[`src/HomeMix/HomeMix.Api/Requests/SaveBoardRequest.cs`](file:///Users/adrianczubaty/Documents/RiderProjects/HomeApps/src/HomeMix/HomeMix.Api/Requests/SaveBoardRequest.cs):**
  ```csharp
  public sealed record SaveBoardRequest(IReadOnlyList<BoardItemInput> Items, DateTimeOffset? ExpectedUpdatedAt = null, bool Force = false);
  ```
- **[`src/HomeMix/HomeMix.Application/Boards/SaveBoard/SaveBoardCommand.cs`](file:///Users/adrianczubaty/Documents/RiderProjects/HomeApps/src/HomeMix/HomeMix.Application/Boards/SaveBoard/SaveBoardCommand.cs):**
  ```csharp
  public sealed record SaveBoardCommand(Guid HouseholdId, string Place, IReadOnlyList<BoardItemInput> Items, DateTimeOffset? ExpectedUpdatedAt = null, bool Force = false) : ICommand<BoardView>;
  ```
- **[`src/HomeMix/HomeMix.Infrastructure/Persistence/Configurations/WidgetBoardConfiguration.cs:13-28`](file:///Users/adrianczubaty/Documents/RiderProjects/HomeApps/src/HomeMix/HomeMix.Infrastructure/Persistence/Configurations/WidgetBoardConfiguration.cs#L13-L28):**
  Skonfigurować token współbieżności:
  ```csharp
  builder.Property(b => b.UpdatedAt).IsConcurrencyToken();
  ```
- **[`src/HomeMix/HomeMix.Application/Boards/SaveBoard/SaveBoardHandler.cs:31-47`](file:///Users/adrianczubaty/Documents/RiderProjects/HomeApps/src/HomeMix/HomeMix.Application/Boards/SaveBoard/SaveBoardHandler.cs#L31-L47):**
  ```csharp
  var board = await boards.FindAsync(command.HouseholdId, command.Place, cancellationToken);
  if (!command.Force && board is not null)
  {
      if (command.ExpectedUpdatedAt is null || Math.Abs((board.UpdatedAt - command.ExpectedUpdatedAt.Value).TotalMilliseconds) > 1)
      {
          return Result.Fail(BoardErrors.Conflict);
      }
  }

  if (board is null)
  {
      board = WidgetBoard.Create(command.HouseholdId, command.Place);
      boards.Add(board);
  }

  var items = command.Items.Select(i => new BoardItem(i.Key, i.X, i.Y, i.W, i.H, i.Fit ?? BoardFit.Center)).ToList();
  var arranged = board.Arrange(items, currentUser.RequiredUserId, time.GetUtcNow());
  if (arranged.IsFailed) return arranged;

  try
  {
      realtime.Add(RealtimeEvents.BoardChanged(command.HouseholdId, command.Place));
      return BoardView.From(board);
  }
  catch (DbUpdateConcurrencyException)
  {
      return Result.Fail(BoardErrors.Conflict);
  }
  ```

#### 3. Frontend (`src/Web`): Swobodny rozmiar widżetów, stabilny zapis i obsługa konfliktów
- **[`src/Web/src/features/widgets/board.ts:66-79`](file:///Users/adrianczubaty/Documents/RiderProjects/HomeApps/src/Web/src/features/widgets/board.ts#L66-L79):**
  ```ts
  const mixes = placedWidgets(installations.value ?? [], place).map(
    ({ installation, widget, size }): BoardWidget => {
      const declaredSizes = (widget.sizes as WidgetSize[]) ?? []
      const allowedWidths = declaredSizes.length > 0 ? widthsFor(declaredSizes) : []
      const minW = allowedWidths.length > 0 ? Math.min(...allowedWidths) : 1
      const maxW = allowedWidths.length > 0 ? Math.max(...allowedWidths) : 8
      const contiguousWidths = Array.from({ length: maxW - minW + 1 }, (_, i) => minW + i)

      return {
        key: `mix:${installation.id}/${widget.id}`,
        kind: 'mix',
        installation,
        widget,
        size,
        title: localized(widget.title),
        icon: `pi ${installation.icon}`,
        widths: contiguousWidths,
        w: widthOf[size],
        fixedWidth: false,
      }
    },
  )
  ```
  Zaktualizować `useHouseholdBoard`:
  ```ts
  const isConfigured = computed(() => board.data.value?.updatedAt != null)
  const cells = computed(() =>
    arrange(
      (board.data.value?.items ?? []).map((i) => ({
        key: i.key,
        x: Number(i.x),
        y: Number(i.y),
        w: Number(i.w),
        h: Number(i.h),
        fit: i.fit as Fit,
      })),
      widgets.value,
      isConfigured.value,
    ),
  )
  ```
  Rozszerzyć `boardsApi.save`:
  ```ts
  async save(householdId: string, place: BoardPlace, items: Cell[], expectedUpdatedAt?: string | null, force = false) {
    return (await http.put<Board>(`/homemix/api/v1/households/${householdId}/boards/${place}`, {
      items,
      expectedUpdatedAt,
      force,
    })).data
  }
  ```
- **[`src/Web/src/features/widgets/layout.ts:79-108`](file:///Users/adrianczubaty/Documents/RiderProjects/HomeApps/src/Web/src/features/widgets/layout.ts#L79-L108):**
  ```ts
  export function arrange(saved: readonly Cell[], available: readonly Available[], isConfigured = false): Cell[] {
    const byKey = new Map(available.map((a) => [a.key, a]))
    const only = available.length === 1 ? available[0]! : null
    if (!isConfigured && only && (!only.fixedWidth || only.widths.includes(SINGLE.w))) {
      const fit = saved.find((c) => c.key === only.key)?.fit
      return [{ key: only.key, x: SINGLE.x, y: 0, w: SINGLE.w, h: SINGLE.h, fit: fit === 'Fill' ? 'Fill' : 'Center' }]
    }
    const placed: Cell[] = []
    const ordered = [...saved].sort((a, b) => a.y - b.y || a.x - b.x)
    for (const cell of ordered) {
      const widget = byKey.get(cell.key)
      if (!widget || placed.some((p) => p.key === cell.key)) continue
      const w = widget.fixedWidth
        ? widget.widths.includes(cell.w) && sizeFor(cell.w) === sizeFor(widget.w)
          ? cell.w
          : widget.w
        : nearest(cell.w, widget.widths)
      const h = Math.min(Math.max(1, cell.h), MAX_H)
      const x = Math.min(Math.max(cell.x, 0), COLUMNS - w)
      let y = Math.max(0, cell.y)
      while (placed.some((p) => overlaps(p, { x, y, w, h }))) y++
      placed.push({ key: cell.key, x, y, w, h, fit: cell.fit === 'Fill' ? 'Fill' : 'Center' })
    }
    if (!isConfigured) {
      for (const widget of available) {
        if (placed.some((p) => p.key === widget.key)) continue
        const h = defaultHeight(widget.w)
        placed.push({ key: widget.key, ...firstFree(placed, widget.w, h), w: widget.w, h, fit: 'Center' })
      }
    }
    return placed
  }
  ```
- **[`src/Web/src/features/widgets/BoardEditor.vue`](file:///Users/adrianczubaty/Documents/RiderProjects/HomeApps/src/Web/src/features/widgets/BoardEditor.vue):**
  Obsługa `save(items: Item[], force = false)` z zachowaniem przycisku Reset i bezpiecznym callbackiem w `submit`:
  ```ts
  const props = defineProps<{
    household: Household
    place: BoardPlace
    cells: Cell[]
    byKey: Map<string, BoardWidget>
    updatedAt?: string | null
  }>()

  const conflictDialogVisible = ref(false)

  async function save(itemsToSave: Item[], force = false) {
    const cellsToSave = itemsToSave.map(({ i, x, y, w, h, fit }) => ({ key: i, x, y, w, h, fit }))
    const ok = await submit(
      async () => {
        const saved = await boardsApi.save(
          props.household.id,
          props.place,
          cellsToSave,
          props.updatedAt,
          force,
        )
        // Natychmiastowa aktualizacja cache zapobiega wyścigowi z powiadomieniem realtime
        queryClient.setQueryData(boardKeys.one(props.household.id, props.place), saved)

        // Aktualizacja presetu w instalacjach tylko przy faktycznej zmianie progu rozmiaru
        for (const item of itemsToSave) {
          const w = widget(item)
          if (w.kind === 'mix' && sizeFor(item.w) !== w.size) {
            await homemixApi.chooseWidget(w.installation.id, props.place, w.widget.id, sizeFor(item.w))
          }
        }
        await queryClient.invalidateQueries({ queryKey: mixKeys.all })
      },
      (problem) => {
        if (problem.status === 409) {
          conflictDialogVisible.value = true
          return true // Handled: tłumi ogólny formError i zapobiega zamknięciu edytora
        }
        formError.value = t('widgets.editor.saveFailed')
        return true // Handled
      },
    )
    if (ok) emit('done')
  }
  ```
- **[`src/Web/src/features/widgets/WidgetTile.vue:50`](file:///Users/adrianczubaty/Documents/RiderProjects/HomeApps/src/Web/src/features/widgets/WidgetTile.vue#L50):**
  Dodać powiązanie klucza `:key="widget.size"` na `<MixFrame>`, gwarantując odświeżenie kontekstu po zmianie progu rozmiaru:
  ```vue
  <MixFrame
    v-if="widget.kind === 'mix'"
    :key="widget.size"
    :installation="widget.installation"
    :household="household"
    page=""
    :file="widget.widget.entry"
    :widget="{ id: widget.widget.id, place, size: widget.size }"
    fill
  />
  ```
- **[`src/Web/src/features/households/pages/WidgetsTab.vue:60`](file:///Users/adrianczubaty/Documents/RiderProjects/HomeApps/src/Web/src/features/households/pages/WidgetsTab.vue#L60):**
  Przekazać `:updated-at="board.board.data.value?.updatedAt"` do `BoardEditor`.
- **[`src/Web/src/shared/i18n/pl.json`](file:///Users/adrianczubaty/Documents/RiderProjects/HomeApps/src/Web/src/shared/i18n/pl.json):**
  Dodać brakujące klucze błędów i komunikaty:
  ```json
  "widgets": {
    "editor": {
      "saveFailed": "Nie udało się zapisać układu. Twoje zmiany są nadal tutaj, spróbuj jeszcze raz.",
      "conflictTitle": "Układ zmienił się w międzyczasie",
      "conflictMessage": "Ktoś inny zmodyfikował układ tej tablicy podczas Twojej edycji. Możesz nadpisać zmiany swoją wersją lub wczytać najnowszy stan.",
      "overwrite": "Nadpisz mimo to",
      "reload": "Wczytaj nowszy układ"
    }
  },
  "errors": {
    "assistant": {
      "not_waiting": "Ta akcja już nie czeka na potwierdzenie.",
      "not_asker": "Tylko osoba, która prosiła JARVIS-a, może potwierdzić tę akcję."
    },
    "homemix": {
      "board_conflict": "Układ został zmodyfikowany przez innego użytkownika."
    }
  }
  ```

---

### B. Repozytorium: `../HomeApps.Chat` (Wtyczka Chat)

#### 1. Konsumpcja odpowiedzi JARVIS i odświeżanie kart w czacie
- **[`src/Chat.Application/Assistant/ReceiveAnswer/ReceiveAssistantAnswerHandler.cs:26-38`](file:///Users/adrianczubaty/Documents/RiderProjects/HomeApps.Chat/src/Chat.Application/Assistant/ReceiveAnswer/ReceiveAssistantAnswerHandler.cs#L26-L38):**
  ```csharp
  var incomingAction = command.Actions.FirstOrDefault();
  var updated = incomingAction is not null
      ? replies.FirstOrDefault(r => r.UpdateAction(incomingAction))
      : null;

  if (updated is not null)
  {
      after.Signal(conversation, SignalNames.MessageEdited,
          new { asked.HouseholdId, ConversationId = conversation.Id, MessageId = updated.Id });
      return Result.Ok();
  }
  ```
- **[`src/Chat.Web/src/messages/pl.ts:111`](file:///Users/adrianczubaty/Documents/RiderProjects/HomeApps.Chat/src/Chat.Web/src/messages/pl.ts#L111):**
  Poprawić etykietę oczekiwania na zgodną ze specyfikacją:
  ```ts
  status: { Done: 'Zrobione', Failed: 'Nie udało się', NeedsConfirmation: 'Czeka na potwierdzenie', Denied: 'Anulowane', Expired: 'Wygasło' },
  ```
- **[`src/Chat.Web/src/ConversationPane.vue:168-176`](file:///Users/adrianczubaty/Documents/RiderProjects/HomeApps.Chat/src/Chat.Web/src/ConversationPane.vue#L168-L176):**
  Wymusić odświeżenie karty przy błędzie decyzji:
  ```ts
  async function decide(message: MessageView, confirmationId: string, approve: boolean, done: () => void) {
    try {
      await api.value.decide(message.id, confirmationId, approve)
    } catch (error) {
      fail(error)
      void thread.refresh(message.id)
    } finally {
      done()
    }
  }
  ```
- **[`src/Chat.Web/src/ActionCard.vue`](file:///Users/adrianczubaty/Documents/RiderProjects/HomeApps.Chat/src/Chat.Web/src/ActionCard.vue):**
  Utrzymywać stan `localInFlight` aż do momentu faktycznej zmiany statusu karty lub błędu sieciowego:
  ```ts
  const localInFlight = ref<Set<string>>(new Set())

  function decide(action: AssistantAction, approve: boolean) {
    if (!action.confirmationId) return
    localInFlight.value.add(action.confirmationId)
    emit('decide', action.confirmationId, approve, () => {
      localInFlight.value.delete(action.confirmationId!)
    })
  }

  const isPendingDecision = (action: AssistantAction) =>
    localInFlight.value.has(action.confirmationId ?? '')
  ```

---

## 5. Etapy wdrożenia (Stages)

Wdrożenie dzieli się na dwa w pełni niezależne tory (Track A oraz Track B), które mogą być realizowane równolegle.

```
       [Track A: JARVIS & Chat]                     [Track B: HomeMix & Widgets]
                  │                                              │
         Stage 1: JARVIS Core                           Stage 3: HomeMix Core
     (HomeApps: Cards, DecideHandler,             (HomeApps: Board Concurrency,
       AssistantConfirmedConsumer)                  WidgetBoardConfiguration)
                  │                                              │
                  ▼                                              ▼
          Stage 2: Chat Plugin                           Stage 4: Web Frontend
   (../HomeApps.Chat: ReceiveAnswer,               (HomeApps: board.ts, layout.ts,
     ConversationPane, ActionCard)                  BoardEditor, WidgetTile, i18n)
                  │                                              │
                  └──────────────────────┬───────────────────────┘
                                         ▼
                             Stage 5: Verification & Gate
```

| Etap | Tor / Tryb | Repozytorium | Zakres | Zależności |
|---|---|---|---|---|
| **Stage 1** | Track A (JARVIS) | `HomeApps` | **JARVIS backend:** zachowanie `view.Id` w `Cards.Action`, publikacja w `DecideHandler`, obsługa błędu w `AssistantConfirmedConsumer`. | Brak |
| **Stage 2** | Track A (Chat) | `../HomeApps.Chat` | **Chat backend & Web:** obsługa `AssistantResponded` w `ReceiveAssistantAnswerHandler`, `thread.refresh` w `ConversationPane.vue`, etykieta `Czeka na potwierdzenie`. | Stage 1 |
| **Stage 3** | Track B (HomeMix) | `HomeApps` | **HomeMix backend:** dodanie `ExpectedUpdatedAt` i `Force`, token EF Core `IsConcurrencyToken()`, obsługa `409 Conflict`. | Brak |
| **Stage 4** | Track B (Frontend) | `HomeApps` | **Web UI:** swobodna szerokość w `board.ts`, `isConfigured` w `layout.ts`, powiązanie `:key` w `WidgetTile.vue`, dialog 409 i obsługa resetu w `BoardEditor.vue`. | Stage 3 |
| **Stage 5** | Integration | Obie bazy | Uruchomienie obowiązkowych testów akceptacyjnych P2 i P5 oraz regresji. | Stage 2, Stage 4 |

---

## 6. Niezmienniki współbieżności i bezpieczeństwo

1. **Idempotentność i atomowość decyzji narzędzia:** Ponowna próba decyzji (na drugim ekranie lub po wygaśnięciu) zwraca 409 (`ToolCallErrors.NotPending`), publikując aktualny stan i nie powodując ponownego wywołania narzędzia.
2. **Optymistyczna izolacja siatki:** Zapis z niezgodnym znacznikiem czasu jest odrzucany z kodem 409. Tylko jawna flaga `Force = true` nadpisuje bazę danych.
3. **Odporność edytora na awarie zapisu (Spec 2.17):** Edytor nigdy nie zamyka się i nie gubi stanu roboczego użytkownika przy kodach błędów 4xx/5xx.
4. **Izolacja uprawnień:** Modyfikacja tablicy wymaga roli `HouseholdRole.Admin` lub `Owner`. Podjęcie decyzji o potwierdzeniu narzędzia jest dozwolone wyłącznie dla osoby, która zadała pytanie JARVIS-owi (`AssistantNotAsker`).

---

## 7. Plan weryfikacji i testy (Verification Plan)

### Obowiązkowe automatyczne testy akceptacyjne (Mandatory Automated Acceptance Tests)

> [!IMPORTANT]
> Zgodnie ze specyfikacją §6 scenariusze P2 oraz P5 są **bezwzględnie obowiązkowymi testami akceptacyjnymi** i muszą posiadać pełne pokrycie w testach automatycznych:

- **[P2 - Mandatory Acceptance Test] Decyzja na czacie domu bez odświeżania strony:**
  - *Test integracyjny / E2E:* Domownik zadaje pytanie na czacie wymagające akcji (np. zmiana głośności). Karta otrzymuje stan `Czeka na potwierdzenie`. Użytkownik klika „Potwierdź”.
  - *Asercja:* Wiadomość otrzymuje zdarzenie SignalR `MessageEdited`, status zmienia się na `Zrobione`, przyciski znikają z DOM w czasie < 2 s bez przeładowania strony. Przy próbie kliknięcia w drugim oknie wyświetla się komunikat `Ta akcja już nie czeka na potwierdzenie.` i karta natychmiast synchronizuje stan z bazą.
- **[P5 - Mandatory Acceptance Test] Trwałość zapisanego układu po zapisie (Spec 2.14):**
  - *Test automatyczny komponentu (`BoardEditor.spec.ts` & `layout.spec.ts`):* Administrator zdejmuje widżet na paletę lub zmienia szerokość widżetu dodatku HomeMix o 1 kolumnę (np. z 4 do 5) i klika „Zapisz”.
  - *Asercja:* Natychmiast po zapisie `WidgetGrid` renderuje dokładnie zapisane współrzędne; odczekanie na zdarzenia SignalR `board.changed` nie cofa kafelka na siatkę ani nie przywraca dawnego rozmiaru.

### Pozostałe testy jednostkowe i integracyjne

1. **JARVIS & Chat (`HomeApps` + `../HomeApps.Chat`):**
   - Test jednostkowy `Cards.Action`: zachowanie `ConfirmationId` dla statusów `Done`, `Denied`, `Failed`.
   - Test jednostkowy `DecideHandler`: weryfikacja publikacji `AssistantResponded` przy decyzji bezpośredniej przez API.
   - Test `AssistantConfirmedConsumer`: weryfikacja publikacji aktualnego stanu przy błędzie `NotPending`.
2. **HomeMix (`HomeApps`):**
   - Test jednostkowy `SaveBoardHandler`: odrzucenie zapisu kodem 409, gdy `ExpectedUpdatedAt` nie zgadza się ze stanem bazy; sukces przy `Force = true`.
   - Test współbieżności EF Core: weryfikacja wyrzucenia i poprawnego obsłużenia `DbUpdateConcurrencyException`.
3. **Frontend Vitest (`src/Web`):**
   - `layout.spec.ts`: test `arrange()` z `isConfigured = true` nie przywracający zdjętych widżetów; weryfikacja, że reguła pojedynczego widżetu (`only`) nie nadpisuje zapisanego rozmiaru.
   - `BoardEditor.spec.ts`: test zachowania działania przycisku Reset (`save([])`), weryfikacja synchronicznego `setQueryData` oraz otwarcie dialogu konfliktu przy kodzie 409 bez zamykania edytora.

---

