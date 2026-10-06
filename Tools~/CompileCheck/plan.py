#!/usr/bin/env python3
"""plan.py: builds a compile plan (assemblies in dependency order) from the asmdef files of the package, so Program.cs can compile
every assembly with Roslyn against Unity's own reference assemblies WITHOUT starting Unity.

usage: plan.py <package root> --out plan.json [--unity <Unity.app>] [--test-project <dir>] [--tests] [--samples]
               [--installed <comma separated package/module names>] [--define SYMBOL ...]

It approximates Unity's compile: no player settings, no csc.rsp, defines only from the asmdef (UNITY_EDITOR for Editor-only assemblies,
UNITY_INCLUDE_TESTS for test assemblies, UNITY_x_OR_NEWER up to 6000.3) and from `versionDefines` whose package is in --installed.
Assemblies whose defineConstraints are not met (for example the optional-package adapters) are skipped and listed.
macOS editor layout only (Unity.app/Contents/Resources/Scripting). Python 3.9+, standard library only.
"""
import argparse, glob, json, os, re, sys
from pathlib import Path

ap = argparse.ArgumentParser()
ap.add_argument("root")
ap.add_argument("--out", required=True)
ap.add_argument("--unity", default=os.environ.get("UNITY_EDITOR_APP", ""))
ap.add_argument("--test-project", default=os.environ.get("FDP_TEST_PROJECT", ""))
ap.add_argument("--tests", action="store_true")
ap.add_argument("--samples", action="store_true")
ap.add_argument("--installed", default="com.unity.modules.unitywebrequest,com.unity.modules.vectorgraphics,com.unity.modules.uielements")
ap.add_argument("--define", action="append", default=[])
args = ap.parse_args()

root = Path(args.root).resolve()

unity_app = args.unity
if not unity_app:
    found = sorted(glob.glob("/Applications/Unity/Hub/Editor/6000.3*/Unity.app"))
    if not found:
        sys.exit("No Unity 6000.3 editor found: pass --unity <Unity.app> or set UNITY_EDITOR_APP")
    unity_app = found[-1]
U = unity_app + "/Contents/Resources/Scripting"
if not os.path.isdir(U):
    sys.exit(f"{U} does not exist (this tool knows the macOS editor layout only)")

base = [U + "/NetStandard/ref/2.1.0/netstandard.dll"] \
     + sorted(glob.glob(U + "/NetStandard/compat/2.1.0/shims/netstandard/*.dll")) \
     + sorted(glob.glob(U + "/Managed/UnityEngine/*.dll"))
editor_refs = sorted(glob.glob(U + "/Managed/UnityEditor*.dll")) + sorted(glob.glob(U + "/Managed/UnityEngine/UnityEditor*.dll"))

ext = {}
nunit = []
if args.test_project:
    tp = Path(args.test_project)
    ext = {
        "UnityEngine.TestRunner": str(tp / "Library/ScriptAssemblies/UnityEngine.TestRunner.dll"),
        "UnityEditor.TestRunner": str(tp / "Library/ScriptAssemblies/UnityEditor.TestRunner.dll"),
    }
    nunit = glob.glob(str(tp / "Library/PackageCache/com.unity.ext.nunit@*/net40/unity-custom/nunit.framework.dll"))
precompiled = {"nunit.framework.dll": nunit[0] if nunit else ""}
for f in root.rglob("*.dll"):
    precompiled[f.name] = str(f)

VERSION_DEFINES = ["UNITY_2021_3_OR_NEWER", "UNITY_2022_1_OR_NEWER", "UNITY_2022_2_OR_NEWER", "UNITY_2022_3_OR_NEWER", "UNITY_2023_1_OR_NEWER",
                   "UNITY_2023_2_OR_NEWER", "UNITY_6000_0_OR_NEWER", "UNITY_6000_1_OR_NEWER", "UNITY_6000_2_OR_NEWER", "UNITY_6000_3_OR_NEWER",
                   "UNITY_2020_3_OR_NEWER", "UNITY_2019_4_OR_NEWER", "UNITY_2018_4_OR_NEWER"]
installed = set(x for x in args.installed.split(",") if x)


