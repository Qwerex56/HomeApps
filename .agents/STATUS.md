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
| release-3.0 | Stage 1 | done (PR pending) | feat/release-3.0-stage-1 | specs/release-3.0-adr.md | 2026-11-26 |
| release-3.0 | Stage 2 | done (PR #74) | feat/release-3.0-stage-2 | specs/release-3.0-adr.md | 2026-11-26 |
| release-3.0 | Stage 3 | done (PR #75) | feat/release-3.0-stage-3 | specs/release-3.0-adr.md | 2026-11-26 |
| release-3.0 | Stage 4 | done (PR pending) | feat/release-3.0-stage-4 | specs/release-3.0-adr.md | 2026-11-26 |
| fix-3.0-post-release | ADR (spec-debate) | done | | specs/fix-3.0-post-release-adr.md | 2026-11-26 |
| fix-3.0-post-release | Stages 1-4 (HomeApps + Chat) | done (PR pending) | feat/fix-3.0-post-release | specs/fix-3.0-post-release-adr.md | 2026-11-26 |
| jarvis-desktop-rework | ADR (spec-debate) | done | | specs/jarvis-desktop-rework-adr.md (stages S1-S4 here, D1-D4 in ../HomeApps.Desktop) | 2026-11-27 |
| jarvis-desktop-rework | S2 | done | feat/jarvis-desktop-rework-s2 | specs/jarvis-desktop-rework-adr.md | 2026-11-27 |
| jarvis-desktop-rework | S1 | done | feat/jarvis-desktop-rework-s1 | specs/jarvis-desktop-rework-adr.md | 2026-11-27 |
| jarvis-desktop-rework | S3 | done (PR pending) | feat/jarvis-desktop-rework-s3 | specs/jarvis-desktop-rework-adr.md | 2026-11-28 |
| jarvis-desktop-rework | D3 | done (PR HomeApps.Desktop#3) | feat/jarvis-desktop-rework-d3 | same | 2026-11-28 |
| jarvis-desktop-rework | S5 | done (PR pending) | feat/jarvis-desktop-rework-s5 | specs/jarvis-desktop-rework-adr.md (D14) | 2026-11-28 |
| jarvis-desktop-rework | S4 | done (PR pending) | feat/jarvis-desktop-rework-s4 | specs/jarvis-desktop-rework-adr.md | 2026-11-28 |
| jarvis-desktop-rework | S6 (fix: repeated identical tool call) | done (PR pending) | fix/jarvis-duplicate-tool-calls | specs/jarvis-desktop-rework-adr.md (D16) | 2026-11-28 |

## Decisions
- 2026-11-21 (owner): agy roles run directly from the Coordinator, never through a Claude subagent. Devil's Advocate on agy uses `gemini-4-argon-<effort>`.
- 2026-11-21 (owner): retro once per feature; one documenter after all stages; reviewer round 1 exhaustive; Devil's Advocate reads `diff.stat` and opens the patch only for blockers.
- 2026-11-22 (owner): household-planner Q1-Q4 per recommendation (A): Guest gets push; both recurrence modes; one overdue recurring task; imported calendars stored in full and masked as "busy".
- 2026-11-25 (owner): release-3.0: okno /v1 = cały cykl 3.x; historia usuniętego urządzenia zostaje do końca retencji; strefa domu z konta właściciela (nullable, efektywna = dom ?? właściciel ?? Europe/Warsaw).
- 2026-11-26 (owner): release-3.0 Stage 1 includes the web UI; 2.11 zone propagation (JARVIS prompt, HomeMixContext, plugin header) deferred until its contract is defined.
- 2026-11-26 (owner): release-3.0 Stage 3: raise HomeMix board MaxWidth 3 -> 8 so the editor's widths 1-8 can be saved.
- 2026-11-26 (owner): fix-3.0-post-release: nowy dodatek nie dodaje się sam na zapisaną tablicę (admin dodaje z palety); odinstalowany dodatek znika z siatki.

## Blockers
- Argon quality as a gate, Haiku as developer and agy token measurement are not measured yet (pilots).
- 2026-11-27 (owner): jarvis-desktop-rework Q1-Q4 per recommendation (A): pending setting waits indefinitely; server turn lease for multi-PC; full PC tool parity on macOS and Windows; history view-only.
