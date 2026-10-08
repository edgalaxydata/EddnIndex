import re
import shlex
import sys
from pathlib import Path

dockerfile, target = sys.argv[1], sys.argv[2]

# Join Dockerfile line continuations.
logical_lines = []
pending = ""
for raw_line in Path(dockerfile).read_text().splitlines():
    line = raw_line.strip()
    if not line or line.startswith("#"):
        continue
    pending = f"{pending} {line}".strip()
    if pending.endswith("\\"):
        pending = pending[:-1].rstrip()
        continue
    logical_lines.append(pending)
    pending = ""

if pending:
    logical_lines.append(pending)

stages = []
for line in logical_lines:
    tokens = shlex.split(line, comments=True)
    if not tokens or tokens[0].upper() != "FROM":
        continue

    i = 1
    while i < len(tokens) and tokens[i].startswith("--"):
        i += 1

    image = tokens[i]
    alias = tokens[i + 2] if len(tokens) > i + 2 and tokens[i + 1].upper() == "AS" else str(len(stages))
    stages.append((alias.lower(), image))

stage_map = dict(stages)
if target.lower() not in stage_map:
    raise SystemExit(f"Target stage not found: {target}")

image = stage_map[target.lower()]
while image.lower() in stage_map:
    image = stage_map[image.lower()]

print(image)