#!/usr/bin/env python3
"""Turn "## Unreleased" of CHANGELOG.md into "## <version>" and open a new, empty "## Unreleased" above it.

Run by release-please.yml on the release PR, so the maintainer never edits versions by hand. The notes stay the
hand-written, user-facing ones of the Unreleased section (the GitHub release and the update window show them).
If that section is empty, the feat/fix/perf commit subjects since the previous tag are listed instead.
Idempotent: a changelog that already has "## <version>" is left as it is.

Usage: scripts/promote_changelog.py <version> [<previous tag>]
"""

import re
import subprocess
import sys
from pathlib import Path

PATH = Path("CHANGELOG.md")
UNRELEASED = "## Unreleased"


def commit_notes(previous_tag: str | None) -> list[str]:
    rev = f"{previous_tag}..HEAD" if previous_tag else "HEAD"
    try:
        subjects = subprocess.run(
            ["git", "log", "--no-merges", "--format=%s", rev], check=True, capture_output=True, text=True
        ).stdout.splitlines()
    except subprocess.CalledProcessError:
        return []
    notes = []
    for subject in subjects:
        m = re.match(r"^(feat|fix|perf)(\([^)]*\))?!?: (.+)$", subject)
        if m:
            text = re.sub(r" \(#\d+\)$", "", m.group(3))
            notes.append(f"- {text[:1].upper()}{text[1:]}")
    return notes


def promote(text: str, version: str, previous_tag: str | None) -> str:
    lines = text.splitlines()
    if f"## {version}" in lines:
        return text
    if UNRELEASED in lines:
        start = lines.index(UNRELEASED)
        end = next((i for i in range(start + 1, len(lines)) if lines[i].startswith("## ")), len(lines))
        body = lines[start + 1 : end]
    else:
        # no Unreleased section: the new version goes before the first version (or at the end)
        start = end = next((i for i, line in enumerate(lines) if line.startswith("## ")), len(lines))
        body = []
    if not any(line.strip() for line in body):
        body = [""] + (commit_notes(previous_tag) or ["- Small fixes and improvements."]) + [""]
    while body and not body[-1].strip():
        body.pop()
    section = [UNRELEASED, "", f"## {version}"] + body + [""]
    before = lines[:start]
    if before and before[-1].strip():
        before.append("")
    return "\n".join(before + section + lines[end:]).rstrip("\n") + "\n"


def main() -> None:
    if len(sys.argv) not in (2, 3):
        sys.exit(__doc__)
    version = sys.argv[1]
    previous_tag = sys.argv[2] if len(sys.argv) == 3 and sys.argv[2] else None
    text = PATH.read_text(encoding="utf-8") if PATH.exists() else "# Changelog\n\n## Unreleased\n"
    PATH.write_text(promote(text, version, previous_tag), encoding="utf-8", newline="\n")


if __name__ == "__main__":
    main()
