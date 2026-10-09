#!/usr/bin/env python3
"""
rename.py -- mechanical collision-proofing rename for com.futuredigitalplus.uikit

Applied once to the live package (see Documentation~/Upgrading.md, "Re-syncing after the rename"). It stays in the
package so that an upstream tree can be pushed through the same function before a three-way merge. Run it on a COPY
first; --apply refuses the package that holds this script unless --allow-live (dry run and --verify-only are always allowed).

What it does (stages, in this order; all are idempotent, a second run changes nothing):

  moves       move Resources content to Resources/FutureDigitalPlus/UIKit/... (files + their .meta, so GUIDs stay)
  asmdef      rename assemblies (asmdef name, references, rootNamespace, file name + .meta name)
  text        rewrite namespaces, resource paths, USS variables/classes, menus, defines in cs/uss/uxml/tss/md/...
  records     append a generated "Rename record" block to each module SOURCE.md (between markers)
  verify      self-checks (asmdef refs resolve, no stale names, Resources literals resolve, metas needed)

Usage (always on a copy first):
  rename.py --root <copy of package>                       # dry run: prints the plan, writes nothing
  rename.py --root <copy of package> --apply               # apply
  rename.py --root <copy> --apply --components assemblies  # rename Components assemblies only, keep namespace UIComponents (stock Roslyn DLLs work)
  rename.py --root <copy> --apply --components keep        # keep every Components name (documented risk)
  rename.py --root <copy> --apply --auto-ref facade        # asmdef autoReferenced=false except the facade
  rename.py --root <copy> --verify-only                    # only run the checks
  rename.py --dump-map                                     # print the mapping tables as markdown
  rename.py --generator-src <uicomponents>/UIComponents.Roslyn --generator-out <dir> [--roslyn 3.8.0]
                                                           # patch + rebuild the Roslyn DLLs (needs dotnet + NuGet)
  rename.py --root <copy> --apply --install-generator <dir>/dll   # copy rebuilt DLLs into the package (derived metas)

Exit code: 0 ok, 1 verify problems, 2 usage error.
Python 3.9+, standard library only.
"""
from __future__ import annotations

import argparse
import fnmatch
import glob
import json
import os
import re
import shutil
import subprocess
import sys
import uuid
from collections import Counter, defaultdict
from pathlib import Path

def _own_package() -> Path:
    """The package that holds this script (Tools~/rename/rename.py), if it does; else a path that matches nothing."""
    p = Path(__file__).resolve()
    return p.parents[2] if len(p.parents) > 2 and p.parent.name == "rename" and p.parent.parent.name == "Tools~" else Path("/nonexistent")


LIVE_PACKAGE = _own_package()
RENAME_MARK = "// FutureDigitalPlus: renamed by Tools~/rename/rename.py (namespaces, resource paths); see SOURCE.md \"Rename record\"."
REC_BEGIN, REC_END = "<!-- rename-record:begin -->", "<!-- rename-record:end -->"

FDP = "FutureDigitalPlus.UIKit"
RES = "FutureDigitalPlus/UIKit"          # Resources root of everything the package ships

# --------------------------------------------------------------------------------------------------
# 1. Assemblies. Exact names first; everything else matching ^UIComponents(\.|$) is derived.
#    value = (new assembly name, new rootNamespace or None = keep/derive)
# --------------------------------------------------------------------------------------------------
ASM_EXACT = {
    "DesignSystem.Runtime":                   (f"{FDP}.DesignSystem",                 f"{FDP}.DesignSystem"),
    "DesignSystem.Editor":                    (f"{FDP}.DesignSystem.Editor",          f"{FDP}.DesignSystem.Editor"),
    "id.instance.elementanimationtoolkit":    (f"{FDP}.Animation",                    f"{FDP}.Animation"),
    # already ours, only re-rooted
    f"{FDP}.Extras":                          (f"{FDP}.Extras",                       f"{FDP}.Extras"),
    # tests: one scheme  <Module>.Tests[.Editor]
    f"{FDP}.Tests.DesignSystem.Runtime":      (f"{FDP}.DesignSystem.Tests",           None),
    f"{FDP}.Tests.DesignSystem.Editor":       (f"{FDP}.DesignSystem.Tests.Editor",    None),
    f"{FDP}.Tests.Animation.Runtime":         (f"{FDP}.Animation.Tests",              None),
    f"{FDP}.Tests.Components":                (f"{FDP}.Components.Tests.Smoke",       None),
}
COMPONENTS_ASM_PREFIX = ("UIComponents", f"{FDP}.Components")   # derived: UIComponents[.X] -> FDP.Components[.X]

# autoReferenced policy (--auto-ref facade): only these stay true
AUTOREF_KEEP = {f"{FDP}"}

# --------------------------------------------------------------------------------------------------
# 2. Namespaces / qualified names in code and docs.  (pattern, replacement); order matters.
#    (?<![\w.]) = not part of a longer qualified name, which also makes every rule idempotent.
# --------------------------------------------------------------------------------------------------
NS_RULES = [
    (r"(?<![\w.])id\.instance\.elementanimationtoolkit\b", f"{FDP}.Animation"),     # assembly name in strings/asmdef
    (r"(?<![\w.])instance\.id\.EATK\b",                    f"{FDP}.Animation"),
    (r"(?<![\w.])DesignSystem\.Runtime\b",                 f"{FDP}.DesignSystem"),  # also the assembly name
    (r"(?<![\w.])DesignSystem\.Editor\b",                  f"{FDP}.DesignSystem.Editor"),
    (r"(?<![\w.])Game\.UI\b",                              f"{FDP}.Extras"),
]
# Components (skipped with --components keep). Not applied to UIComponents.Roslyn (the generator's own names).
NS_COMPONENTS_CODE = (r"(?<![\w.])UIComponents\b(?!\.Roslyn)", f"{FDP}.Components")
# in markdown only qualified / using / namespace forms, so prose "UIComponents" (the product) is left alone
NS_COMPONENTS_MD = (r"(?<![\w.])UIComponents(?=\.[A-Z]|;|\s*\{|\)|`)(?!\.Roslyn)", f"{FDP}.Components")

