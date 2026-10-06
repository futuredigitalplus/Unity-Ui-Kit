#!/usr/bin/env python3
"""Static checks of the package's Markdown, no Unity needed.

  check-docs.py [package root]      (default: the package this script is in)

Checks, in every .md file of the package except the upstream copies and rename tooling:
  1. every relative link points at a file that exists,
  2. every #anchor of a link into a Markdown file exists as a heading there,
  3. every Markdown file under Documentation~ is linked from Documentation~/index.md,
  4. every `Documentation~/...`, `Runtime/...`, `Editor/...`, `Samples~/...`, `Tools~/...`, `Tests/...` path in backticks exists
     (paths in the upstream layout, which SOURCE.md files and the upstream guides quote, are skipped).
Exit code 1 when anything is broken.
"""
import glob
import os
import re
import sys
import urllib.parse

SKIP_FILES = ("BlurUrpLegacy~", "UpstreamMiscStylesheets", "upstream-")
SKIP_PATH_FILES = ("SOURCE.md", "THIRD_PARTY_NOTICES.md", "ARCHITECTURE.md", "COMPONENTS.md", "FONTS.md", "ICONS.md", "MATERIALS.md", "MOBILE.md", "UpstreamFeatures.md")


def slug(heading):
    heading = re.sub(r"\[([^\]]*)\]\([^)]*\)", r"\1", heading)
    heading = heading.replace("`", "").strip().lower()
    heading = re.sub(r"[^\w\- ]", "", heading)
    return heading.replace(" ", "-")


def anchors(path):
    seen, found, fence = {}, set(), False
    with open(path, encoding="utf-8") as f:
        for line in f:
            if line.startswith("```"):
                fence = not fence
                continue
            match = None if fence else re.match(r"#{1,6}\s+(.*?)\s*#*\s*$", line)
            if match:
                s = slug(match.group(1))
                n = seen.get(s, 0)
                seen[s] = n + 1
                found.add(s if n == 0 else "%s-%d" % (s, n))
    return found


def main():
    root = os.path.abspath(sys.argv[1] if len(sys.argv) > 1 else os.path.join(os.path.dirname(__file__), "..", ".."))
    os.chdir(root)
    files = [f for f in glob.glob("**/*.md", recursive=True)
             if not any(s in "/" + f for s in SKIP_FILES) and not f.startswith("Tools~/rename/")]
    problems = []
    for md in sorted(files):
        with open(md, encoding="utf-8") as f:
            text = f.read()
        plain = re.sub(r"```.*?```", "", text, flags=re.S)
        plain = re.sub(r"`[^`\n]*`", "", plain)
        for match in re.finditer(r"\[[^\]]*\]\(([^)\s]+)\)", plain):
            target = match.group(1)
            if re.match(r"(https?:|mailto:)", target):
                continue
            path, _, fragment = target.partition("#")
            resolved = md if path == "" else os.path.normpath(os.path.join(os.path.dirname(md), urllib.parse.unquote(path)))
            if not os.path.exists(resolved):
                problems.append("%s: broken link %s" % (md, target))
            elif fragment and resolved.endswith(".md") and fragment not in anchors(resolved):
                problems.append("%s: broken anchor %s" % (md, target))
        if os.path.basename(md) in SKIP_PATH_FILES:
            continue
        for match in re.finditer(r"`((?:Documentation~|Runtime|Editor|Tools~|Samples~|Tests)/[^`\s*<>{}]+?)`", text):
            path = match.group(1).rstrip(".,:;)/")
            if "..." in path:
                continue
            candidates = [path, os.path.join(os.path.dirname(md), path)]
            if not any(os.path.exists(c) for c in candidates) and not glob.glob(path):
                problems.append("%s: missing path `%s`" % (md, path))
    index = open("Documentation~/index.md", encoding="utf-8").read()
    for md in sorted(glob.glob("Documentation~/*.md") + glob.glob("Documentation~/*/README.md")):
        rel = os.path.relpath(md, "Documentation~").replace(os.sep, "/")
        if rel != "index.md" and ("(%s)" % rel) not in index and ("(%s#" % rel) not in index:
            problems.append("Documentation~/index.md does not link %s" % rel)
    for p in problems:
        print(p)
    print("%d Markdown files checked, %d problems" % (len(files), len(problems)))
    sys.exit(1 if problems else 0)


if __name__ == "__main__":
    main()
