#!/usr/bin/env python3
"""matrix.py: helper of run-matrix.sh. Python 3.9+, standard library only, no network.

  matrix.py prepare   --project <variant project> --package <package root> --variant <name> [--variants variants.json] [--classic]
        Rewrites the cloned project for a variant: manifest (package path, testables, variant packages), colour space and
        Active Input Handling in ProjectSettings/ProjectSettings.asset, and copies Editor/ into Assets/FdpMatrix/Editor.
        With --classic the kit is not referenced from the manifest (no package entry, no testables): run-matrix.sh --classic has put the
        classic copy under Assets/FutureDigitalPlus/UIKit with Tools~/make_classic_kit.py, and the manifest keeps only the variant's packages.
  matrix.py summarize --dir <variant work dir> --variant <name> [--variants variants.json]
        Reads results-edit.xml, results-play.xml and the logs in <dir>, writes <dir>/summary.json and prints one row.
  matrix.py table     --work <work dir> [--format markdown|tsv]
        Prints one table with one row per variant summarized under <work>.
  matrix.py pipeline-method --variant <name> [--variants variants.json]
        Prints the -executeMethod that creates the variant's render pipeline asset, or nothing for the Built-in pipeline.
"""
import argparse, json, os, re, shutil, sys
import xml.etree.ElementTree as ET
from pathlib import Path

HERE = Path(__file__).resolve().parent
PACKAGE_ID = "com.futuredigitalplus.uikit"
INPUT_HANDLER = {"legacy": 0, "new": 1, "both": 2}
COLOR_SPACE = {"gamma": 0, "linear": 1}
PIPELINE_METHOD = {"urp3d": "FdpMatrix.UrpSetup.Use3D", "urp2d": "FdpMatrix.UrpSetup.Use2D", "hdrp": "FdpMatrix.HdrpSetup.Use"}


def load_variant(variants_file, name):
    data = json.loads(Path(variants_file).read_text())
    if name not in data["variants"]:
        sys.exit(f"unknown variant '{name}'; known: {', '.join(sorted(data['variants']))}")
    return data["variants"][name], data


# ---------------------------------------------------------------------------------------------- prepare

def edit_project_setting(text, key, value):
    """Sets `  key: value` (one line, 2 spaces indent) in ProjectSettings.asset text; raises when the key is missing."""
    new, n = re.subn(rf"^(  {re.escape(key)}:) .*$", rf"\g<1> {value}", text, flags=re.M)
    if n != 1:
        raise ValueError(f"ProjectSettings.asset has {n} lines for '{key}' (expected 1)")
    return new


def prepare(project, package, variant_name, variants_file, classic=False):
    variant, _ = load_variant(variants_file, variant_name)
    project = Path(project)

    manifest_path = project / "Packages/manifest.json"
    manifest = json.loads(manifest_path.read_text())
    deps = manifest.setdefault("dependencies", {})
    if classic:
        deps.pop(PACKAGE_ID, None)          # the classic copy is a folder below Assets, not a package
        if PACKAGE_ID in manifest.get("testables", []):
            manifest["testables"].remove(PACKAGE_ID)
    else:
        deps[PACKAGE_ID] = "file:" + str(Path(package).resolve())
    deps.update(variant.get("packages", {}))
    if not classic:
        testables = manifest.setdefault("testables", [])
        if PACKAGE_ID not in testables:
            testables.append(PACKAGE_ID)
    lock = project / "Packages/packages-lock.json"
    if lock.exists():
        lock.unlink()
    manifest_path.write_text(json.dumps(manifest, indent=2) + "\n")

    settings_path = project / "ProjectSettings/ProjectSettings.asset"
    text = settings_path.read_text()
    text = edit_project_setting(text, "m_ActiveColorSpace", COLOR_SPACE[variant["colorSpace"]])
    text = edit_project_setting(text, "activeInputHandler", INPUT_HANDLER[variant["input"]])
    settings_path.write_text(text)

    target = project / "Assets/FdpMatrix/Editor"
    if target.exists():
        shutil.rmtree(target)
    shutil.copytree(HERE / "Editor", target)

    (project / "FdpMatrixVariant.json").write_text(json.dumps({"name": variant_name, **variant}, indent=2) + "\n")
    print(f"prepared {project} for '{variant_name}'{' (classic layout)' if classic else ''}: {variant['description']}")


