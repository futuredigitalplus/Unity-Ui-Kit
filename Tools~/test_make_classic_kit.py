#!/usr/bin/env python3
"""Tests of make_classic_kit.py on a small fake kit (no Unity). Run: python3 -m unittest discover -s Tools~ -p "test_make_classic_kit.py" -v"""
import contextlib
import importlib.util
import io
import json
import shutil
import tempfile
import unittest
from pathlib import Path

HERE = Path(__file__).resolve().parent
spec = importlib.util.spec_from_file_location("make_classic_kit", HERE / "make_classic_kit.py")
mck = importlib.util.module_from_spec(spec)
spec.loader.exec_module(mck)

RESOLVER = ('namespace X { internal static class TestAssetPaths {\n'
            '    public const string KitAssetRoot = "Packages/com.futuredigitalplus.uikit"; // CLASSIC-REWRITE:KitAssetRoot\n} }\n')


def make_source(root: Path) -> Path:
    src = root / "src"
    files = {
        "package.json": json.dumps({"name": "com.futuredigitalplus.uikit", "version": "1.2.3", "displayName": "Kit", "unity": "6000.3",
                                    "description": "d", "dependencies": {"com.unity.modules.ui": "1.0.0"},
                                    "samples": [{"displayName": "S", "description": "sd", "path": "Samples~/S"}]}),
        "package.json.meta": "meta-pj\n",
        "README.md": "# Kit\n\nBody\n",
        "README.md.meta": "guid: r\n",
        "Runtime.meta": "guid: runtime\n",
        "Runtime/A.cs": "class A {}\n",
        "Runtime/A.cs.meta": "guid: a\n",
        "Tests.meta": "guid: tests\n",
        "Tests/Editor/Components/AssetDatabaseAssetResolverTests.cs": RESOLVER,
        "Samples~/S/S.cs": "class S {}\n",
        "Tools~/make_classic_kit.py": "# tool\n",
        "Tools~/bin/Debug/x.dll": "build output",
        "Tools~/classic/VERSION.meta.tpl": "guid: version\n",
        "Tools~/classic/UIKit.folder.meta.tpl": "guid: uikit\nfolderAsset: yes\n",
        "Tools~/classic/FutureDigitalPlus.folder.meta.tpl": "guid: fdp\nfolderAsset: yes\n",
        ".gitignore": "!*~/\n",
        ".DS_Store": "junk",
    }
    for rel, text in files.items():
        p = src / rel
        p.parent.mkdir(parents=True, exist_ok=True)
        p.write_text(text)
    return src


def run(*args):
    out, err = io.StringIO(), io.StringIO()
    with contextlib.redirect_stdout(out), contextlib.redirect_stderr(err):
        code = mck.main([str(a) for a in args])
    return code, out.getvalue(), err.getvalue()


def snapshot(d: Path):
    return {p.relative_to(d).as_posix(): (p.read_bytes(), p.stat().st_mtime_ns) for p in sorted(d.rglob("*")) if p.is_file()}