# --------------------------------------------------------------------------------------------------
# 3. Resources. (resources-root glob relative to package, old sub path, new sub path)
#    old_sub "" = every child of that Resources root (samples).
# --------------------------------------------------------------------------------------------------
S = f"{RES}/Samples/Components"
MOVES = [
    ("Runtime/DesignSystem/Resources", "UI/Styles/DesignSystem", f"{RES}/DesignSystem/Styles"),
    ("Runtime/DesignSystem/Resources", "UI/Themes",              f"{RES}/DesignSystem/Themes"),
    ("Runtime/DesignSystem/Resources", "Textures/Icons",         f"{RES}/DesignSystem/Icons"),
    ("Runtime/DesignSystem/Resources", "Fx/Shaders",             f"{RES}/DesignSystem/Shaders"),
    ("Runtime/Animation/Elements/*/Resources", "Style",          f"{RES}/Animation/Styles"),
    ("Runtime/Extras/*/Resources", "GameUI/Styles",              f"{RES}/Extras/Styles"),
    ("Tests/Runtime/Components/Resources",                      "UIComponentTests",           f"{RES}/Tests/Components"),
    ("Tests/Runtime/Components/Roslyn/Resources",               "UIComponentTests",           f"{RES}/Tests/Components"),
    ("Tests/Runtime/Components/FutureDigitalPlusSmoke/Resources", "FutureDigitalPlusUIKitTests", f"{RES}/Tests"),
    ("Tests/Runtime/UIKit/Resources",                           "FutureDigitalPlusUIKitTests", f"{RES}/Tests"),
    ("Samples~/UIKitQuickStart/Resources",        "FutureDigitalPlusSamples", f"{RES}/Samples"),
    ("Samples~/DesignSystemShowcase/Resources",   "FutureDigitalPlusSamples", f"{RES}/Samples"),
    ("Samples~/Components/EventInterfaces/Resources", "", f"{S}/EventInterfaces"),
    ("Samples~/Components/Query/Resources",           "", f"{S}/Query"),
    ("Samples~/Components/UxmlTraits/Resources",      "", f"{S}/UxmlTraits"),
    ("Samples~/Components/UxmlTraits/Editor/Resources", "", f"{S}/UxmlTraits"),
    ("Samples~/Components/Resources/Resources",       "", f"{S}/Resources"),
]
# sample folder -> Resources prefix its [Layout]/[Stylesheet]/Resources.Load literals get (Addressables sample: none)
SAMPLE_LITERAL_PREFIX = {
    "Samples~/Components/EventInterfaces": f"{S}/EventInterfaces/",
    "Samples~/Components/Query":           f"{S}/Query/",
    "Samples~/Components/UxmlTraits":      f"{S}/UxmlTraits/",
    "Samples~/Components/Resources":       f"{S}/Resources/",
}
_END = r"""(?=[/"'`)\s,.;:\]]|$)"""           # what may follow a resource path prefix
_NB = r"(?<![\w./~-])"                       # not inside a longer path
RES_LITERAL_RULES = [
    (_NB + r"UI/Styles/DesignSystem" + _END, f"{RES}/DesignSystem/Styles"),
    (_NB + r"UI/Themes" + _END,              f"{RES}/DesignSystem/Themes"),
    (_NB + r"Textures/Icons" + _END,         f"{RES}/DesignSystem/Icons"),
    (_NB + r"Fx/Shaders" + _END,             f"{RES}/DesignSystem/Shaders"),
    (_NB + r"GameUI/Styles" + _END,          f"{RES}/Extras/Styles"),
    # "/UI/Styles/DesignSystem/x.uss": a literal that is matched as a path suffix (tests)
    (r'(?<=")/UI/Styles/DesignSystem' + _END, f"/{RES}/DesignSystem/Styles"),
    # Animation: exact names only ("Style/" alone is too generic)
    (_NB + r"Style/(AnimatedFoldoutStyle|MaskedInputField|AnimatedLabel)" + _END, rf"{RES}/Animation/Styles/\1"),
    # single words that are also identifiers (class UIComponentTests ...): only when followed by '/'
    (_NB + r"UIComponentTests(?=/)",             f"{RES}/Tests/Components"),
    (_NB + r"FutureDigitalPlusUIKitTests(?=/)",  f"{RES}/Tests"),
    (_NB + r"FutureDigitalPlusSamples(?=/)",     f"{RES}/Samples"),
]
# the same paths when spelled with their Resources folder: "Resources/Textures/Icons", "Assets/Resources/UI/Themes"
RES_LITERAL_RULES += [(r"(?<=Resources/)" + pat[len(_NB):], rep) for pat, rep in list(RES_LITERAL_RULES)
                      if pat.startswith(_NB)]
# package-relative forms (…/Resources/UI/Themes inside the package), derived from MOVES at run time (see build_pkg_rules)

# --------------------------------------------------------------------------------------------------
# 4. USS custom properties and classes
# --------------------------------------------------------------------------------------------------
VAR_PREFIX = "fdp-"                          # --color-bg -> --fdp-color-bg ; --unity-* untouched
EXTRAS_CLASS_BASES = ["tooltip", "slider-2d", "color-field", "color-popup", "popup-panel", "popup-message", "popup-textfield"]
CLASS_PREFIX = "fdp-"                        # .tooltip -> .fdp-tooltip
# other classes: (regex, replacement)
CLASS_RULES = [
    (r"(?<![\w-])back-btn(?=__|[^\w-]|$)", "ds-back-btn"),     # the only bare DS component class (3 USS families)
    (r"(?<![\w-])is-spinning(?![\w-])",    "ds-is-spinning"),  # behaviour class matched by code in every panel
]

# the bare class "mobile" (the touch-size tier) is a host-collision risk: it becomes fdp-input-touch
TOUCH_CLASS = "fdp-input-touch"
MOBILE_CSS_RULE = (r"(?<![\w-])\.mobile(?![\w-])", f".{TOUCH_CLASS}")                  # uss, tss, md
MOBILE_CS_RULES = [                                                                    # cs, md (code samples)
    (r'((?:Add|Remove|Toggle|Enable)(?:To|From|In)?ClassList\(|ClassListContains\(|\bMobileClass\s*=\s*)"mobile"', rf'\1"{TOUCH_CLASS}"'),
    (r"<c>mobile</c>", f"<c>{TOUCH_CLASS}</c>"),
]
MOBILE_MD_RULES = [
    (r"`mobile`", f"`{TOUCH_CLASS}`"),
    (r'(class )"mobile"', rf'\1"{TOUCH_CLASS}"'),
]

# --------------------------------------------------------------------------------------------------
# 5. Strings: menus, prefs, temp folders, defines, shader name, caches
# --------------------------------------------------------------------------------------------------
MENU = "FDP UI Kit"
STRING_RULES = [
    # menus (must run BEFORE the UIComponents namespace rule)
    (r'(\[(?:UnityEngine\.)?AddComponentMenu\(")Design System/',      rf'\1{MENU}/Design System/'),
    (r'(menuName\s*=\s*")Design System/',                              rf'\1{MENU}/Design System/'),
    (r'(MenuItem\(")Assets/Design System/',                            rf'\1Assets/{MENU}/Design System/'),
    (r'(MenuItem\(")Design System/',                                   rf'\1Tools/{MENU}/Design System/'),
    (r'(MenuItem\(")Window/UIComponents/',                             rf'\1Window/{MENU}/Components/'),
    (r'(MenuItem\(")UIComponents Examples/',                           rf'\1Tools/{MENU}/Components Examples/'),   # samples
    # sentences that name the menu (tooltips, warnings, USS comments, doc comments). The lookbehind keeps a second run from stacking the prefix.
    (r'(?<!UI Kit/)Tools/Design System/',                             f'Tools/{MENU}/Design System/'),
    (r'(?<!UI Kit > )Design System > ',                                f'Tools > {MENU} > Design System > '),
    (r'(?<!UI Kit &gt; )Design System &gt; ',                          f'Tools &gt; {MENU} &gt; Design System &gt; '),
    (r'\[UIComponents\] ',                                             '[UIKit.Components] '),                    # log prefix
    # editor prefs, temp + font folders, caches
    (r'"DesignSystem\.ThemeConfigurator\.PreviewUxmlGuid"',            f'"{FDP}.DesignSystem.ThemeConfigurator.PreviewUxmlGuid"'),
    (r'_DesignSystemTemp',                                             '_FutureDigitalPlusUIKitTemp'),
    (r'Assets/Resources/DsFonts',                                      f'Assets/Resources/{RES}/Fonts'),
    (r'(persistentDataPath,\s*)"DsFonts"',                             rf'\1"{RES}/Fonts"'),
    (r'persistentDataPath/DsFonts',                                    f'persistentDataPath/{RES}/Fonts'),
    (r'resource\("DsFonts/',                                           f'resource("{RES}/Fonts/'),
    # shader + public define
    (r'"Hidden/DsFx/Blueprint"',                                       '"Hidden/FutureDigitalPlus/UIKit/DsFx/Blueprint"'),
]
# --components assemblies: assembly names that appear in strings, while the namespace stays UIComponents
ASM_STRING_RULES = [
    (r'(InternalsVisibleTo\(")UIComponents((?:\.[A-Za-z]+)*)"',  rf'\1{FDP}.Components\2"'),
    (r'(Name\.StartsWith\(")UIComponents\.Tests"',             rf'\1{FDP}.Components.Tests"'),
]
# markdown-only: menu prose that is not also a C# or USS string
MD_ONLY_RULES = [
    (r'Window > UIComponents > ',                                      f'Window > {MENU} > Components > '),
    (r'`UIComponents Examples( > |`)',                                 rf'`Tools > {MENU} > Components Examples\1'),
]