# -------------------------------------------------------------------------------------------- summarize

def read_results(path):
    """Counts of an NUnit 3 result file: {passed, failed, skipped, total, failures:[names], cases:[(name, result)]} or None when absent."""
    path = Path(path)
    if not path.exists():
        return None
    try:
        root = ET.parse(path).getroot()
    except ET.ParseError:
        return {"passed": 0, "failed": 1, "skipped": 0, "total": 1, "failures": ["(result file is not valid XML)"], "cases": []}
    cases = [(tc.get("fullname") or tc.get("name"), tc.get("result")) for tc in root.iter("test-case")]
    passed = sum(1 for _, r in cases if r == "Passed")
    failed = sum(1 for _, r in cases if r in ("Failed", "Error"))
    skipped = len(cases) - passed - failed
    return {"passed": passed, "failed": failed, "skipped": skipped, "total": len(cases),
            "failures": [n for n, r in cases if r in ("Failed", "Error")][:20], "cases": cases}


COMPILE_MSG = re.compile(r"\b(error|warning) (CS\d+|UIC\d+)\b")


def compile_messages(*logs):
    """Distinct compiler lines (`error CS...`, `warning CS...`, UIComponents `UIC...`) in the given log files; returns (errors, warnings)."""
    errors, warnings = set(), set()
    for log in logs:
        p = Path(log)
        if not p.exists():
            continue
        for line in p.read_text(errors="replace").splitlines():
            m = COMPILE_MSG.search(line)
            if not m:
                continue
            (errors if m.group(1) == "error" else warnings).add(line.strip())
    return len(errors), len(warnings)


def grep_last(path, pattern):
    p = Path(path)
    if not p.exists():
        return None
    found = None
    rx = re.compile(pattern)
    for line in p.read_text(errors="replace").splitlines():
        m = rx.search(line)
        if m:
            found = m
    return found


def summarize(directory, variant_name, variants_file, label=None):
    variant, _ = load_variant(variants_file, variant_name)
    d = Path(directory)
    edit = read_results(d / "results-edit.xml")
    play = read_results(d / "results-play.xml")

    env = grep_last(d / "play.log", r"FDPMATRIX env pipeline=(\S+) renderer=(\S+) device=(\S+) colorSpace=(\S+)")
    paint = [r for n, r in (play["cases"] if play else []) if "ThemedPanel_PaintsTheThemeColours" in n]
    errors, warnings = compile_messages(d / "edit.log", d / "play.log")

    pcc = {}
    pcc_log = d / "pcc.log"
    if pcc_log.exists():
        for line in pcc_log.read_text(errors="replace").splitlines():
            m = re.search(r"PLAYERCOMPILE (\w+): assemblies=(\d+)", line)
            if m and int(m.group(2)) > 0:
                pcc[m.group(1)] = "ok"
            m = re.search(r"PLAYERCOMPILE (\w+) skipped", line)
            if m:
                pcc[m.group(1)] = "no module"
            m = re.search(r"PLAYERCOMPILE (\w+) threw", line)
            if m:
                pcc[m.group(1)] = "FAILED"

    exports = {}
    for log in sorted(d.glob("export-*.log")):
        m = grep_last(log, r"BUILDRESULT (\w+) result=(\w+) errors=(\d+) warnings=(\d+)")
        exports[log.stem.replace("export-", "")] = f"{m.group(2)} ({m.group(3)} err, {m.group(4)} warn)" if m else "no result"

    summary = {
        "variant": label or variant_name,
        "description": variant["description"],
        "pipeline": f"{env.group(1)} / {env.group(2)}" if env else "?",
        "device": env.group(3) if env else "?",
        "colorSpace": env.group(4) if env else variant["colorSpace"],
        "input": variant["input"],
        "edit": {k: edit[k] for k in ("passed", "failed", "skipped", "total")} if edit else None,
        "play": {k: play[k] for k in ("passed", "failed", "skipped", "total")} if play else None,
        "failures": (edit["failures"] if edit else []) + (play["failures"] if play else []),
        "paint": f"{paint.count('Passed')}/{len(paint)}" if paint else "-",
        "compileErrors": errors,
        "compileWarnings": warnings,
        "playerCompile": pcc,
        "exports": exports,
    }
    (d / "summary.json").write_text(json.dumps(summary, indent=2) + "\n")
    print(format_row(summary))
    return summary


