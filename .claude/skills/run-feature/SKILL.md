---
name: run-feature
description: Build an approved HomeApps spec end to end with subagents (developer Sonnet → tester Haiku → reviewer Opus, max 3 review rounds, then docs-writer Haiku) on feat/<name>, then push and open a GitHub PR for the owner. Run by hand as /run-feature @specs/<name>.md.
disable-model-invocation: true
model: sonnet
argument-hint: "@specs/<name>.md [--base <branch>]"
---

# /run-feature: spec → reviewed branch → pull request

You are the **orchestrator** and only trigger and pass things on. You don't write feature code, tests or docs and
don't analyse the code yourself (the agents do; at most `git log`/`git show --stat` checks): you prepare the branch,
start the subagents in order, pass files between them, keep a short status for the owner and open the PR. Talk to the
owner in Polish; code, commits and the PR are in English.

Input: `$ARGUMENTS`. `<name>` = spec file name without `.md`. Base branch = `--base <branch>` if given, else
`master`. (During a multi-stage plan the owner names an integration branch such as `smart-devices/main`.)
Working folder for this run: `.claude/runs/<name>/` (git-ignored).

## 1. Check the inputs

1. `specs/<name>.md` exists and has acceptance criteria (section 2). If not, stop.
2. `specs/<name>-adr.md` exists? If not, ask the owner: continue without it, or run `/spec-debate` first
   (recommend the debate for anything with new endpoints or data).
3. `git status` is clean (stop and ask if not). `git fetch origin`, then look at what landed on the base since
   the last run: `git log --oneline -15 origin/<base>`; open diffs only for files this feature will touch, and
   mention anything relevant (kit, contracts, compose, docs) to the owner in one sentence.
4. Note the start time (`date +%Y-%m-%dT%H:%M`) for the usage report.
5. **Sequential Mapper -> Developer startup**:
   - In Antigravity (`agy`) swarm or Claude Code: spawn `@mapper` in Mode 1 and **wait for `@mapper [mapper ready]`** before spawning `@developer`, ensuring `.agents/project-map.md` reflects current `HEAD`.
   - **Mandatory Stage `path:line` Cheat Sheet & Active Mode 2**: Instruct `@mapper` in Mode 1 to both update `.agents/project-map.md` AND post a **Stage `path:line` Cheat Sheet** on the board containing exact `path:line` anchors for all existing files, handlers, DTOs, endpoints, Vue components, composables, test fixtures, and doc sections touched by the current stage's ADR, then remain active in **Mode 2 (On-Demand `path:line` Locator)** for the rest of the run.
   - **Swarm-only rule in `agy`**: Forbid mixing `invoke_subagent` with `swarm` during `/run-feature`. Use `swarm{action:"add"}` exclusively from the start.
   - Pass `.agents/project-map.md` and the Mapper Cheat Sheet seq `#<n>` to all subsequent agents.

## 2. Branch

- `feat/<name>` doesn't exist: `git switch -c feat/<name> origin/<base>`.
- It exists (a resumed run): `git switch feat/<name>`; read `.claude/runs/<name>/` to see where it stopped.
- Commit the spec and ADR if they aren't committed yet: `git add specs/<name>.md specs/<name>-adr.md`,
  `git commit -m "Specs: <name>"`.

## 3. The loop

**Mode** (check at the start and again before each review round):
`curl -s -m 3 http://127.0.0.1:7340/__router/health`. `"local":true` and `"disabled":false` → **local mode**
(Sonnet/Haiku agents run on a weaker local LLM). Anything else (no router, local down, disabled) → **Anthropic mode**:
the limits below stay exactly as written. Local mode changes only this:
- **At most 5 review rounds** and **5 test loops per round** (instead of 3 and 3). Rounds already used count
  toward whichever limit applies when the mode is checked.
- **Fixes in one batch, spelled out.** Add to the reviewer's prompt: "Local mode: the developer is a weaker model.
  List every finding (BLOCKER and MINOR) in this one pass, each with file, line, the problem and the exact change
  wanted (a code snippet where it helps); no 'consider…'." Step A then gets one batch: "Fix every item in
  `review-feedback.md` and every failure in `test-report.md`, in that order, one commit per area."
- Time is not a limit: the local model is slow, so never stop a run or ask the owner because a step takes long.
- Say the mode in the first status line and in the PR's Review section ("after <n> round(s), local mode").