# --------------------------------------------------------------------------------------------------
# 6. Which files get which rules
# --------------------------------------------------------------------------------------------------
TEXT_EXT = {".cs", ".uss", ".uxml", ".tss", ".asmdef", ".asmref", ".rsp", ".shader", ".cginc", ".md", ".asset"}
VERBATIM_GLOBS = [                           # never rewritten by the text stage
    "**/LICENSE*", "**/UNLICENSE*", "THIRD_PARTY_NOTICES.md", "CHANGELOG.md", "package.json",
    "**/SOURCE.md",                          # gets the generated record block only
    "Documentation~/Components/UIComponents-upstream-*.md",
    "Samples~/Extras/BlurUrpLegacy~/**",
    "Tests/Runtime/Components/NSubstitute/**",
    "Runtime/Components/Core/Roslyn/**",
    "Tools~/**",                             # this tooling and its mapping describe the old names on purpose
    # Written after the rename, already in the new names. The old-name rules would misread them: a class suffix such as UssClassName + "--empty"
    # looks like an old custom property, and the game-owned example tokens (--mygame-color-health) look like unprefixed ones.
    "Runtime/Foundation/**", "Runtime/Interaction/**", "Runtime/Language/**", "Runtime/Hud/**", "Runtime/Controls/**", "Runtime/Gameplay/**",
    "Runtime/Bootstrap/**", "Runtime/UIKit/**",
    "Tests/Pure/**", "Tests/Editor/Foundation/**", "Tests/Editor/Interaction/**", "Tests/Editor/Language/**", "Tests/Editor/Lint/**", "Tests/Editor/UIKit/**",
    "Tests/Runtime/Foundation/**", "Tests/Runtime/Interaction/**", "Tests/Runtime/Language/**", "Tests/Runtime/Hud/**", "Tests/Runtime/Controls/**",
    "Tests/Runtime/Gameplay/**", "Tests/Runtime/Bootstrap/**", "Tests/Runtime/UIKit/**", "Tests/Runtime/Platform/**",
    "Runtime/DesignSystem/Theme/Data/ThemeData.Runtime.cs", "Tests/Editor/DesignSystem/ThemingEditorTests.cs",
    "Samples~/ShellQuickStart/**", "Samples~/RpgInventory/**", "Samples~/ActionHud/**", "Samples~/GamepadMenu/**", "Samples~/Responsive/**", "Samples~/ThemingLanguage/**",
    "Documentation~/Foundation/**", "Documentation~/Interaction/**", "Documentation~/Language/**", "Documentation~/Hud/**", "Documentation~/Controls/**",
    "Documentation~/Gameplay/**", "Documentation~/Bootstrap/**", "Documentation~/UIKit/**", "Documentation~/DesignSystem/THEMING.md",
    "Documentation~/Recipes.md", "Documentation~/Migration.md", "Documentation~/ChooseModules.md", "Documentation~/Samples.md", "Documentation~/Platforms.md",
    "Documentation~/CompatibilityMatrix.md", "Documentation~/Verification.md", "Documentation~/GettingStarted.md", "Documentation~/Upgrading.md",
    "Documentation~/index.md", "README.md",
]
# upstream-derived code: files changed in these get the one-line FutureDigitalPlus marker
UPSTREAM_CODE_GLOBS = [
    "Runtime/DesignSystem/**/*.cs", "Runtime/Animation/**/*.cs", "Runtime/Components/**/*.cs", "Runtime/Extras/**/*.cs",
    "Editor/DesignSystem/**/*.cs", "Editor/Components/**/*.cs",
    "Samples~/Components/**/*.cs",
    "Tests/Runtime/Components/*.cs", "Tests/Runtime/Components/Roslyn/*.cs", "Tests/Runtime/Components/Utilities/*.cs",
    "Tests/Editor/Components/*.cs",
]
SOURCE_MD = {   # module SOURCE.md -> (title, which map sections it lists)
    "Runtime/DesignSystem/SOURCE.md": "DesignSystem",
    "Runtime/Animation/SOURCE.md":    "Animation",
    "Runtime/Components/SOURCE.md":   "Components",
    "Runtime/Extras/SOURCE.md":       "Extras",
    "Runtime/UIKit/SOURCE.md":        "UIKit",
}
# Files and names written after the rename (new modules, lint rules, development tools) that quote old-style names on purpose as test data or document game-owned tokens;
# the stale-name and custom-property checks are for the upstream-derived modules the script renamed.
VERIFY_SKIP_PREFIXES = ("Tools~/", "Tests/Editor/Lint/", "Tests/Editor/DesignSystem/ThemingEditorTests.cs")
# Theme tokens that are baked into the themes but deliberately not declared in DesignTokens.uss (consumers fall back through var()), see DesignSystem/SOURCE.md.
UNDECLARED_BAKED_TOKENS = {"--fdp-color-focus-ring"}
# Placeholders in prose and test strings.
EXAMPLE_TOKENS = {"--fdp-foundation", "--fdp-other", "--fdp-w", "--fdp-x"}
KNOWN_EXTERNAL_ASM = {
    "UnityEngine.TestRunner", "UnityEditor.TestRunner", "Unity.Addressables", "Unity.ResourceManager",
    "Unity.Addressables.Editor", "Unity.Localization", "Unity.InputSystem", "Unity.TextMeshPro", "UnityEngine.UI",
}
OLD_TOKENS = [   # leftovers the verify stage looks for
    r"(?<![\w.])instance\.id\.EATK\b", r"(?<![\w.])DesignSystem\.Runtime\b", r"(?<![\w.])DesignSystem\.Editor\b",
    r"(?<![\w.])Game\.UI\b", r"\bid\.instance\.elementanimationtoolkit\b",
    r"\bDS_DISABLE_AUTO_ATTACH\b", r"Hidden/DsFx/Blueprint", r"_DesignSystemTemp",
]
OLD_TOKENS += [
    r"(?<![\w-])\.mobile(?![\w-])",                                                    # uss selector
    r'(?:ClassList\(|ClassListContains\(|MobileClass\s*=\s*)"mobile"',
]
OLD_TOKENS += [
    r'(?<![\w./~-])/?(?:UI/Styles/DesignSystem|UI/Themes|Textures/Icons|Fx/Shaders|GameUI/Styles)(?![\w-])',   # old Resources paths
    rf'(?:className\s*:\s*|ClassList\(|ClassListContains\(|const\s+string\s+uss\w*\s*=\s*)"(?:{"|".join(EXTRAS_CLASS_BASES)})(?:__[\w-]+)?"',
]
OLD_VAR_FILES = (".uss", ".tss", ".uxml", ".asset", ".cs")
_SCOPED = r"(?!fdp-|unity-|ds-|token\b)"                  # prose placeholder "--token" is not a property
OLD_VAR_RE = re.compile(rf'(?<![\w-])--{_SCOPED}[A-Za-z][\w-]*(?=\s*[:)\s,]|$)')
OLD_VAR_UXML_RE = re.compile(rf'(?<![\w-])--{_SCOPED}[A-Za-z][\w-]*\s*:|var\(\s*--{_SCOPED}[A-Za-z]')   # UXML: label text may say "--danger"
OLD_VAR_CS_RE = re.compile(rf'"--{_SCOPED}[A-Za-z]|var\(--{_SCOPED}[A-Za-z]')   # C#: only string literals (`--t` is a decrement)
OLD_TOKENS_COMPONENTS = [r"(?<![\w.])UIComponents(?=\.[A-Z]|;|\s*\{)(?!\.Roslyn)", r"\[UIComponents\] ", r"UIComponents Examples"]


