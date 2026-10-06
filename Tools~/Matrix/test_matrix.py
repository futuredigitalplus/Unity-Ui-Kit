"""Tests of matrix.py without Unity: python3 -m unittest Tools~/Matrix/test_matrix.py (or run from this folder: python3 -m unittest)."""
import json, tempfile, unittest
from pathlib import Path

import matrix

EDIT_XML = """<test-run total="3"><test-suite><test-case fullname="A.One" result="Passed"/><test-case fullname="A.Two" result="Passed"/>
<test-case fullname="A.Three" result="Failed"/></test-suite></test-run>"""
PLAY_XML = """<test-run><test-case fullname="P.ThemedPanel_PaintsTheThemeColours(True,&quot;Dark&quot;)" result="Passed"/>
<test-case fullname="P.ThemedPanel_PaintsTheThemeColours(False,&quot;Dark&quot;)" result="Failed"/><test-case fullname="P.Other" result="Skipped"/></test-run>"""
PLAY_LOG = """noise
FDPMATRIX env pipeline=UniversalRenderPipelineAsset renderer=Renderer2D device=Metal colorSpace=Linear unity=6000.3.25f1 platform=OSXEditor
Assets/x.cs(1,1): warning CS0618: obsolete
Assets/x.cs(1,1): warning CS0618: obsolete
Library/y.cs(2,2): error CS0103: nope
"""


class MatrixTests(unittest.TestCase):
    def setUp(self):
        self.tmp = tempfile.TemporaryDirectory()
        self.dir = Path(self.tmp.name)

    def tearDown(self):
        self.tmp.cleanup()

    def test_variants_file_is_consistent(self):
        data = json.loads((matrix.HERE / "variants.json").read_text())
        for name, v in data["variants"].items():
            self.assertIn(v["input"], matrix.INPUT_HANDLER, name)
            self.assertIn(v["colorSpace"], matrix.COLOR_SPACE, name)
            self.assertIn(v["pipeline"], ["builtin", "urp3d", "urp2d", "hdrp"], name)
            if v["pipeline"].startswith("urp"):
                self.assertIn("com.unity.render-pipelines.universal", v["packages"], name)
            if v["pipeline"] == "hdrp":
                self.assertIn("com.unity.render-pipelines.high-definition", v["packages"], name)
            if v["input"] != "legacy":
                self.assertIn("com.unity.inputsystem", v["packages"], name)

    def test_edit_project_setting(self):
        text = "x:\n  m_ActiveColorSpace: 0\n  activeInputHandler: 0\n"
        text = matrix.edit_project_setting(text, "m_ActiveColorSpace", 1)
        self.assertIn("  m_ActiveColorSpace: 1\n", text)
        with self.assertRaises(ValueError):
            matrix.edit_project_setting(text, "missing", 1)

    def test_prepare_rewrites_manifest_settings_and_copies_editor_scripts(self):
        (self.dir / "Packages").mkdir()
        (self.dir / "ProjectSettings").mkdir()
        (self.dir / "Packages/manifest.json").write_text(json.dumps({"dependencies": {"com.unity.modules.ui": "1.0.0", matrix.PACKAGE_ID: "file:/old"}}))
        (self.dir / "ProjectSettings/ProjectSettings.asset").write_text("  m_ActiveColorSpace: 0\n  activeInputHandler: 0\n")

        matrix.prepare(self.dir, "/some/pkg", "hdrp-linear", matrix.HERE / "variants.json")

        manifest = json.loads((self.dir / "Packages/manifest.json").read_text())
        self.assertEqual("file:/some/pkg", manifest["dependencies"][matrix.PACKAGE_ID])
        self.assertIn("com.unity.render-pipelines.high-definition", manifest["dependencies"])
        self.assertEqual([matrix.PACKAGE_ID], manifest["testables"])
        settings = (self.dir / "ProjectSettings/ProjectSettings.asset").read_text()
        self.assertIn("m_ActiveColorSpace: 1", settings)
        self.assertIn("activeInputHandler: 0", settings)
        self.assertTrue((self.dir / "Assets/FdpMatrix/Editor/PlayerExport.cs").exists())
        self.assertTrue((self.dir / "Assets/FdpMatrix/Editor/Hdrp/HdrpSetup.cs").exists())

    def test_input_system_only_sets_the_new_handler(self):
        (self.dir / "Packages").mkdir()
        (self.dir / "ProjectSettings").mkdir()
        (self.dir / "Packages/manifest.json").write_text("{}")
        (self.dir / "ProjectSettings/ProjectSettings.asset").write_text("  m_ActiveColorSpace: 0\n  activeInputHandler: 0\n")
        matrix.prepare(self.dir, "/p", "input-system", matrix.HERE / "variants.json")
        self.assertIn("activeInputHandler: 1", (self.dir / "ProjectSettings/ProjectSettings.asset").read_text())

    def test_summarize_and_table(self):
        (self.dir / "results-edit.xml").write_text(EDIT_XML)
        (self.dir / "results-play.xml").write_text(PLAY_XML)
        (self.dir / "play.log").write_text(PLAY_LOG)
        (self.dir / "pcc.log").write_text("PLAYERCOMPILE iOS: assemblies=23\nPLAYERCOMPILE Android threw: boom\n")
        (self.dir / "export-ios.log").write_text("BUILDRESULT iOS result=Succeeded errors=0 warnings=0 totalSize=1\n")

        s = matrix.summarize(self.dir, "urp2d", matrix.HERE / "variants.json")

        self.assertEqual({"passed": 2, "failed": 1, "skipped": 0, "total": 3}, s["edit"])
        self.assertEqual({"passed": 1, "failed": 1, "skipped": 1, "total": 3}, s["play"])
        self.assertEqual("1/2", s["paint"])
        self.assertEqual("UniversalRenderPipelineAsset / Renderer2D", s["pipeline"])
        self.assertEqual("Linear", s["colorSpace"])
        self.assertEqual(1, s["compileErrors"])
        self.assertEqual(1, s["compileWarnings"], "the same warning twice counts once")
        self.assertEqual({"iOS": "ok", "Android": "FAILED"}, s["playerCompile"])
        self.assertEqual({"ios": "Succeeded (0 err, 0 warn)"}, s["exports"])

        work = self.dir / "work"
        (work / "urp2d").mkdir(parents=True)
        (work / "urp2d/summary.json").write_text(json.dumps(s))
        out = matrix.table(work, "markdown")
        self.assertIn("| urp2d |", out)
        self.assertIn("2/3 (1 FAILED)", out)

    def test_player_compile_reports_a_missing_module_and_the_samples_label(self):
        (self.dir / "pcc.log").write_text("PLAYERCOMPILE iOS: assemblies=17\nPLAYERCOMPILE WebGL skipped: the platform module is not installed\n")
        s = matrix.summarize(self.dir, "base", matrix.HERE / "variants.json", label="base-samples")
        self.assertEqual({"iOS": "ok", "WebGL": "no module"}, s["playerCompile"])
        self.assertEqual("base-samples", s["variant"])

    def test_missing_results_are_reported_as_not_run(self):
        s = matrix.summarize(self.dir, "base", matrix.HERE / "variants.json")
        self.assertIsNone(s["edit"])
        self.assertIn("not run", matrix.format_row(s))

    def test_pipeline_method(self):
        self.assertEqual("FdpMatrix.UrpSetup.Use2D", matrix.PIPELINE_METHOD["urp2d"])


if __name__ == "__main__":
    unittest.main()