Start each subagent with the Agent tool and its agent type; give it **paths, not file contents** (including `.agents/project-map.md` and the `@mapper` Stage `path:line` Cheat Sheet seq `#<n>`).
- **HARD `@mapper` REQUIREMENT in every agent brief** (`developer`, `tester`, `reviewer`, `devils-advocate`, `docs-writer`): Never frame `@mapper` as optional (forbidden phrasing: *"w razie potrzeby możesz pytać `@mapper`"*). Explicitly instruct every agent: if any `path:line`, call site, symbol, DTO, component, test fixture, or doc section is not already explicitly given with line numbers in the ADR, `.agents/project-map.md`, Mapper Cheat Sheet, or `git diff`, the agent **MUST** send a batched query `@mapper Give exact path:line for: <1..N>` + `swarm_wait` instead of scanning/grepping the repo or reading full files. Each subagent waits for the previous one.

**Step A, developer** (`developer`, Sonnet):
> Implement `specs/<name>.md` following `specs/<name>-adr.md`. Base branch `<base>`, you are on `feat/<name>`. Read `.agents/project-map.md` and `@mapper`'s Stage `path:line` Cheat Sheet (`#<seq>`). HARD RULE: do not scan/grep the repo or read full files to locate symbols — for any missing `path:line` or call site, post a batched query to `@mapper` and `swarm_wait`.
> [Round 2+: Fix only the items in `.claude/runs/<name>/review-feedback.md` | the failures in `.claude/runs/<name>/test-report.md`.]

If it reports `BLOCKED` (spec/ADR gap), stop and ask the owner; don't guess around a spec gap.

**Step B, tester** (`tester`, Haiku):
> Test `specs/<name>.md` and `specs/<name>-adr.md` on `feat/<name>` against base `<base>`. Read `.agents/project-map.md` and `@mapper`'s Cheat Sheet (`#<seq>`). HARD RULE: query `@mapper` + `swarm_wait` for any `path:line` or test fixture location not in the ADR/Cheat Sheet/`git diff` instead of scanning the repo. Write missing tests, run everything, return the report.

Save its report to `.claude/runs/<name>/test-report.md`. If `VERDICT: FAIL` → back to Step A with the test report
(this "test loop" may repeat at most **3 times** per round, 5 in local mode; then stop and ask the owner). Tests that were NOT RUN
because Docker is off: ask the owner to start Docker once; if they can't, carry on and say so in the PR.

**Step C, reviewer** (`reviewer`, Opus, effort high):
> Review `feat/<name>` against `<base>` for `specs/<name>.md` and `specs/<name>-adr.md`. Test report:
> `.claude/runs/<name>/test-report.md`. Read `.agents/project-map.md` and `@mapper`'s Cheat Sheet (`#<seq>`). HARD RULE: for any symbol or `path:line` lookup outside `git diff <base>...HEAD`, query `@mapper` + `swarm_wait` instead of scanning the repo. [Round 2+: previous findings in `.claude/runs/<name>/review-feedback.md`,
> previous head `<sha>`.]

Save its output to `.claude/runs/<name>/review-feedback.md` (overwrite; keep the round number at the top).
- `APPROVED` → Step E (Devil's Advocate Gate).
- `CHANGES_REQUESTED` → Step A with the feedback, then B, then C again.
- **At most 3 review rounds** (5 in local mode). After the last allowed `CHANGES_REQUESTED`, stop: leave the branch as it is (no push, no
  PR), and tell the owner which findings are still open and what you recommend (accept as MINOR, change the spec,
  or another round).

**Step E, Devil's Advocate Gate** (`devils-advocate`, Opus high), once after `reviewer` `APPROVED`:
> Verify `<base>...HEAD` against `specs/<name>.md`, `specs/<name>-adr.md`, `.claude/runs/<name>/test-report.md`, and `.claude/runs/<name>/review-feedback.md` using `.agents/project-map.md` and `@mapper`'s Cheat Sheet (`#<seq>`). HARD RULE: query `@mapper` + `swarm_wait` for any `path:line` lookup outside `git diff <base>...HEAD`.
- `CHANGES_REQUESTED` → send exact blockers to `developer` → quick `tester` re-verify → re-check with `devils-advocate` (use `gemini-4-hydrogen-medium` on re-checks in `agy` swarm to preserve Opus quota).
- `APPROVED` → Step D.

**Step D, docs-writer** (`docs-writer`, Haiku, effort low), once, after Devil's Advocate `APPROVED`:
> Bring `docs/` and `CHANGELOG.md` in line with `feat/<name>` against base `<base>` for `specs/<name>.md` and
> `specs/<name>-adr.md`. Read `.agents/project-map.md` and `@mapper`'s Cheat Sheet (`#<seq>`). HARD RULE: query `@mapper` + `swarm_wait` for exact doc section `path:line` anchors before reading doc files.

Then check its commit touches only `docs/`, `README.md` or `CHANGELOG.md` (`git show --stat HEAD`); anything else →
revert that commit and tell the owner. No new review round for a docs-only commit; mention it in the PR.

Agents' idle notifications and progress chatter need no answer and no summary. Between steps, give the owner one short
status line (e.g. "Runda 1: developer gotowy, tester: PASS 42/42, reviewer pracuje"). No replay of what the agents
did.

## 4. Commits, push, pull request

0. **Mark the stage done in the spec** (the only edit to `specs/` allowed in a run, and only by you, after `APPROVED`
   and the docs step): in `specs/<name>.md` section 0, set the stage's row to `✅ <YYYY-MM-DD>` (add the row if the
   stage was born in the ADR, e.g. H-4c) and remove or update section 0 lines this stage settled (an open decision the
   ADR closed, a deferred item it delivered). Nothing outside section 0. Commit `Specs: <stage> done`. After the PR
   exists (step 4), put `PR #<n>` in the row's merge column in one more commit and push it.