# --------------------------------------------------------------------------------------------------
class Ctx:
    def __init__(self, root: Path, apply: bool, components: str, auto_ref: str):
        self.root, self.apply, self.components, self.auto_ref = root, apply, components, auto_ref
        self.changes: dict[str, Counter] = defaultdict(Counter)   # file -> rule -> count
        self.moves: list[tuple[Path, Path]] = []
        self.asm_renames: dict[str, str] = {}
        self.new_dirs: set[Path] = set()
        self.removed_dirs: list[Path] = []
        self.review: list[str] = []
        self.problems: list[str] = []

    def rel(self, p: Path) -> str:
        return p.relative_to(self.root).as_posix()


def is_verbatim(rel: str) -> bool:
    return any(fnmatch.fnmatch(rel, g) or fnmatch.fnmatch(Path(rel).name, g) for g in VERBATIM_GLOBS)


def is_upstream_comment(line: str) -> bool:
    return bool(re.match(r"\s*(//|\*|/\*|<!--)", line)) and "upstream" in line.lower()


def all_files(root: Path):
    for p in sorted(root.rglob("*")):
        if p.is_file() and ".git" not in p.relative_to(root).parts:
            yield p


def read(p: Path) -> tuple[str, bool]:
    b = p.read_bytes()
    bom = b.startswith(b"\xef\xbb\xbf")
    return b.decode("utf-8-sig"), bom


def write(p: Path, text: str, bom: bool):
    p.write_bytes((b"\xef\xbb\xbf" if bom else b"") + text.encode("utf-8"))


# ----------------------------------------------------------------------- assemblies
def map_asm(name: str, ctx: Ctx):
    """(new name, new rootNamespace or None) or None if unchanged."""
    if name in ASM_EXACT:
        return ASM_EXACT[name]
    old, new = COMPONENTS_ASM_PREFIX
    if ctx.components in ("rename", "assemblies") and (name == old or name.startswith(old + ".")):
        return (new + name[len(old):], None)
    return None


def stage_asmdef(ctx: Ctx):
    for p in sorted(ctx.root.rglob("*.asmdef")):
        text, bom = read(p)
        orig = text
        m = re.search(r'"name"\s*:\s*"([^"]+)"', text)
        if not m:
            continue
        old_name = m.group(1)
        mapped = map_asm(old_name, ctx)
        new_name = mapped[0] if mapped else old_name
        if mapped and new_name != old_name:
            ctx.asm_renames[old_name] = new_name
        # references by name (GUID: references are left alone; the package uses names)
        def ref(mm):
            r = map_asm(mm.group(1), ctx)
            return f'"{r[0]}"' if r else mm.group(0)
        refs_m = re.search(r'("references"\s*:\s*\[)(.*?)(\])', text, re.S)
        if refs_m:
            body = re.sub(r'"([^"]+)"', lambda mm: ref(mm) if not mm.group(1).startswith("GUID:") else mm.group(0), refs_m.group(2))
            text = text[:refs_m.start(2)] + body + text[refs_m.end(2):]
        text = re.sub(r'("name"\s*:\s*")[^"]+(")', rf'\g<1>{new_name}\g<2>', text, count=1)
        # rootNamespace: explicit from table, else run the namespace rules over it, else (empty) leave
        rn = re.search(r'("rootNamespace"\s*:\s*")([^"]*)(")', text)
        if rn:
            cur = rn.group(2)
            new_rn = mapped[1] if (mapped and mapped[1]) else apply_ns(cur, ctx, md=False)
            text = text[:rn.start(2)] + new_rn + text[rn.end(2):]
        if ctx.auto_ref == "facade":
            ar = re.search(r'"autoReferenced"\s*:\s*(true|false)', text)
            if ar and not (new_name in AUTOREF_KEEP) and "UNITY_INCLUDE_TESTS" not in text and ar.group(1) == "true":
                text = text.replace(ar.group(0), '"autoReferenced": false')
        if text != orig:
            ctx.changes[ctx.rel(p)]["asmdef"] += 1
            if ctx.apply:
                write(p, text, bom)
        if new_name != old_name:   # rename file + meta so file name == assembly name
            dst = p.with_name(new_name + ".asmdef")
            ctx.moves.append((p, dst))
            if ctx.apply:
                move_with_meta(p, dst)


def move_with_meta(src: Path, dst: Path):
    dst.parent.mkdir(parents=True, exist_ok=True)
    if dst.exists():
        raise SystemExit(f"refusing to overwrite {dst}")
    os.rename(src, dst)
    sm = Path(str(src) + ".meta")
    if sm.exists():
        os.rename(sm, Path(str(dst) + ".meta"))


# ----------------------------------------------------------------------- text rules
def apply_ns(text: str, ctx: Ctx, md: bool) -> str:
    for pat, rep in NS_RULES:
        text = re.sub(pat, rep, text)
    if ctx.components == "rename":
        pat, rep = NS_COMPONENTS_MD if md else NS_COMPONENTS_CODE
        text = re.sub(pat, rep, text)
    elif ctx.components == "assemblies":
        for pat, rep in ASM_STRING_RULES:
            text = re.sub(pat, rep, text)
    return text


def collect_variable_names(root: Path) -> set[str]:
    """package-owned USS custom properties: declarations (`--name:`), `var(--name` uses, C# string literals starting with
    `--`, baked `m_CustomName` entries. Prose such as a `--danger` modifier shorthand is not a property. `--unity-*` and
    `--ds-*` (already scoped) are excluded."""
    decl = re.compile(r"(?<![\w-])--([A-Za-z][\w-]*)\s*:")
    use = re.compile(r"var\(\s*--([A-Za-z][\w-]*)")
    names: set[str] = set()
    for p in all_files(root):
        if p.suffix in (".uss", ".tss"):
            t = re.sub(r"/\*.*?\*/", "", read(p)[0], flags=re.S)
            names.update(decl.findall(t))
            names.update(use.findall(t))
        elif p.suffix == ".uxml":
            t = read(p)[0]
            names.update(use.findall(t))
            names.update(re.findall(r'style\s*=\s*"[^"]*?--([A-Za-z][\w-]*)\s*:', t))
        elif p.suffix == ".cs":
            t = read(p)[0]
            names.update(re.findall(r'"--([A-Za-z][\w-]*)', t))
            names.update(use.findall(t))
        elif p.suffix == ".asset":
            names.update(re.findall(r"m_CustomName:\s*--([A-Za-z][\w-]*)", read(p)[0]))
    return {n for n in names if not n.startswith(("unity-", "ds-", VAR_PREFIX)) and "--" not in n}


