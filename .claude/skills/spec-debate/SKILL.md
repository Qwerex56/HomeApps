---
name: spec-debate
description: Run the HomeApps spec debate (dotnet-architect, vue-architect, devils-advocate as an agent team) over a spec in specs/ and write the agreed contract to specs/<name>-adr.md. Run by hand as /spec-debate @specs/<name>.md.
disable-model-invocation: true
argument-hint: "@specs/<name>.md"
---

# /spec-debate: from spec to ADR

You are the **team lead**. You don't design the feature yourself: you run the debate, keep it short, take the
open questions to the owner and write the ADR. Talk to the owner in Polish; the ADR is in Polish too (code names,
routes and error codes stay as in the code).

Input: `$ARGUMENTS` (a path such as `specs/household-shopping.md`). `<name>` = the file name without `.md`.

## 0. Check the input

1. The spec exists and follows `specs/_TEMPLATE.md` (sections 1–5). If section 2 has no Given/When/Then criteria,
   stop and tell the owner what is missing.
2. If `specs/<name>-adr.md` already exists, ask whether to revise it (keep resolved questions) or start over.
3. `git fetch` and note the current branch; the debate reads code, so it should be on the branch the work will start
   from (`master`, or the integration branch the owner named, e.g. `smart-devices/main`).

## 1. Start the team

Agent teams must be on: `CLAUDE_CODE_EXPERIMENTAL_AGENT_TEAMS=1` (set in `.claude/settings.json`). Create an agent
team with three teammates, using the agent definitions from `.claude/agents/`:

| Teammate | Agent type | Model | Focus |
|---|---|---|---|
| `dotnet` | `dotnet-architect` | Sonnet | owner service, API contract, data, migration, messages, concurrency |
| `vue` | `vue-architect` | Sonnet | screens, routes, states per role, forms, query keys, pl.json |
| `devil` | `devils-advocate` | Opus | ambiguity, edge cases, permissions/privacy, integration risks, scope creep |

Spawn prompt for each (fill in the path):

> Read `specs/<name>.md` in full. If your skill file isn't loaded, read it from `.claude/skills/<skill>/SKILL.md`.
> Study the code the spec touches. Then debate peer to peer with the other two teammates (`dotnet`, `vue`, `devil`):
> send your objections and contract proposals directly to the teammate concerned, answer theirs, converge.
> Read-only: don't edit any file. When you agree or after 3 exchanges per topic, send me your final position in the
> format from your agent definition.

Rules for the lead during the debate:
- Don't relay messages that teammates can send each other; step in only to unblock or cut a loop.
- Each topic gets at most 3 back-and-forth exchanges; then it becomes an open question or a recorded disagreement.
- Wait for all three final positions before writing anything.
- If agent teams are not available, run the three agents as subagents in two rounds instead: round 1 positions
  (in parallel), round 2 each gets the other two positions and answers them; you merge.

## 2. Report to the owner (in the chat, not a file)

Short, in Polish:
1. **Pytania do autora specyfikacji** (merged and de-duplicated from all three): numbered, each with options and
   the team's recommendation. Ask them with the question tool when available; otherwise as a numbered list.
2. **Proponowany kontrakt**: endpoints, payloads, codes, data, in a few lines.
3. **Najważniejsze ryzyka** (top 5) and anything the team wants to drop as out of scope.

Wait for the owner's answers. Don't write the ADR before the open questions are answered or explicitly deferred.
When an answer changes the contract, ask the affected teammate for a corrected position (one message), then clean up
the team.

## 3. Write `specs/<name>-adr.md`

```markdown
# ADR: <Nazwa funkcji>

Specyfikacja: [specs/<name>.md](<name>.md) · Data: <YYYY-MM-DD> · Status: zaakceptowany
Zespół: dotnet-architect (Sonnet), vue-architect (Sonnet), devils-advocate (Opus)

## 1. Rozstrzygnięte pytania i niejasności
| # | Pytanie | Odpowiedź autora | Skutek dla implementacji |
|---|---|---|---|

## 2. Uzgodniony kontrakt API i model danych
### Serwis i agregaty
### Endpointy
| Metoda | Ścieżka | Kto | Body | Odpowiedź | Błędy (status, code) |
|---|---|---|---|---|---|
### Model danych i migracja
### Zdarzenia między serwisami (HomeApps.Contracts)
### Front end: ekrany, trasy, klucze zapytań, nowe klucze pl.json

## 3. Ryzyka i „na co uważać”
### Backend
### Frontend
### Testy (scenariusze, które muszą powstać)

## 4. Odrzucone i odłożone
```

Rules for the ADR:
- Only what was agreed; disputed points that the owner decided show the decision, not the debate.
- Every error the UI must handle appears in the endpoint table with its status and code.
- Section 3 "Testy" lists concrete scenarios; the tester agent turns them into tests.
- A decision that changes how **every** service works also needs a numbered ADR in `docs/adr/` (propose it to the
  owner; don't write it unasked).

## 4. Finish

Tell the owner (Polish, 3–5 lines): the ADR path, how many questions were resolved, the top risks, and the next step
`/run-feature @specs/<name>.md`. Don't commit unless the owner asks; the ADR is committed with the feature branch.