1. `git log --oneline origin/<base>..HEAD`: commits must be topical and readable (spec, domain, API, front end,
   tests, fixes). Never rewrite history that was already pushed; locally, squash only "fix review" noise when it
   makes the story clearer (`git reset --soft` + new commits is fine before the first push). Never commit
   `.claude/runs/`.
2. `git push -u origin feat/<name>`. **Never merge into the base branch, never push to `master`, never force-push.**
3. Usage report (Claude Code only; skip/optional when running in `agy` swarm): `python3 .claude/scripts/token-usage.py --since <start time> --markdown > .claude/runs/<name>/usage.md`.
4. Pull request with `gh pr create --base <base> --head feat/<name> --title "<Feature name>" --body-file .claude/runs/<name>/pr.md`,
   body in English:

```markdown
## Summary
<2–4 sentences: what the feature does for the user>

Spec: [specs/<name>.md](specs/<name>.md) · ADR: [specs/<name>-adr.md](specs/<name>-adr.md)

## Backend (.NET)
- <service>: <use cases, endpoints, migration, messages>

## Frontend (Vue)
- <screens, routes, states, pl.json keys>

## Tests
| Suite | Result |
|---|---|
| <project> | <p>/<t> passed |
| vitest | <p>/<t> passed |
| Not run | <what and why, or "none"> |

Acceptance criteria → tests: <2.1 → TestName, …>

## Review (Opus)
Verdict: APPROVED after <n> round(s) (<mode> mode). Remaining minor points: <list or "none">.

## Please look closely at
- <from the reviewer's "For the human reviewer" + any choice the ADR didn't settle>

## Docs
<docs-writer commit and the docs it changed, or "no change needed">

## Agent usage
<the table from usage.md + output share per model>
```

5. Without `gh` (or not signed in: `gh auth status`): print the PR body, the push command and the compare URL
   `https://github.com/Qwerex56/HomeApps/compare/<base>...feat/<name>?expand=1` for the owner.
6. **Lean Retro (`/lean-retro`)**: Run `lean-analyst` after PR creation. If `ACTION_NEEDED: YES`, run `lean-integrator`, append `## Lean` summary (`ACTION_NEEDED` + concrete changes) to `.claude/runs/<name>/pr.md`, and update the PR via `gh pr edit <n> --body-file .claude/runs/<name>/pr.md`.

## 5. Final message to the owner (Polish, under 15 lines)

PR link (or the manual steps), rounds used, test totals, what to read closely vs skim (commits in reading order),
choices the ADR didn't settle, and the token table's model shares. Then stop: the owner reviews and merges on GitHub.

## Rules for unattended runs

- Anything that needs the owner (spec gap, 3 rounds used, Docker missing for a feature that is mostly API) → stop and ask.
- Nothing irreversible: no merge, no force push, no deploy, no branch deletion, no changes to `master`.
- Never let an agent skip, disable or delete tests, or edit `specs/` (the reviewer treats it as a BLOCKER). The one
  exception is your own section 0 status update in step 4.0.
