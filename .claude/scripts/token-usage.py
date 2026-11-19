#!/usr/bin/env python3
"""Token usage of a /run-feature run, per agent transcript and per model.

Claude Code keeps transcripts as JSONL under ~/.claude/projects/<project-dir-with-dashes>/ (the main session file and,
for subagents, files under <session-id>/subagents/). Each assistant line has message.model and message.usage.

Usage:
  python3 .claude/scripts/token-usage.py --since 2026-11-19T10:00        # files changed since the run started
  python3 .claude/scripts/token-usage.py --since 2026-11-19T10:00 --markdown >> .claude/runs/<name>/usage.md
"""
import argparse
import json
import os
import sys
from collections import defaultdict
from datetime import datetime
from pathlib import Path


def project_dir(cwd: str) -> Path:
    root = Path.home() / ".claude" / "projects"
    exact = root / cwd.replace("/", "-").replace(".", "-")
    if exact.exists():
        return exact
    # Fall back to the newest folder whose name ends with the repository folder name.
    tail = Path(cwd).name
    candidates = [p for p in root.glob("*") if p.is_dir() and p.name.endswith(tail)]
    if not candidates:
        sys.exit(f"No transcript folder for {cwd} under {root}")
    return max(candidates, key=lambda p: p.stat().st_mtime)


def label(path: Path) -> str:
    """The agent type when the transcript names it, else the file name."""
    try:
        with path.open(encoding="utf-8") as f:
            for i, line in enumerate(f):
                if i > 5:
                    break
                row = json.loads(line)
                for key in ("agentType", "subagent_type", "agentName"):
                    if row.get(key):
                        return str(row[key])
    except (OSError, ValueError):
        pass
    return "main" if path.parent.name != "subagents" else path.stem


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--since", required=True, help="ISO time the run started, local time, e.g. 2026-11-19T10:00")
    parser.add_argument("--cwd", default=os.getcwd())
    parser.add_argument("--markdown", action="store_true")
    args = parser.parse_args()
    since = datetime.fromisoformat(args.since).timestamp()

    rows = []
    per_model = defaultdict(int)
    for path in sorted(project_dir(args.cwd).rglob("*.jsonl"), key=lambda p: p.stat().st_mtime):
        if path.stat().st_mtime < since:
            continue
        stats = defaultdict(lambda: [0, 0, 0, 0])  # replies, output, cache write, cache read
        with path.open(encoding="utf-8") as f:
            for line in f:
                try:
                    msg = json.loads(line).get("message") or {}
                except ValueError:
                    continue
                usage, model = msg.get("usage"), msg.get("model")
                if not usage or not model or model.startswith("<"):
                    continue
                s = stats[model]
                s[0] += 1
                s[1] += usage.get("output_tokens", 0)
                s[2] += usage.get("cache_creation_input_tokens", 0)
                s[3] += usage.get("cache_read_input_tokens", 0)
        for model, (replies, out, cw, cr) in stats.items():
            rows.append((label(path), model, replies, out, cw, cr))
            per_model[model] += out

    total = sum(per_model.values()) or 1
    if args.markdown:
        print("| Step | Model | Replies | Output | Cache write | Cache read |\n|---|---|---|---|---|---|")
        for r in rows:
            print(f"| {r[0]} | {r[1]} | {r[2]} | {r[3]:,} | {r[4]:,} | {r[5]:,} |")
        print()
    else:
        for r in rows:
            print(f"{r[0]:<28} {r[1]:<28} replies={r[2]:<4} out={r[3]:<9,} cw={r[4]:<11,} cr={r[5]:,}")
    print("Output share: " + ", ".join(f"{m} {100 * n / total:.0f}%" for m, n in sorted(per_model.items(), key=lambda x: -x[1])))


if __name__ == "__main__":
    main()