def build_pkg_rules(root: Path):
    """package-relative path rules (Runtime/DesignSystem/Resources/UI/Themes ...), one per existing Resources root in MOVES."""
    rules = []
    for pat, old, new in MOVES:
        if not old:
            continue
        for rr in sorted(glob.glob(str(root / pat))):
            rel = Path(rr).relative_to(root).as_posix()
            rules.append((re.escape(f"{rel}/{old}") + _END, f"{rel}/{new}"))
    return rules


def build_var_rule(names: set[str]):
    """`--name` -> `--fdp-name`, and the wildcard shorthand `--color-back-*` used in prose for a family of names."""
    if not names:
        return None
    alt = "|".join(sorted((re.escape(n) for n in names), key=len, reverse=True))
    prefixes = {n[:i + 1] for n in names for i, c in enumerate(n) if c == "-"}
    pre = "|".join(sorted((re.escape(x) for x in prefixes), key=len, reverse=True))
    return (rf'(?<![\w-])(?<!\+ ")(?<!\+")--(?=(?:{alt})(?![\w-])|(?:{pre})\*)', f"--{VAR_PREFIX}")


def extras_rules():
    bases = "|".join(re.escape(b) for b in EXTRAS_CLASS_BASES)
    css = (rf"(?<![\w\)\]])\.({bases})(?=__|[^\w-]|$)", rf".{CLASS_PREFIX}\1")
    # const string uss... = "tooltip__label" and class lookups in code: className: "x", AddToClassList("x") ...
    cs = (rf'((?:const\s+string\s+uss\w*\s*=\s*|className\s*:\s*|(?:Add|Remove|Toggle)(?:To|From|In)ClassList\(|ClassListContains\()")({bases})((?:__[\w-]+)?")', rf"\1{CLASS_PREFIX}\2\3")
    uxml = (rf'(class\s*=\s*"[^"]*?)(?<![\w-])({bases})(?=__|[\s"])', rf"\1{CLASS_PREFIX}\2")
    return css, cs, uxml


def sample_prefix_for(rel: str):
    for folder, prefix in SAMPLE_LITERAL_PREFIX.items():
        if rel.startswith(folder + "/"):
            return prefix
    return None


def rewrite_sample_literals(text: str, prefix: str) -> str:
    def fix(m):
        v = m.group(2)
        if v.startswith(("Assets/", "Packages/", RES + "/")):
            return m.group(0)
        return f"{m.group(1)}{prefix}{v}{m.group(3)}"
    text = re.sub(r'(\[(?:Layout|Stylesheet|SharedStylesheet)\(")([^"]+)(")', fix, text)
    text = re.sub(r'(Resources\.Load(?:<[^>]+>)?\(")([^"]+)(")', fix, text)
    return text


def stage_text(ctx: Ctx):
    var_rule = build_var_rule(collect_variable_names(ctx.root))
    pkg_rules = build_pkg_rules(ctx.root)
    css_x, cs_x, uxml_x = extras_rules()
    for p in all_files(ctx.root):
        rel = ctx.rel(p)
        if p.suffix not in TEXT_EXT or is_verbatim(rel):
            continue
        text, bom = read(p)
        is_md = p.suffix == ".md"
        total: Counter = Counter()

        def run(t: str, counts: Counter) -> str:
            def sub(name, pat, rep, s):
                new, n = re.subn(pat, rep, s)
                if n and new != s:
                    counts[name] += n
                return new
            for pat, rep in STRING_RULES:                       # menus first (before the UIComponents namespace rule)
                t = sub("string", pat, rep, t)
            for pat, rep in pkg_rules:
                t = sub("resources-pkg", pat, rep, t)
            for pat, rep in RES_LITERAL_RULES:
                t = sub("resources", pat, rep, t)
            ns = apply_ns(t, ctx, is_md)
            if ns != t:
                counts["namespace"] += 1
                t = ns
            if var_rule:
                t = sub("uss-var", var_rule[0], var_rule[1], t)
            for pat, rep in CLASS_RULES:
                t = sub("uss-class", pat, rep, t)
            if p.suffix in (".uss", ".tss") or is_md:
                t = sub("uss-class", css_x[0], css_x[1], t)
                t = sub("touch-class", MOBILE_CSS_RULE[0], MOBILE_CSS_RULE[1], t)
            if p.suffix == ".cs" or is_md:
                for pat, rep in MOBILE_CS_RULES:
                    t = sub("touch-class", pat, rep, t)
            if is_md:
                for pat, rep in MOBILE_MD_RULES:
                    t = sub("touch-class", pat, rep, t)
            if p.suffix == ".cs":
                t = sub("uss-class", cs_x[0], cs_x[1], t)
            if p.suffix == ".uxml":
                t = sub("uss-class", uxml_x[0], uxml_x[1], t)
            if is_md:
                for pat, rep in MD_ONLY_RULES:
                    t = sub("string", pat, rep, t)
            return t

        if is_md:
            # a line that talks about "upstream" documents the old names on purpose: report it, do not rewrite it
            out = []
            for i, line in enumerate(text.split("\n"), 1):
                c: Counter = Counter()
                new = run(line, c)
                if new != line and re.search(r"upstream", line, re.I):
                    ctx.review.append(f"{rel}:{i}: mentions upstream, left as is: {line.strip()[:120]}")
                    out.append(line)
                else:
                    total.update(c)
                    out.append(new)
            new_text = "\n".join(out)
        else:
            # a line naming upstream's package id (com.sinanata.designsystem) documents upstream's layout: left as is
            # A comment line that says "upstream" states a fact about upstream's names ("upstream hard-codes ..."): left as is.
            lines = text.split("\n")
            if any("com.sinanata." in l or is_upstream_comment(l) for l in lines):
                new_text = "\n".join(l if "com.sinanata." in l or is_upstream_comment(l) else run(l, total) for l in lines)
            else:
                new_text = run(text, total)
            sp = sample_prefix_for(rel)
            if sp and p.suffix == ".cs":
                t2 = rewrite_sample_literals(new_text, sp)
                if t2 != new_text:
                    total["sample-literals"] += 1
                    new_text = t2

        if new_text != text:
            if p.suffix == ".cs" and any(fnmatch.fnmatch(rel, g) for g in UPSTREAM_CODE_GLOBS) and RENAME_MARK not in new_text:
                new_text = RENAME_MARK + ("\r\n" if "\r\n" in new_text else "\n") + new_text
                total["marker"] += 1
            ctx.changes[rel].update(total)
            if ctx.apply:
                write(p, new_text, bom)