class ClassicKitTests(unittest.TestCase):
    def setUp(self):
        self.tmp = Path(tempfile.mkdtemp(prefix="classic-kit-test-"))
        self.src = make_source(self.tmp)
        self.dst = self.tmp / "Game" / "Assets" / "FutureDigitalPlus" / "UIKit"

    def tearDown(self):
        shutil.rmtree(self.tmp, ignore_errors=True)

    def convert(self, *extra):
        return run("--src", self.src, "--dst", self.dst, *extra)

    def test_layout_and_rewrites(self):
        code, out, err = self.convert("--quiet")
        self.assertEqual(code, 0, err)
        d = self.dst
        self.assertFalse((d / "package.json").exists())
        self.assertFalse((d / "package.json.meta").exists())
        self.assertFalse((d / ".DS_Store").exists())
        self.assertFalse((d / "Tools~/bin").exists(), "build output is not kit content")
        self.assertTrue((d / "Samples~/S/S.cs").exists())
        self.assertTrue((d / ".gitignore").exists())
        self.assertEqual((d / "Runtime/A.cs.meta").read_text(), "guid: a\n", "meta files are copied byte for byte")
        self.assertEqual((d / "Runtime.meta").read_text(), "guid: runtime\n")
        version = (d / "VERSION").read_text().splitlines()
        self.assertEqual(version[0], "1.2.3")
        self.assertIn("requires: com.unity.modules.ui 1.0.0", version)
        self.assertIn("sample: S | Samples~/S | sd", version)
        self.assertEqual((d / "VERSION.meta").read_text(), "guid: version\n")
        text = (d / "Tests/Editor/Components/AssetDatabaseAssetResolverTests.cs").read_text()
        self.assertIn('KitAssetRoot = "Assets/FutureDigitalPlus/UIKit"; // CLASSIC-REWRITE:KitAssetRoot', text)
        readme = (d / "README.md").read_text()
        self.assertTrue(readme.startswith("# Kit\n\n> **Classic copy.**"))
        self.assertTrue(readme.endswith("Body\n"))
        # folder metas are written for a .../FutureDigitalPlus/UIKit destination
        self.assertEqual((d.parent / "UIKit.meta").read_text(), "guid: uikit\nfolderAsset: yes\n")
        self.assertEqual((d.parent.parent / "FutureDigitalPlus.meta").read_text(), "guid: fdp\nfolderAsset: yes\n")
        manifest = json.loads((d / mck.MANIFEST).read_text())
        self.assertEqual(manifest["kitVersion"], "1.2.3")
        self.assertEqual(manifest["assetRoot"], "Assets/FutureDigitalPlus/UIKit")
        self.assertIn("Runtime/A.cs", manifest["files"])
        self.assertNotIn(mck.MANIFEST, manifest["files"])

    def test_deterministic_and_idempotent(self):
        self.assertEqual(self.convert("--quiet")[0], 0)
        first = snapshot(self.dst)
        self.assertEqual(self.convert("--quiet")[0], 0)
        self.assertEqual(snapshot(self.dst), first, "a second run changes nothing, not even a modification time")
        other = self.tmp / "Other" / "Assets" / "FutureDigitalPlus" / "UIKit"
        self.assertEqual(run("--src", self.src, "--dst", other, "--quiet")[0], 0)
        a = {k: v[0] for k, v in first.items()}
        b = {k: v[0] for k, v in snapshot(other).items()}
        self.assertEqual(a, b, "the same source gives the same bytes in another project")

    def test_check_mode(self):
        self.assertEqual(self.convert("--check")[0], 3, "nothing there yet")
        self.assertEqual(self.convert("--quiet")[0], 0)
        self.assertEqual(self.convert("--check")[0], 0)
        (self.src / "Runtime/A.cs").write_text("class A { int x; }\n")
        self.assertEqual(self.convert("--check")[0], 3, "the source changed")
        self.assertEqual((self.dst / "Runtime/A.cs").read_text(), "class A {}\n", "--check writes nothing")

    def test_refuses_local_changes_unless_forced(self):
        self.assertEqual(self.convert("--quiet")[0], 0)
        (self.dst / "Runtime/A.cs").write_text("local edit\n")
        (self.dst / "Runtime/New.cs").write_text("added\n")
        (self.dst / "README.md.meta").unlink()
        code, out, err = self.convert()
        self.assertEqual(code, 2)
        for needle in ("modified: Runtime/A.cs", "added:    Runtime/New.cs", "deleted:  README.md.meta"):
            self.assertIn(needle, err)
        self.assertEqual((self.dst / "Runtime/A.cs").read_text(), "local edit\n", "a refused run writes nothing")
        self.assertEqual(self.convert("--check")[0], 3)
        self.assertEqual(self.convert("--force", "--quiet")[0], 0)
        self.assertEqual((self.dst / "Runtime/A.cs").read_text(), "class A {}\n")
        self.assertFalse((self.dst / "Runtime/New.cs").exists(), "--force removes files that are not part of the kit")
        self.assertTrue((self.dst / "README.md.meta").exists())
        self.assertEqual(self.convert("--check")[0], 0)

    def test_refuses_a_foreign_non_empty_destination(self):
        self.dst.mkdir(parents=True)
        (self.dst / "mine.txt").write_text("x")
        self.assertEqual(self.convert()[0], 2)
        self.assertEqual(self.convert("--force", "--quiet")[0], 0)
        self.assertFalse((self.dst / "mine.txt").exists())

    def test_update_mirrors_the_source(self):
        self.assertEqual(self.convert("--quiet")[0], 0)
        (self.src / "Runtime/A.cs").unlink()
        (self.src / "Runtime/A.cs.meta").unlink()
        (self.src / "Runtime/B.cs").write_text("class B {}\n")
        pj = json.loads((self.src / "package.json").read_text())
        pj["version"] = "1.2.4"
        (self.src / "package.json").write_text(json.dumps(pj))
        self.assertEqual(self.convert("--quiet")[0], 0)
        self.assertFalse((self.dst / "Runtime/A.cs").exists())
        self.assertFalse((self.dst / "Runtime/A.cs.meta").exists())
        self.assertTrue((self.dst / "Runtime/B.cs").exists())
        self.assertEqual((self.dst / "VERSION").read_text().splitlines()[0], "1.2.4")

    def test_dry_run_writes_nothing(self):
        code, out, err = self.convert("--dry-run")
        self.assertEqual(code, 0)
        self.assertFalse(self.dst.exists())

    def test_exclude(self):
        self.assertEqual(self.convert("--quiet", "--exclude", "Tests", "--exclude", "Samples~")[0], 0)
        self.assertFalse((self.dst / "Tests").exists())
        self.assertFalse((self.dst / "Tests.meta").exists())
        self.assertFalse((self.dst / "Samples~").exists())
        self.assertTrue((self.dst / "Runtime/A.cs").exists())
        self.assertEqual(self.convert("--check", "--exclude", "Tests", "--exclude", "Samples~")[0], 0)
        self.assertEqual(self.convert("--quiet", "--exclude", "Runtime")[0], 1, "Runtime cannot be excluded")

    def test_explicit_asset_root(self):
        dst = self.tmp / "elsewhere"
        self.assertEqual(run("--src", self.src, "--dst", dst, "--quiet")[0], 1, "no Assets segment and no --asset-root")
        self.assertEqual(run("--src", self.src, "--dst", dst, "--asset-root", "Assets/Vendor/Kit", "--quiet")[0], 0)
        self.assertIn('"Assets/Vendor/Kit"', (dst / "Tests/Editor/Components/AssetDatabaseAssetResolverTests.cs").read_text())

    def test_source_drift_is_an_error(self):
        p = self.src / "Tests/Editor/Components/AssetDatabaseAssetResolverTests.cs"
        p.write_text("class NoConstantAnymore {}\n")
        code, out, err = self.convert("--quiet")
        self.assertEqual(code, 1)
        self.assertIn("drifted", err)
        self.assertFalse(self.dst.exists())

    def test_not_a_kit(self):
        shutil.rmtree(self.src / "Tools~")
        self.assertEqual(self.convert("--quiet")[0], 1, "the .meta templates are required")
        (self.src / "package.json").unlink()
        code, out, err = self.convert("--quiet")
        self.assertEqual(code, 1)
        self.assertIn("package.json", err)

    def test_dst_inside_src_is_refused(self):
        code, out, err = run("--src", self.src, "--dst", self.src / "Assets" / "x", "--quiet")
        self.assertEqual(code, 1)


if __name__ == "__main__":
    unittest.main()
