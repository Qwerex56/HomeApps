# STATUS

Written only by the Coordinator (agent teams). Read it first when a session starts.

| Feature | Stage | State | Branch | ADR | Updated |
|---|---|---|---|---|---|
| household-jarvis-devices | H-0..H-4c | done | smart-devices/* | specs/household-jarvis-devices-adr.md | 2026-11-19 |
| household-jarvis-devices | H-5 | done (PR #65) | | same | 2026-11-21 |
| household-jarvis-devices | H-6 | done (PR #64) | | same | 2026-11-19 |
| household-jarvis-devices | H-7 | done (PR #66) | | same | 2026-11-21 |
| household-jarvis-devices | H-8 | done (PR #67) | | same | 2026-11-21 |
| household-jarvis-devices | H-9 | done (PR #68) | feat/household-jarvis-devices-h9 | same | 2026-11-21 |
| harness | lean-v2 (agent hub, cheaper teams) | doing | harness/lean-v2 | | 2026-11-22 |
| household-planner | ADR (spec-debate) | done | | specs/household-planner-adr.md | 2026-11-22 |
| release-3.0 | ADR (spec-debate) | done | | specs/release-3.0-adr.md | 2026-11-25 |
| release-3.0 | Stage 0 | done (PR pending) | feat/release-3.0-stage-0 | specs/release-3.0-adr.md | 2026-11-25 |

## Decisions
- 2026-11-21 (owner): agy roles run directly from the Coordinator, never through a Claude subagent. Devil's Advocate on agy uses `gemini-4-argon-<effort>`.
- 2026-11-21 (owner): retro once per feature; one documenter after all stages; reviewer round 1 exhaustive; Devil's Advocate reads `diff.stat` and opens the patch only for blockers.
- 2026-11-22 (owner): household-planner Q1-Q4 per recommendation (A): Guest gets push; both recurrence modes; one overdue recurring task; imported calendars stored in full and masked as "busy".
- 2026-11-25 (owner): release-3.0: okno /v1 = cały cykl 3.x; historia usuniętego urządzenia zostaje do końca retencji; strefa domu z konta właściciela (nullable, efektywna = dom ?? właściciel ?? Europe/Warsaw).

## Blockers
- Argon quality as a gate, Haiku as developer and agy token measurement are not measured yet (pilots).