# ----------------------------------------------------------------------- resource moves
def stage_moves(ctx: Ctx):
    for pat, old, new in MOVES:
        for rr in sorted(glob.glob(str(ctx.root / pat))):
            rr = Path(rr)
            src = rr / old if old else rr
            if not rr.is_dir() or not src.is_dir():
                continue
            dst = rr / new
            # a child's .meta travels with the child (move_with_meta); the already-moved prefix folder is skipped
            children = [c for c in sorted(src.iterdir())
                        if not c.name.endswith(".meta") and not (not old and c.name == "FutureDigitalPlus")]
            for c in children:
                ctx.moves.append((c, dst / c.name))
                if ctx.apply:
                    move_with_meta(c, dst / c.name)
            if ctx.apply:
                sm, dm = Path(str(src) + ".meta"), Path(str(dst) + ".meta")
                if old and sm.exists() and not dm.exists() and dst.exists():
                    os.rename(sm, dm)                  # the old leaf folder's meta (stable GUID) becomes the new leaf's
                prune_empty(rr, ctx)
                for d in [dst] + list(dst.parents):
                    if d == rr:
                        break
                    ctx.new_dirs.add(d)


def prune_empty(res_root: Path, ctx: Ctx):
    for d in sorted([x for x in res_root.rglob("*") if x.is_dir()], key=lambda x: len(x.parts), reverse=True):
        if not any(d.iterdir()):
            m = Path(str(d) + ".meta")
            d.rmdir()
            if m.exists():
                m.unlink()
            ctx.removed_dirs.append(d)


# ----------------------------------------------------------------------- records
def pat_to_name(pat: str) -> str:
    return re.sub(r"\(\?<!\[\\w\.\]\)|\\b|\\", "", pat)


# what each module's record lists: old-name regexes for assembly / namespace / resources rows, and prose paragraphs
MODULE_SCOPE = {
    "DesignSystem": dict(asm=r"^DesignSystem\.|\.Tests\.DesignSystem\.", ns=r"DesignSystem", res=r"^(UI/|Textures/|Fx/)", prose=("vars", "ds", "touch", "menus", "autoattach")),
    "Animation":    dict(asm=r"elementanimationtoolkit|\.Tests\.Animation\.", ns=r"EATK|elementanimationtoolkit", res=r"^Style$", prose=()),
    "Components":   dict(asm=r"^UIComponents|\.Tests\.Components$", ns=r"^UIComponents$", res=r"^(UIComponentTests|FutureDigitalPlusUIKitTests)$", prose=("menus", "generator")),
    "Extras":       dict(asm=r"^FutureDigitalPlus\.UIKit\.Extras$", ns=r"Game\.UI", res=r"^GameUI/", prose=("extras",)),
    "UIKit":        dict(asm=r"^$", ns=r"^$", res=r"^FutureDigitalPlus(UIKitTests|Samples)$", prose=("touch",)),
}
PROSE = {
    "vars": f"USS custom properties: `--<name>` became `--{VAR_PREFIX}<name>` (`--unity-*` and `--ds-*` untouched); the baked theme sheets (`Themes/*.asset`) follow.",
    "ds": "`.back-btn*` became `.ds-back-btn*`, `is-spinning` became `ds-is-spinning`; the shader name became `Hidden/FutureDigitalPlus/UIKit/DsFx/Blueprint`.",
    "touch": f"The touch-size class `mobile` became `{TOUCH_CLASS}`.",
    "menus": "Menus moved under `FDP UI Kit` (`Tools/`, `Assets/`, `Window/`, `AddComponentMenu`, and `Design System >` sentences in code and stylesheets).",
    "autoattach": "Not mechanical, recorded in the modification table: the auto-attach define (opt-out `DS_DISABLE_AUTO_ATTACH` became opt-in `FDP_UIKIT_AUTO_ATTACH`).",
    "extras": f"Extras classes `{', '.join(EXTRAS_CLASS_BASES)}` got the prefix `{CLASS_PREFIX}`.",
    "generator": "The four Roslyn DLLs were rebuilt from the upstream source with the namespace literals patched (see `Tools~/rename/README.md` and `THIRD_PARTY_NOTICES.md`).",
}


def mapping_markdown(module: str, ctx: Ctx) -> str:
    sc = MODULE_SCOPE.get(module, dict(asm=r"", ns=r"", res=r"", prose=()))
    L = [f"Generated by `Tools~/rename/rename.py` (collision-proofing). Module: {module}. The upstream names survive only in this record.", ""]
    asm = [(o, n) for o, n in sorted(ctx.asm_renames.items()) if re.search(sc["asm"], o)]
    if asm:
        L += ["| Assembly (old) | Assembly (new) |", "|---|---|"] + [f"| `{o}` | `{n}` |" for o, n in asm]
        L.append("")
    ns = [(pat_to_name(pat), rep) for pat, rep in NS_RULES if re.search(sc["ns"], pat_to_name(pat))]
    if ctx.components == "rename" and re.search(sc["ns"], "UIComponents"):
        ns.append(("UIComponents", f"{FDP}.Components"))
    if ns:
        L += ["| Namespace (old) | Namespace (new) |", "|---|---|"] + [f"| `{o}` | `{n}` |" for o, n in ns]
        L.append("")
    res = [(old, new) for _, old, new in MOVES if old and re.search(sc["res"], old)]
    res = list(dict.fromkeys(res))
    if res:
        L += ["| Resources path (old) | Resources path (new) |", "|---|---|"] + [f"| `{o}/` | `{n}/` |" for o, n in res]
        L.append("")
    for k in sc["prose"]:
        L += [PROSE[k], ""]
    return "\n".join(L).rstrip("\n")


def stage_records(ctx: Ctx):
    for rel, module in SOURCE_MD.items():
        p = ctx.root / rel
        if not p.exists():
            continue
        text, bom = read(p)
        block = f"{REC_BEGIN}\n## Rename record\n\n{mapping_markdown(module, ctx)}\n{REC_END}"
        if REC_BEGIN in text and not ctx.asm_renames:
            continue                                   # a re-run has nothing new to record
        if REC_BEGIN in text:
            text = re.sub(re.escape(REC_BEGIN) + ".*?" + re.escape(REC_END), lambda m: block, text, flags=re.S)
        else:
            text = text.rstrip("\n") + "\n\n" + block + "\n"
        ctx.changes[rel]["record"] += 1
        if ctx.apply:
            write(p, text, bom)


