# HomeApps for Claude Code

HomeApps is a self-hosted smart home for families: .NET 10 microservices (Auth, Accounts, Households, Notifications,
HomeMix, Devices, Jarvis) on a shared kit, a Vue 3 web app (`src/Web`) and the JARVIS assistant on top (MCP tools).
The owner (team lead) designs; Claude writes the code, including all the repetitive parts.

## How work flows here

1. **Spec** (`specs/<name>.md`): written in the Claude Desktop project (Chat), business level: WHAT and acceptance criteria.
2. **Debate** (`/spec-debate @specs/<name>.md`): architects turn it into the technical ADR `specs/<name>-adr.md`; only business questions reach the owner.
3. **Build** (`/run-feature @specs/<name>.md [--base <branch>]`): developer -> tester -> reviewer -> Devil's Advocate, docs once, then a GitHub PR for the owner. Never merge into the base branch yourself.

The skills, agents (`developer`, `tester`, `reviewer`, `devils-advocate`, `docs-writer`, `mapper`, ...) and scripts live in
`~/.agent-hub` (shared with Antigravity; see its `README.md` for who runs which role and with which model).
This repo keeps only what is specific to HomeApps: the conventions skills, `dotnet-architect`/`vue-architect`, this file.
Do not copy a hub role into the repo: a project-local file overrides the hub and then hub improvements stop applying.

- Run files: `.claude/runs/<name>/` (git-ignored). Progress across sessions: `.agents/STATUS.md` (committed, written only by the Coordinator).
- Token report per run: `python3 .claude/scripts/token-usage.py` (Claude) and `python3 ~/.agent-hub/scripts/agy-usage.py` (agy).

## Where to look first

- `docs/how-a-service-is-built.md`: the whole backend recipe. `docs/adr/`: decisions for every service.
- `docs/working-with-the-repo.md`: build, test, branches, CI. `docs/web.md`: the front end.
- Newest code to copy from: `src/Devices` (backend), `src/Web/src/features/devices` (front end).
  Older services (Households, parts of Accounts) still bundle several features in one file: don't copy that.

## Commands

| What | Command |
|---|---|
| Build everything (warnings are errors) | `dotnet build HomeApps.slnx` |
| One test project | `dotnet test tests/<Service>/<Project>` |
| All .NET tests (Api tests need Docker: Testcontainers Postgres) | `dotnet test HomeApps.slnx` |
| Front end checks (in `src/Web`) | `npm run type-check`, `npm run test:run`, `npx eslint .`, `npm run build` |

`npm run lint` runs eslint with `--fix`: use `npx eslint .` when you only want to check.

## Standing rules

- Project skills: `dotnet-conventions`, `vue-conventions`, `testing-standards`, `code-review-checklist`.
- One type per file; one folder per command/query with `Command`/`Validator`/`Handler` files.
- Lean: no new abstractions, base classes or generic repositories without a real need.
- Never touch `master` directly and never force-push. During a multi-stage plan the owner may name an integration
  branch (e.g. `smart-devices/main`); branch from it and open the PR against it.
- Never skip, disable or delete tests to get green.
- `specs/` is written by the owner and by `/spec-debate`; implementation agents and the Coordinator only read it.
  Stage progress goes to `.agents/STATUS.md`.
- Developer rules: the ADR contract (routes, field names, status and error codes) is binding; no new packages, Pinia
  stores or abstractions unless the ADR says so; never bump MediatR, MassTransit or PrimeVue. New migration:
  `dotnet tool restore && dotnet ef migrations add <Name> -p <Infrastructure project> -s <Api project> -o Persistence/Migrations`.
- Commits: small and topical, English, in the style of `git log` (`Devices: household satellites with pairing codes`).
  Never commit `.claude/runs/`, `.agents/runs/`, `.env`, `bin/`, `obj/`, `node_modules/`, `.idea/`.
- Answer the owner in Polish. Code, comments, commits and docs stay in English.
- Keep tokens low: grep and read line ranges, build/test only affected projects, pipe output through `tail`/`grep`.
