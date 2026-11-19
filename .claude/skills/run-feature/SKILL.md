---
name: run-feature
description: Build an approved HomeApps spec end to end with subagents (developer Sonnet → tester Sonnet → reviewer Opus, max 3 review rounds) on feat/<name>, then push and open a GitHub PR for the owner. Run by hand as /run-feature @specs/<name>.md.
disable-model-invocation: true
argument-hint: "@specs/<name>.md [--base <branch>]"
---

# /run-feature: spec → reviewed branch → pull request

You are the **orchestrator**. You don't write feature code or tests yourself: you prepare the branch, start the
subagents in order, pass files between them, keep a short status for the owner and open the PR. Talk to the owner in
Polish; code, commits and the PR are in English.

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

## 2. Branch

- `feat/<name>` doesn't exist: `git switch -c feat/<name> origin/<base>`.
- It exists (a resumed run): `git switch feat/<name>`; read `.claude/runs/<name>/` to see where it stopped.
- Commit the spec and ADR if they aren't committed yet: `git add specs/<name>.md specs/<name>-adr.md`,
  `git commit -m "Specs: <name>"`.

## 3. The loop

Start each subagent with the Agent tool and its agent type; give it **paths, not file contents**, so it reads only
what it needs. Each subagent waits for the previous one.

**Step A, developer** (`developer`, Sonnet):
> Implement `specs/<name>.md` following `specs/<name>-adr.md`. Base branch `<base>`, you are on `feat/<name>`.
> [Round 2+: Fix only the items in `.claude/runs/<name>/review-feedback.md` | the failures in `.claude/runs/<name>/test-report.md`.]

If it reports `BLOCKED` (spec/ADR gap), stop and ask the owner; don't guess around a spec gap.

**Step B, tester** (`tester`, Sonnet):
> Test `specs/<name>.md` and `specs/<name>-adr.md` on `feat/<name>` against base `<base>`. Write missing tests, run
> everything, return the report.

Save its report to `.claude/runs/<name>/test-report.md`. If `VERDICT: FAIL` → back to Step A with the test report
(this "test loop" may repeat at most **3 times** per round; then stop and ask the owner). Tests that were NOT RUN
because Docker is off: ask the owner to start Docker once; if they can't, carry on and say so in the PR.

**Step C, reviewer** (`reviewer`, Opus, effort high):
> Review `feat/<name>` against `<base>` for `specs/<name>.md` and `specs/<name>-adr.md`. Test report:
> `.claude/runs/<name>/test-report.md`. [Round 2+: previous findings in `.claude/runs/<name>/review-feedback.md`,
> previous head `<sha>`.]

Save its output to `.claude/runs/<name>/review-feedback.md` (overwrite; keep the round number at the top).
- `APPROVED` → step 4.
- `CHANGES_REQUESTED` → Step A with the feedback, then B, then C again.
- **At most 3 review rounds.** After the third `CHANGES_REQUESTED`, stop: leave the branch as it is (no push, no
  PR), and tell the owner which findings are still open and what you recommend (accept as MINOR, change the spec,
  or another round).

Between steps, give the owner one short status line (e.g. "Runda 1: developer gotowy, tester: PASS 42/42,
reviewer pracuje"). No replay of what the agents did.

## 4. Commits, push, pull request

1. `git log --oneline origin/<base>..HEAD`: commits must be topical and readable (spec, domain, API, front end,
   tests, fixes). Never rewrite history that was already pushed; locally, squash only "fix review" noise when it
   makes the story clearer (`git reset --soft` + new commits is fine before the first push). Never commit
   `.claude/runs/`.
2. `git push -u origin feat/<name>`. **Never merge into the base branch, never push to `master`, never force-push.**
3. Usage report: `python3 .claude/scripts/token-usage.py --since <start time> --markdown > .claude/runs/<name>/usage.md`.
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
Verdict: APPROVED after <n> round(s). Remaining minor points: <list or "none">.

## Please look closely at
- <from the reviewer's "For the human reviewer" + any choice the ADR didn't settle>

## Agent usage
<the table from usage.md + output share per model>
```

5. Without `gh` (or not signed in: `gh auth status`): print the PR body, the push command and the compare URL
   `https://github.com/Qwerex56/HomeApps/compare/<base>...feat/<name>?expand=1` for the owner.

## 5. Final message to the owner (Polish, under 15 lines)

PR link (or the manual steps), rounds used, test totals, what to read closely vs skim (commits in reading order),
choices the ADR didn't settle, and the token table's model shares. Then stop: the owner reviews and merges on GitHub.

## Rules for unattended runs

- Anything that needs the owner (spec gap, 3 rounds used, Docker missing for a feature that is mostly API) → stop and ask.
- Nothing irreversible: no merge, no force push, no deploy, no branch deletion, no changes to `master`.
- Never let an agent skip, disable or delete tests, or edit `specs/` (the reviewer treats it as a BLOCKER).
