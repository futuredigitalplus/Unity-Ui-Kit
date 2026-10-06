#!/usr/bin/env python3
"""make_classic_kit.py -- turn the UPM-layout UI kit into the "classic" layout: a plain folder inside a project's Assets.

    python3 make_classic_kit.py --src <UPM kit root> --dst <game>/Assets/FutureDigitalPlus/UIKit [--force] [--check] [--dry-run]

The UPM source (the folder that holds package.json) stays the single source of truth. The classic copy is generated from it, never edited by hand:
to change the kit edit the source and run this script again. Python 3.9+, standard library only, no network, no timestamps in the output.

What it does
  * copies everything of the source except package.json (+ .meta), VCS folders, .DS_Store and build output (Tools~/**/bin, obj, out);
  * keeps every .meta file byte for byte, so every GUID is unchanged and assets, Resources, USS and UXML references keep resolving;
  * keeps the tilde folders (Samples~, Documentation~, Tools~) hidden from Unity, so the game does not import them;
  * writes VERSION (first line: the kit version; then the metadata that was in package.json) and its .meta (from Tools~/classic/VERSION.meta.tpl);
  * rewrites the few places that must name the install folder (a constant used in an attribute, see REWRITES below) and adds a short banner to README.md;
  * writes <dst>.meta and (for .../FutureDigitalPlus/UIKit) the parent folder .meta when they are missing, from Tools~/classic/*.meta.tpl
    (Unity-generated files, committed so the GUIDs of the folders are the same in every game);
  * writes .fdp-classic-manifest.json (a dot file, Unity ignores it): the sha256 of every file written. It is how the next run knows whether someone
    edited the copy.

Safety
  * deterministic: same source and options, same bytes; sorted traversal, LF files copied as they are;
  * idempotent: a second run changes nothing (files whose bytes already match are not touched, so Unity does not reimport them);
  * refuses to overwrite a destination that has local changes (a file that differs from the manifest, a file that was deleted, a file that is not in
    the manifest) unless --force; a non-empty destination without a manifest is refused too;
  * files that disappeared from the source are removed from the destination (mirror), with their .meta.

Options
  --src DIR          the UPM kit root (the folder with package.json)
  --dst DIR          the destination, normally <project>/Assets/FutureDigitalPlus/UIKit
  --asset-root PATH  the project relative asset path of --dst (default: derived from the last "Assets" segment of --dst)
  --exclude NAME     leave a top level entry out (repeatable), e.g. --exclude Tests --exclude Samples~. Documented in Documentation~/GettingStarted.md.
  --force            overwrite local changes and delete files that are not part of the kit
  --check            write nothing; exit 3 when --dst is not what this run would produce (CI, pre-commit); local edits count as a difference
  --dry-run          print what would change and write nothing
  --quiet            only print errors and the summary line

Exit codes: 0 done / up to date, 1 error, 2 refused (local changes), 3 --check found a difference.
"""
from __future__ import annotations

import argparse
import hashlib
import json
import os
import re
import shutil
import sys
from pathlib import Path

TOOL = "Tools~/make_classic_kit.py"
MANIFEST = ".fdp-classic-manifest.json"
MANIFEST_VERSION = 1
GENERATED_META_DIR = "Tools~/classic"          # Unity-generated .meta files, kept as *.meta.tpl (a .meta inside Tools~ would fail the lint)

# Top level entries of the source that are never copied.
SKIP_TOP = {"package.json", "package.json.meta"}
SKIP_NAMES = {".DS_Store", ".git", ".svn", ".vs", ".idea", "__pycache__"}
# Build output below Tools~ (bin, obj, out) is not kit content.
BUILD_DIRS = {"bin", "obj", "out", "__pycache__", ".build"}

# The places that must name the install folder. (relative path, regex, replacement template) -- exactly one match each, else the source drifted
# from this script and the run stops (a silent miss would leave a test pointing at Packages/ in a copy that lives in Assets/).
REWRITES = [
    ("Tests/Editor/Components/AssetDatabaseAssetResolverTests.cs",
     re.compile(r'(public const string KitAssetRoot = )"[^"]*"(;\s*// CLASSIC-REWRITE:KitAssetRoot)'),
     r'\1"{asset_root}"\2'),
]

