#!/usr/bin/env python3
"""
Output a JSON array of test project names affected by the current changeset.

Reads BASE_SHA and HEAD_SHA from environment, parses .csproj dependency graph,
and prints the affected test project names to stdout.
"""
import json
import os
import subprocess
import xml.etree.ElementTree as ET
from collections import defaultdict
from pathlib import Path


def parse_project_refs(csproj: Path) -> list[str]:
    try:
        root = ET.parse(csproj).getroot()
        return [
            Path(el.get("Include", "").replace("\\", "/")).stem
            for el in root.iter()
            if el.tag.endswith("ProjectReference") and el.get("Include")
        ]
    except Exception:
        return []


def main() -> None:
    repo_root = Path(os.environ.get("GITHUB_WORKSPACE", ".")).resolve()

    # Discover all csproj files and build project map
    name_to_dir: dict[str, Path] = {}
    name_to_deps: dict[str, list[str]] = {}
    for csproj in repo_root.rglob("*.csproj"):
        name = csproj.stem
        name_to_dir[name] = csproj.parent.resolve()
        name_to_deps[name] = parse_project_refs(csproj)

    # Reverse map: project → projects that depend on it
    dependents: dict[str, set[str]] = defaultdict(set)
    for proj, deps in name_to_deps.items():
        for dep in deps:
            dependents[dep].add(proj)

    # Get changed files via git diff
    base = os.environ.get("BASE_SHA", "")
    head = os.environ.get("HEAD_SHA", "HEAD")
    cmd = (
        ["git", "diff", "--name-only", f"{base}...{head}"]
        if base
        else ["git", "diff", "--name-only", "HEAD~1"]
    )
    result = subprocess.run(cmd, capture_output=True, text=True, cwd=repo_root)
    changed = [(repo_root / f).resolve() for f in result.stdout.splitlines() if f]

    # Map changed files to owning projects
    def owning(file: Path) -> str | None:
        for parent in file.parents:
            for name, d in name_to_dir.items():
                if parent == d:
                    return name
        return None

    changed_projs: set[str] = {p for f in changed if (p := owning(f)) is not None}

    # Compute transitive dependents (BFS)
    visited = set(changed_projs)
    queue = list(changed_projs)
    while queue:
        p = queue.pop()
        for dep in dependents.get(p, set()):
            if dep not in visited:
                visited.add(dep)
                queue.append(dep)

    # Collect affected test projects
    affected = sorted(
        name
        for name in name_to_dir
        if name.endswith(".Tests")
        and (
            name in visited
            or any(d in visited for d in name_to_deps.get(name, []))
        )
    )
    print(json.dumps(affected))


if __name__ == "__main__":
    main()