# ----------------------------------------------------------------------- verify
def stage_verify(ctx: Ctx):
    root = ctx.root
    asm: dict[str, Path] = {}
    for p in sorted(root.rglob("*.asmdef")):
        j = json.loads(read(p)[0])
        n = j["name"]
        if n in asm:
            ctx.problems.append(f"duplicate assembly name {n}: {ctx.rel(asm[n])} and {ctx.rel(p)}")
        asm[n] = p
        if p.stem != n:
            ctx.problems.append(f"asmdef file name differs from assembly name: {ctx.rel(p)} -> {n}")
    for n, p in asm.items():
        for r in json.loads(read(p)[0]).get("references", []):
            if not (r.startswith("GUID:") or r in asm or r in KNOWN_EXTERNAL_ASM or r.startswith("Unity.RenderPipelines.")):
                ctx.problems.append(f"{ctx.rel(p)}: reference '{r}' resolves to no asmdef in the package")
    tokens = list(OLD_TOKENS) + (OLD_TOKENS_COMPONENTS if ctx.components == "rename" else [])
    for p in all_files(root):
        rel = ctx.rel(p)
        if p.suffix not in TEXT_EXT or is_verbatim(rel) or rel.startswith(VERIFY_SKIP_PREFIXES):
            continue
        for i, line in enumerate(read(p)[0].split("\n"), 1):
            if (p.suffix == ".md" and re.search(r"upstream", line, re.I)) or is_upstream_comment(line):
                continue
            if any(re.search(t, line) for t in tokens):
                ctx.problems.append(f"stale name in {rel}:{i}: {line.strip()[:110]}")
    # package-owned custom properties must all carry the prefix (sheets, baked assets, C# literals)
    for p in all_files(root):
        rel = ctx.rel(p)
        if p.suffix not in OLD_VAR_FILES or is_verbatim(rel) or rel.startswith(("Samples~/Extras/BlurUrpLegacy~",) + VERIFY_SKIP_PREFIXES):
            continue
        text = read(p)[0]
        if p.suffix in (".uss", ".tss"):
            text = re.sub(r"/\*.*?\*/", lambda m: "\n" * m.group(0).count("\n"), text, flags=re.S)
        for i, line in enumerate(text.split("\n"), 1):
            if p.suffix == ".asset" and "m_CustomName" not in line:
                continue
            if p.suffix == ".cs" and re.search(r'\+\s*"--', line):    # a class-name suffix such as UssClassName + "--empty", not a custom property
                continue
            if {".cs": OLD_VAR_CS_RE, ".uxml": OLD_VAR_UXML_RE}.get(p.suffix, OLD_VAR_RE).search(line):
                ctx.problems.append(f"unprefixed custom property in {rel}:{i}: {line.strip()[:110]}")
    # every --fdp-* name that is used is declared somewhere (a renamed prose shorthand would show up here)
    declared, used = set(), {}
    for p in all_files(root):
        rel = ctx.rel(p)
        if p.suffix not in TEXT_EXT or is_verbatim(rel):
            continue
        text = read(p)[0]
        for m in re.finditer(r"(?<![\w-])(--fdp-[\w-]*[\w])", text):
            if text[m.end():m.end() + 2] == "-*":              # wildcard shorthand for a family of names
                continue
            used.setdefault(m.group(1), rel)
            if re.match(r"\s*:", text[m.end():m.end() + 4]) and p.suffix in (".uss", ".tss", ".uxml"):
                declared.add(m.group(1))
            if p.suffix == ".cs" and text[m.start() - 1:m.start()] == '"':
                declared.add(m.group(1))
    for n, rel in sorted(used.items()):
        if n not in declared and n not in EXAMPLE_TOKENS and not re.search(r"--fdp-<|-$", n):
            ctx.problems.append(f"{n} is used (first in {rel}) but declared nowhere: renamed prose?")
    # every --fdp-* custom name baked into a theme asset is defined in the tokens sheet
    tokens_uss = "\n".join(read(p)[0] for p in root.rglob("DesignTokens.uss"))
    for p in all_files(root):
        if p.suffix == ".asset":
            for n in set(re.findall(r"m_CustomName:\s*(--fdp-[\w-]+)", read(p)[0])):
                if n not in UNDECLARED_BAKED_TOKENS and not re.search(re.escape(n) + r"\s*:", tokens_uss):
                    ctx.problems.append(f"{ctx.rel(p)}: baked token {n} is not defined in DesignTokens.uss")
    res_files: set[str] = set()
    for rr in root.rglob("Resources"):
        if rr.is_dir():
            for f in rr.rglob("*"):
                if f.is_file() and f.suffix != ".meta":
                    rp = f.relative_to(rr).as_posix()
                    res_files.add(rp)
                    s1 = re.sub(r"\.[^./]+$", "", rp)
                    res_files.add(s1)
                    res_files.add(re.sub(r"\.[^./]+$", "", s1))      # X.style.uss -> X.style -> X
    lit = re.compile(r'(?:Resources\.Load(?:All|Async)?(?:<[^>]+>)?\(|\[(?:Layout|Stylesheet|SharedStylesheet)\(|resource\()\s*[\\]?["\']([^"\'\\]+)')
    for p in all_files(root):
        rel = ctx.rel(p)
        if p.suffix not in (".cs", ".uss", ".uxml") or is_verbatim(rel):
            continue
        for m in lit.finditer(read(p)[0]):
            v = m.group(1)
            if v.startswith(("Assets/", "Packages/")) or "{" in v or v in ("", "/"):
                continue
            if v not in res_files and not any(f.startswith(v.rstrip("/") + "/") for f in res_files):
                ctx.review.append(f"resource literal resolves to no file in any Resources folder: {rel}: '{v}'")
    for d in sorted(ctx.new_dirs):
        if not Path(str(d) + ".meta").exists() and "~" not in ctx.rel(d):
            ctx.review.append(f"new folder has no .meta yet (let Unity generate it, then keep it): {ctx.rel(d)}")


# ----------------------------------------------------------------------- Roslyn generator rebuild
def stage_generator(src: Path, out: Path, roslyn: str, ns: str, nuget_source: str = ""):
    """Copy upstream UIComponents.Roslyn, patch the runtime-namespace string literals, rename the 4 assemblies, build."""
    work = out / "src"
    if work.exists():
        shutil.rmtree(work)
    shutil.copytree(src, work, ignore=shutil.ignore_patterns("obj", "bin", "*.Test", "*.Tests", "*.Package", ".git"))
    patched = 0
    for p in work.rglob("*.cs"):
        if p.name.endswith("Designer.cs"):
            continue
        t = p.read_text(encoding="utf-8-sig")
        n = re.sub(r'"UIComponents(?!\.Roslyn)(?=[."])', '"' + ns, t)
        if n != t:
            p.write_text(n, encoding="utf-8")
            patched += 1
    # FutureDigitalPlus: shipped fix (not a rename). The generator shortens a type name by every `using` of the component's file, but
    # keeps UIComponents' own types explicit only when the *using* starts with the framework namespace. With `using FutureDigitalPlus.UIKit;`
    # (the facade) the type FutureDigitalPlus.UIKit.Components.X became `Components.X` and did not compile (CS0246). Framework types are
    # now never shortened.
    ctx_file = work / "UIComponents.Roslyn.Generation/Generators/AugmentGenerationContext.cs"
    t = ctx_file.read_text(encoding="utf-8-sig")
    marker = "var typeName = type.ToDisplayString();\n"
    if marker not in t.replace("\r\n", "\n"):
        raise SystemExit("generator patch: AugmentGenerationContext.GetTypeName changed upstream, update the patch")
    t = t.replace("\r\n", "\n").replace(marker, marker + f'\n            // FutureDigitalPlus: never shorten the framework\'s own types (a using of the facade namespace made them unresolvable).\n            if (typeName.StartsWith("{ns}."))\n                return typeName;\n', 1)
    ctx_file.write_text(t, encoding="utf-8")
    asm_new = {
        "UIComponents.Roslyn.Common": f"{FDP}.Components.Roslyn.Common",
        "UIComponents.Roslyn.Generation": f"{FDP}.Components.Roslyn.Generation",
        "UIComponents.Roslyn.Analyzers": f"{FDP}.Components.Roslyn.Analyzers",
        "UIComponents.Roslyn.Analyzers.CodeFixes": f"{FDP}.Components.Roslyn.Analyzers.CodeFixes",
    }
    for csproj in work.rglob("*.csproj"):
        if any(x in csproj.name for x in (".Test", ".Package")):
            continue
        t = csproj.read_text(encoding="utf-8-sig")
        t = re.sub(r'\s*<PackageReference Include="coverlet.collector".*?</PackageReference>', "", t, flags=re.S)
        t = re.sub(r'(Microsoft\.CodeAnalysis(?:\.CSharp(?:\.Workspaces)?)?)" Version="[^"]+"', rf'\1" Version="{roslyn}"', t)
        t = t.replace('Include="Microsoft.CodeAnalysis" Version', 'Include="Microsoft.CodeAnalysis.CSharp" Version') if roslyn.startswith("4.0") else t
        stem = csproj.stem
        if stem in asm_new and "<AssemblyName>" not in t:
            t = t.replace("<PropertyGroup>", f"<PropertyGroup>\n    <AssemblyName>{asm_new[stem]}</AssemblyName>", 1)
        csproj.write_text(t, encoding="utf-8")
    out.mkdir(parents=True, exist_ok=True)
    dlls = out / "dll"
    for rel in ["UIComponents.Roslyn.Generation/UIComponents.Roslyn.Generation.csproj",
                "UIComponents.Roslyn.Analyzers/UIComponents.Roslyn.Analyzers/UIComponents.Roslyn.Analyzers.csproj",
                "UIComponents.Roslyn.Analyzers/UIComponents.Roslyn.Analyzers.CodeFixes/UIComponents.Roslyn.Analyzers.CodeFixes.csproj"]:
        cmd = ["dotnet", "build", str(work / rel), "-c", "Release", "-o", str(dlls)] + (["--source", nuget_source] if nuget_source else [])
        r = subprocess.run(cmd, capture_output=True, text=True)
        if r.returncode != 0:
            print(r.stdout[-2000:], r.stderr[-2000:])
            raise SystemExit("generator build failed (offline NuGet cache may lack Microsoft.CodeAnalysis " + roslyn + ")")
    print(f"generator: patched {patched} files, DLLs in {dlls}")
    for f in sorted(dlls.glob("*.dll")):
        print("  ", f.name)