BANNER = (
    "> **Classic copy.** This folder is the \"classic\" (plain `Assets` folder) form of the kit, version {version}, generated from the UPM source\n"
    "> by `Tools~/make_classic_kit.py`. Do not edit it by hand: change the UPM source and run the script again (see `Documentation~/Upgrading.md`,\n"
    "> \"Updating a classic copy\"). `VERSION` has the version and the package metadata; the rest of this file describes the kit in both layouts.\n"
    "> `Samples~`, `Documentation~` and `Tools~` end with `~` on purpose: Unity ignores them. Rename `Samples~` to `Samples` to import the samples.\n"
)


class Refused(Exception):
    """The destination has local changes (exit 2)."""


class Failure(Exception):
    """Bad input or a source that no longer matches this script (exit 1)."""


def sha256(data: bytes) -> str:
    return hashlib.sha256(data).hexdigest()


def read_package_json(src: Path) -> dict:
    pj = src / "package.json"
    if not pj.is_file():
        raise Failure(f"{src} has no package.json: --src must be the UPM kit root")
    return json.loads(pj.read_text(encoding="utf-8"))


def is_skipped(rel_parts, excludes) -> bool:
    if rel_parts[0] in excludes:
        return True
    if len(rel_parts) == 1 and rel_parts[0] in SKIP_TOP:
        return True
    for i, part in enumerate(rel_parts):
        if part in SKIP_NAMES:
            return True
        # the .meta of an excluded top level folder
    if rel_parts[0].endswith(".meta") and rel_parts[0][:-5] in excludes:
        return True
    if rel_parts[0] == "Tools~" and any(p in BUILD_DIRS for p in rel_parts[1:-1]):
        return True
    return False


def collect_source(src: Path, excludes) -> dict[str, bytes]:
    """Relative posix path -> bytes of every file that is copied, sorted."""
    files: dict[str, bytes] = {}
    for dirpath, dirnames, filenames in os.walk(src):
        dirnames.sort()
        d = Path(dirpath)
        rel_dir = d.relative_to(src).parts
        # prune
        keep = []
        for name in dirnames:
            parts = rel_dir + (name,)
            if name in SKIP_NAMES or (parts[0] == "Tools~" and len(parts) > 1 and name in BUILD_DIRS) or parts[0] in excludes:
                continue
            keep.append(name)
        dirnames[:] = keep
        for name in sorted(filenames):
            parts = rel_dir + (name,)
            if is_skipped(parts, excludes):
                continue
            p = d / name
            if p.is_symlink():
                raise Failure(f"symbolic link in the source: {p}")
            files["/".join(parts)] = p.read_bytes()
    return files


def derive_asset_root(dst: Path, override: str | None) -> str:
    if override:
        root = override.strip("/")
    else:
        parts = dst.resolve().parts
        if "Assets" not in parts:
            raise Failure(f"--dst {dst} is not below an Assets folder; pass --asset-root Assets/... to say where it will live")
        idx = len(parts) - 1 - parts[::-1].index("Assets")
        root = "/".join(parts[idx:])
    if not (root == "Assets" or root.startswith("Assets/")):
        raise Failure(f"asset root must start with Assets/: {root}")
    if "\\" in root or '"' in root:
        raise Failure(f"asset root must not contain backslashes or quotes: {root}")
    return root


def version_text(pj: dict, layout_note: str) -> str:
    lines = [str(pj["version"])]
    lines.append(f"package: {pj['name']}")
    lines.append(f"displayName: {pj.get('displayName', '')}")
    lines.append(f"unity: {pj.get('unity', '')}")
    deps = pj.get("dependencies", {})
    lines.append("requires: " + (", ".join(f"{k} {v}" for k, v in sorted(deps.items())) if deps else "nothing"))
    lines.append(f"license: {pj.get('license', '')}")
    lines.append(f"author: {pj.get('author', {}).get('name', '')}")
    lines.append(f"keywords: {', '.join(pj.get('keywords', []))}")
    lines.append(f"layout: {layout_note}")
    for s in pj.get("samples", []):
        lines.append(f"sample: {s['displayName']} | Samples~/{s['path'].split('/', 1)[-1]} | {s.get('description', '')}")
    lines.append(f"description: {pj.get('description', '')}")
    return "\n".join(lines) + "\n"


