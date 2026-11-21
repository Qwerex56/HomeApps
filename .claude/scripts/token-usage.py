#!/usr/bin/env python3
"""Token usage of a /run-feature run, per agent transcript and per model.

Supports both Claude Code (~/.claude/projects/) and Google Antigravity (~/.gemini/antigravity-cli/brain/).

Usage:
  python3 .claude/scripts/token-usage.py --since 2026-11-19T10:00        # Claude Code
  python3 .claude/scripts/token-usage.py --agy --since 2026-11-19T10:00  # Antigravity CLI
  python3 .claude/scripts/token-usage.py --agy --since 2026-11-19T10:00 --markdown >> .claude/runs/<name>/usage.md
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


def label_claude(path: Path) -> str:
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


def label_agy(path: Path) -> str:
    """Extract agent roster name from Antigravity transcript or fallback to directory UUID."""
    try:
        with path.open(encoding="utf-8") as f:
            for i, line in enumerate(f):
                if i > 15:
                    break
                row = json.loads(line)
                content = row.get("content", "")
                if "You are **" in content:
                    return content.split("You are **")[1].split("**")[0].strip()
                if "You are " in content and "one agent in a swarm" in content:
                    return content.split("You are ")[1].split(",")[0].strip()
                if "recorded on the board as #" in content and "for @" in content:
                    part = content.split("for @")[1]
                    return part.split(")")[0].split(":")[0].strip()
    except (OSError, ValueError):
        pass
    # UUID fallback
    parts = path.parts
    for idx, p in enumerate(parts):
        if p == "brain" and idx + 1 < len(parts):
            return parts[idx + 1][:8]
    return path.stem


def parse_agy(brain_dir: Path, since: float):
    rows = []
    per_model = defaultdict(int)
    for path in sorted(brain_dir.rglob("transcript.jsonl"), key=lambda p: p.stat().st_mtime):
        if path.stat().st_mtime < since:
            continue
        stats = defaultdict(lambda: [0, 0, 0, 0])  # replies, output, uncached input, cache read
        try:
            with path.open(encoding="utf-8") as f:
                for line in f:
                    try:
                        row = json.loads(line)
                    except ValueError:
                        continue
                    if row.get("type") != "PLANNER_RESPONSE":
                        continue
                    model = row.get("model")
                    if not model:
                        continue
                    out_tokens = row.get("output_tokens", 0)
                    in_tokens = row.get("input_tokens", 0)
                    cr_tokens = row.get("cache_read_tokens", 0)
                    s = stats[model]
                    s[0] += 1
                    s[1] += out_tokens
                    s[2] += in_tokens
                    s[3] += cr_tokens
        except OSError:
            continue
        agent_name = label_agy(path)
        for model, (replies, out, uncached, cr) in stats.items():
            rows.append((agent_name, model, replies, out, uncached, cr))
            per_model[model] += out
    return rows, per_model


def parse_claude(cwd: str, since: float):
    rows = []
    per_model = defaultdict(int)
    for path in sorted(project_dir(cwd).rglob("*.jsonl"), key=lambda p: p.stat().st_mtime):
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
            rows.append((label_claude(path), model, replies, out, cw, cr))
            per_model[model] += out
    return rows, per_model


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--since", required=True, help="ISO time the run started, local time, e.g. 2026-11-19T10:00")
    parser.add_argument("--cwd", default=os.getcwd())
    parser.add_argument("--markdown", action="store_true")
    parser.add_argument("--agy", action="store_true", help="Parse Antigravity transcripts under ~/.gemini/antigravity-cli/brain/")
    parser.add_argument("--brain-dir", default=str(Path.home() / ".gemini" / "antigravity-cli" / "brain"))
    args = parser.parse_args()
    since = datetime.fromisoformat(args.since).timestamp()

    if args.agy:
        rows, per_model = parse_agy(Path(args.brain_dir), since)
        col3_header = "Uncached input"
    else:
        rows, per_model = parse_claude(args.cwd, since)
        col3_header = "Cache write"

    total = sum(per_model.values()) or 1
    if args.markdown:
        print(f"| Step | Model | Replies | Output | {col3_header} | Cache read |\n|---|---|---|---|---|---|")
        for r in rows:
            print(f"| {r[0]} | {r[1]} | {r[2]} | {r[3]:,} | {r[4]:,} | {r[5]:,} |")
        print()
    else:
        for r in rows:
            print(f"{r[0]:<28} {r[1]:<28} replies={r[2]:<4} out={r[3]:<9,} col3={r[4]:<11,} cr={r[5]:,}")
    print("Output share: " + ", ".join(f"{m} {100 * n / total:.0f}%" for m, n in sorted(per_model.items(), key=lambda x: -x[1])))


if __name__ == "__main__":
    main()