# ---------------------------------------------------------------------------------------------- table

COLUMNS = ["variant", "pipeline / renderer", "colour", "input", "EditMode", "PlayMode", "paint", "compile err/warn", "player compile", "IL2CPP export"]


def fmt_counts(c):
    if not c:
        return "not run"
    s = f"{c['passed']}/{c['total']}"
    if c["failed"]:
        s += f" ({c['failed']} FAILED)"
    if c["skipped"]:
        s += f" ({c['skipped']} skipped)"
    return s


def row_cells(s):
    pcc = ", ".join(f"{k}:{v}" for k, v in sorted(s["playerCompile"].items())) or "-"
    exp = ", ".join(f"{k}:{v}" for k, v in sorted(s["exports"].items())) or "-"
    return [s["variant"], s["pipeline"], s["colorSpace"], s["input"], fmt_counts(s["edit"]), fmt_counts(s["play"]), s["paint"],
            f"{s['compileErrors']}/{s['compileWarnings']}", pcc, exp]


def format_row(s):
    return " | ".join(f"{c}={v}" for c, v in zip(COLUMNS, row_cells(s)))


def table(work, fmt):
    rows = []
    for p in sorted(Path(work).glob("*/summary.json")):
        rows.append(row_cells(json.loads(p.read_text())))
    if fmt == "tsv":
        return "\n".join("\t".join(r) for r in [COLUMNS] + rows)
    lines = ["| " + " | ".join(COLUMNS) + " |", "|" + "|".join("---" for _ in COLUMNS) + "|"]
    lines += ["| " + " | ".join(r) + " |" for r in rows]
    return "\n".join(lines)


def main(argv=None):
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    sub = ap.add_subparsers(dest="cmd", required=True)
    for name in ("prepare", "summarize", "pipeline-method"):
        p = sub.add_parser(name)
        p.add_argument("--variant", required=True)
        p.add_argument("--variants", default=str(HERE / "variants.json"))
        if name == "prepare":
            p.add_argument("--project", required=True)
            p.add_argument("--package", required=True)
            p.add_argument("--classic", action="store_true")
        if name == "summarize":
            p.add_argument("--dir", required=True)
            p.add_argument("--label", default=None, help="name shown in the table instead of the variant's (the samples run uses <variant>-samples)")
    t = sub.add_parser("table")
    t.add_argument("--work", required=True)
    t.add_argument("--format", choices=["markdown", "tsv"], default="markdown")
    a = ap.parse_args(argv)

    if a.cmd == "prepare":
        prepare(a.project, a.package, a.variant, a.variants, a.classic)
    elif a.cmd == "summarize":
        summarize(a.dir, a.variant, a.variants, a.label)
    elif a.cmd == "pipeline-method":
        v, _ = load_variant(a.variants, a.variant)
        print(PIPELINE_METHOD.get(v["pipeline"], ""))
    else:
        print(table(a.work, a.format))


if __name__ == "__main__":
    main()