def build_output(src: Path, dst: Path, asset_root: str, excludes) -> tuple[dict[str, bytes], dict]:
    """Everything that belongs in dst: relative path -> bytes (the manifest is added by the caller)."""
    pj = read_package_json(src)
    out = collect_source(src, excludes)
    info = {"version": str(pj["version"]), "package": pj["name"], "asset_root": asset_root}

    # rewrites
    for rel, pattern, template in REWRITES:
        if rel not in out:
            if rel.split("/", 1)[0] in excludes:
                continue
            raise Failure(f"{rel} is not in the source but make_classic_kit.py rewrites it: update REWRITES")
        text = out[rel].decode("utf-8")
        new_text, n = pattern.subn(template.replace("{asset_root}", asset_root), text)
        if n != 1:
            raise Failure(f"{rel}: expected exactly one match of {pattern.pattern!r}, found {n}: the source drifted from make_classic_kit.py")
        out[rel] = new_text.encode("utf-8")

    # README banner (after the first heading)
    if "README.md" in out:
        text = out["README.md"].decode("utf-8")
        nl = text.find("\n")
        banner = BANNER.format(version=pj["version"])
        out["README.md"] = (text[:nl + 1] + "\n" + banner + text[nl + 1:]).encode("utf-8")

    # VERSION + its meta
    out["VERSION"] = version_text(pj, f"classic ({asset_root}), generated from the UPM source by {TOOL}").encode("utf-8")
    meta_tpl = src / GENERATED_META_DIR / "VERSION.meta.tpl"
    if not meta_tpl.is_file():
        raise Failure(f"missing {GENERATED_META_DIR}/VERSION.meta.tpl in the source (generate it with Unity once and commit it)")
    out["VERSION.meta"] = meta_tpl.read_bytes()
    return out, info


def manifest_bytes(files: dict[str, bytes], info: dict) -> bytes:
    source_hash = sha256("\n".join(f"{k} {sha256(v)}" for k, v in sorted(files.items())).encode("utf-8"))
    doc = {
        "tool": TOOL,
        "manifestVersion": MANIFEST_VERSION,
        "kitVersion": info["version"],
        "package": info["package"],
        "assetRoot": info["asset_root"],
        "contentHash": source_hash,
        "files": {k: sha256(v) for k, v in sorted(files.items())},
    }
    return (json.dumps(doc, indent=2, sort_keys=True) + "\n").encode("utf-8")


def scan_dst(dst: Path) -> dict[str, str]:
    """Relative path -> sha256 of every file now in dst (the manifest excluded)."""
    found: dict[str, str] = {}
    if not dst.exists():
        return found
    for dirpath, dirnames, filenames in os.walk(dst):
        dirnames.sort()
        d = Path(dirpath)
        for name in sorted(filenames):
            if name == ".DS_Store":
                continue
            p = d / name
            rel = p.relative_to(dst).as_posix()
            if rel == MANIFEST:
                continue
            found[rel] = sha256(p.read_bytes())
    return found


def local_changes(dst: Path, current: dict[str, str]) -> list[str]:
    mpath = dst / MANIFEST
    if not mpath.is_file():
        return [f"{len(current)} file(s) and no {MANIFEST}: not a classic kit copy made by this script"] if current else []
    try:
        recorded = json.loads(mpath.read_text(encoding="utf-8"))["files"]
    except Exception as exc:
        return [f"{MANIFEST} is unreadable ({exc})"]
    problems = []
    for rel, h in sorted(recorded.items()):
        if rel not in current:
            problems.append(f"deleted:  {rel}")
        elif current[rel] != h:
            problems.append(f"modified: {rel}")
    for rel in sorted(current):
        if rel not in recorded:
            problems.append(f"added:    {rel}")
    return problems


def write_file(path: Path, data: bytes, mode_from: Path | None = None) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_bytes(data)
    if mode_from is not None and mode_from.exists():
        shutil.copymode(mode_from, path)


def prune_empty_dirs(dst: Path) -> None:
    for dirpath, dirnames, filenames in os.walk(dst, topdown=False):
        d = Path(dirpath)
        if d == dst:
            continue
        if not any(d.iterdir()):
            d.rmdir()