def kind_of(rel):
    if rel.startswith("Samples~/"): return "sample"
    if rel.startswith("Tests/"): return "test"
    if rel.startswith("Editor/"): return "editor"
    return "runtime"


asmdefs, guid_to_name = {}, {}
for p in root.rglob("*.asmdef"):
    rel = p.relative_to(root).as_posix()
    if kind_of(rel) == "sample" and not args.samples: continue
    if kind_of(rel) == "test" and not args.tests: continue
    if "Tools~/" in rel or "Documentation~/" in rel: continue
    j = json.loads(p.read_bytes().decode("utf-8-sig"))
    j["_dir"], j["_rel"] = p.parent, rel
    asmdefs[j["name"]] = j
    meta = Path(str(p) + ".meta")
    if meta.exists():
        m = re.search(r"^guid:\s*([0-9a-f]{32})", meta.read_text(), re.M)
        if m: guid_to_name[m.group(1)] = j["name"]


def ref_name(r):
    return guid_to_name.get(r[5:], r) if r.startswith("GUID:") else r


asmdir_all = {Path(p).parent for p in glob.glob(str(root / "**/*.asmdef"), recursive=True)}


def sources(d):
    files = []
    for f in d.rglob("*.cs"):
        anc = f.parent
        while anc not in asmdir_all and anc != root: anc = anc.parent
        if anc == d: files.append(str(f))
    return sorted(files)


def defines(a):
    d = list(VERSION_DEFINES) + args.define
    if "Editor" in a.get("includePlatforms", []): d.append("UNITY_EDITOR")
    for vd in a.get("versionDefines", []):
        if vd["name"] in installed: d.append(vd["define"])
    if "UNITY_INCLUDE_TESTS" in a.get("defineConstraints", []): d.append("UNITY_INCLUDE_TESTS")
    return d


def constraints_met(a):
    d = defines(a)
    for c in a.get("defineConstraints", []):
        neg = c.startswith("!")
        c = c.lstrip("!")
        if (c in d) == neg: return False
    return True


order, seen = [], set()


def visit(n):
    if n in seen or n not in asmdefs: return
    seen.add(n)
    for r in asmdefs[n].get("references", []): visit(ref_name(r))
    order.append(n)


for n in asmdefs: visit(n)

COMPONENTS = "FutureDigitalPlus.UIKit.Components"
memo = {}


def refs_components(n):
    if n in memo: return memo[n]
    a = asmdefs.get(n)
    memo[n] = bool(a) and (n == COMPONENTS or any(refs_components(ref_name(r)) for r in a.get("references", [])))
    return memo[n]


generator_dir = ""
for cand in glob.glob(str(root / "Runtime/Components/**/*Roslyn.Generation.dll"), recursive=True):
    generator_dir = str(Path(cand).parent)

result = {"packageRoot": str(root), "baseRefs": base, "editorRefs": editor_refs, "assemblies": []}
skipped = []
for n in order:
    a = asmdefs[n]
    if not constraints_met(a):
        skipped.append(n)
        continue
    refs = []
    for r in a.get("references", []):
        r = ref_name(r)
        if r in asmdefs and r not in skipped: refs.append(r)
        elif r in ext: refs.append(ext[r])
        elif r in asmdefs: pass
        else: print("unresolved external reference", r, "in", n, file=sys.stderr)
    for pr in a.get("precompiledReferences", []):
        if precompiled.get(pr): refs.append(precompiled[pr])
    if a.get("precompiledReferences"):
        refs += sorted(glob.glob(U + "/NetStandard/compat/2.1.0/shims/netfx/*.dll"))
    rel = a["_rel"]
    is_editor = "Editor" in a.get("includePlatforms", []) or kind_of(rel) == "editor" or "UnityEditor.TestRunner" in a.get("references", [])
    result["assemblies"].append({
        "name": n, "kind": kind_of(rel), "files": sources(a["_dir"]), "refs": refs, "defines": defines(a), "editor": is_editor,
        "unsafe": a.get("allowUnsafeCode", False),
        "generatorDir": generator_dir if generator_dir and refs_components(n) else "",
    })

json.dump(result, open(args.out, "w"), indent=1)
print(f"assemblies: {len(result['assemblies'])}; skipped because defineConstraints are not met: {skipped}")