def install_generator(ctx: Ctx, dll_dir: Path):
    """Replace the stock DLLs by the rebuilt ones. A meta is derived from the stock meta (it carries the labels
    RoslynAnalyzer + SourceGenerator and the importer settings) with a NEW but deterministic guid, so a project that also has the
    upstream package never sees two assets with one guid. This is the one place a meta is not generated by Unity;
    the later stage must open the copy in Unity once and confirm the labels survive and both analyzers run."""
    target = ctx.root / "Runtime/Components/Core/Roslyn"
    for f in sorted(dll_dir.glob(f"{FDP}.Components.Roslyn.*")):
        if f.suffix not in (".dll", ".pdb"):
            continue
        stock_name = f.name.replace(f"{FDP}.Components.Roslyn.", "UIComponents.Roslyn.")
        stock_meta = target / (stock_name + ".meta")
        print("install", f.name, "<-", stock_name)
        if not ctx.apply:
            continue
        if not stock_meta.exists():
            ctx.problems.append(f"no stock meta for {stock_name} (already installed?)")
            continue
        guid = uuid.uuid5(uuid.NAMESPACE_URL, f"com.futuredigitalplus.uikit/{f.name}").hex   # stable: a re-run gives the same GUID
        meta = re.sub(r"guid: [0-9a-f]{32}", "guid: " + guid, stock_meta.read_text(encoding="utf-8"), count=1)
        shutil.copy2(f, target / f.name)
        (target / (f.name + ".meta")).write_text(meta, encoding="utf-8")
        (target / stock_name).unlink(missing_ok=True)
        stock_meta.unlink()
    print("remember: record the SHA-256 of the new DLLs and the build recipe in THIRD_PARTY_NOTICES.md and Runtime/Components/SOURCE.md")


# ----------------------------------------------------------------------- main
def dump_map(components: str) -> str:
    """The whole mapping as markdown (Tools~/rename/rename-map.md): one section per module."""
    ctx = Ctx(Path("."), False, components, "keep")
    for k, (n, _) in ASM_EXACT.items():
        ctx.asm_renames[k] = n
    ctx.asm_renames["UIComponents[.*]"] = f"{FDP}.Components[.*]"
    out = ["# Rename mapping (generated by `rename.py --dump-map`)", ""]
    for module in MODULE_SCOPE:
        out += [f"## {module}", "", mapping_markdown(module, ctx), ""]
    return "\n".join(out)


def main(argv=None) -> int:
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--root")
    ap.add_argument("--apply", action="store_true")
    ap.add_argument("--allow-live", action="store_true")
    ap.add_argument("--components", choices=["rename", "assemblies", "keep"], default="rename")
    ap.add_argument("--auto-ref", choices=["keep", "facade"], default="keep")
    ap.add_argument("--verify-only", action="store_true")
    ap.add_argument("--dump-map", action="store_true")
    ap.add_argument("--report")
    ap.add_argument("--generator-src")
    ap.add_argument("--generator-out")
    ap.add_argument("--roslyn", default="3.8.0", help="Microsoft.CodeAnalysis version (upstream pins 3.8.0; 4.0.1 is in an offline NuGet cache)")
    ap.add_argument("--nuget-source", default="", help="e.g. ~/.nuget/packages for an offline build")
    ap.add_argument("--install-generator")
    a = ap.parse_args(argv)

    if a.dump_map:
        print(dump_map(a.components))
        return 0
    if a.generator_src:
        if not a.generator_out:
            return ap.error("--generator-out required")
        stage_generator(Path(a.generator_src), Path(a.generator_out), a.roslyn, f"{FDP}.Components", os.path.expanduser(a.nuget_source))
        return 0
    if not a.root:
        return ap.error("--root required")
    root = Path(a.root).resolve()
    if not (root / "package.json").exists():
        print("not a package root:", root)
        return 2
    if a.apply and root == LIVE_PACKAGE.resolve() and not a.allow_live:
        print("refusing to write to the package that holds this script: run it on a copy, or pass --allow-live")
        return 2

    ctx = Ctx(root, a.apply, a.components, a.auto_ref)
    if not a.verify_only:
        stage_moves(ctx)
        stage_asmdef(ctx)
        stage_text(ctx)
        if a.install_generator:
            install_generator(ctx, Path(a.install_generator))
        stage_records(ctx)
    if a.apply or a.verify_only:
        stage_verify(ctx)
    else:
        print("(dry run: verify skipped, it only means something after --apply)")

    print(f"{'APPLIED' if a.apply else 'DRY RUN'} on {root}")
    print(f"  files moved: {len(ctx.moves)}; assemblies renamed: {len(ctx.asm_renames)}; files rewritten: {len(ctx.changes)}")
    totals = Counter()
    for c in ctx.changes.values():
        totals.update(c)
    print("  rule hits:", dict(totals))
    print(f"  review items: {len(ctx.review)}; problems: {len(ctx.problems)}")
    for r in ctx.review[:60]:
        print("   review:", r)
    for r in ctx.problems[:60]:
        print("   PROBLEM:", r)
    if a.report:
        Path(a.report).write_text(json.dumps({
            "moves": [[str(s), str(d)] for s, d in ctx.moves],
            "asm_renames": ctx.asm_renames,
            "changes": {k: dict(v) for k, v in ctx.changes.items()},
            "review": ctx.review, "problems": ctx.problems,
            "new_dirs_need_meta": sorted(ctx.rel(d) for d in ctx.new_dirs),
        }, indent=1))
    return 1 if ctx.problems else 0


if __name__ == "__main__":
    sys.exit(main())