def folder_metas(src: Path, dst: Path, dry: bool, log) -> list[str]:
    """<dst>.meta and the parent's .meta when the layout is .../FutureDigitalPlus/UIKit and they are missing. Never overwritten."""
    written = []
    pairs = [(dst.parent / (dst.name + ".meta"), src / GENERATED_META_DIR / "UIKit.folder.meta.tpl")]
    if dst.parent.name == "FutureDigitalPlus":
        pairs.append((dst.parent.parent / "FutureDigitalPlus.meta", src / GENERATED_META_DIR / "FutureDigitalPlus.folder.meta.tpl"))
    for target, template in pairs:
        if target.exists() or not template.is_file():
            continue
        log(f"folder meta: {target}")
        if not dry:
            write_file(target, template.read_bytes())
        written.append(str(target))
    return written


def run(args) -> int:
    log = (lambda m: None) if args.quiet else print
    src = Path(args.src).resolve()
    dst = Path(args.dst).resolve()
    if not src.is_dir():
        raise Failure(f"--src is not a folder: {src}")
    if dst == src or src in dst.parents or dst in src.parents:
        raise Failure("--src and --dst must not contain each other")
    excludes = set(args.exclude or [])
    for e in excludes:
        if "/" in e or e in ("Runtime", "Editor"):
            raise Failure(f"--exclude takes one top level folder name other than Runtime and Editor, not {e!r}")
    asset_root = derive_asset_root(dst, args.asset_root)

    out, info = build_output(src, dst, asset_root, excludes)
    manifest = manifest_bytes(out, info)
    current = scan_dst(dst)
    problems = local_changes(dst, current)

    # what would change
    to_write = sorted(k for k, v in out.items() if k not in current or current[k] != sha256(v))
    to_delete = sorted(k for k in current if k not in out)
    manifest_current = (dst / MANIFEST).read_bytes() if (dst / MANIFEST).is_file() else None
    manifest_changes = manifest_current != manifest
    in_sync = not to_write and not to_delete and not manifest_changes

    if args.check:
        if in_sync:
            log(f"classic kit {info['version']} at {dst} is up to date")
            return 0
        print(f"classic kit at {dst} differs from {src}: {len(to_write)} to write, {len(to_delete)} to delete"
              + (", manifest differs" if manifest_changes else ""))
        for k in to_write[:20]:
            print(f"  write:  {k}")
        for k in to_delete[:20]:
            print(f"  delete: {k}")
        return 3

    if problems and not args.force:
        print(f"refusing to overwrite {dst}: it has local changes (use --force to discard them)", file=sys.stderr)
        for p in problems[:40]:
            print(f"  {p}", file=sys.stderr)
        if len(problems) > 40:
            print(f"  ... and {len(problems) - 40} more", file=sys.stderr)
        raise Refused()

    if in_sync:
        folder_metas(src, dst, args.dry_run, log)
        log(f"classic kit {info['version']} at {dst} is already up to date ({len(out)} files)")
        return 0

    for k in to_delete:
        log(f"delete: {k}")
        if not args.dry_run:
            (dst / k).unlink()
    for k in to_write:
        log(f"write:  {k}")
        if not args.dry_run:
            srcfile = src / k
            write_file(dst / k, out[k], srcfile)
    if not args.dry_run:
        if manifest_changes:
            write_file(dst / MANIFEST, manifest)
        prune_empty_dirs(dst)
    folder_metas(src, dst, args.dry_run, log)
    verb = "would write" if args.dry_run else "wrote"
    print(f"classic kit {info['version']} -> {dst}: {verb} {len(to_write)} file(s), deleted {len(to_delete)}, {len(out) - len(to_write)} unchanged"
          f" (asset root {asset_root})")
    return 0


def main(argv=None) -> int:
    ap = argparse.ArgumentParser(description=__doc__.split("\n\n")[0], formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--src", required=True)
    ap.add_argument("--dst", required=True)
    ap.add_argument("--asset-root")
    ap.add_argument("--exclude", action="append")
    ap.add_argument("--force", action="store_true")
    ap.add_argument("--check", action="store_true")
    ap.add_argument("--dry-run", action="store_true")
    ap.add_argument("--quiet", action="store_true")
    args = ap.parse_args(argv)
    try:
        return run(args)
    except Refused:
        return 2
    except Failure as exc:
        print(f"error: {exc}", file=sys.stderr)
        return 1


if __name__ == "__main__":
    sys.exit(main())
