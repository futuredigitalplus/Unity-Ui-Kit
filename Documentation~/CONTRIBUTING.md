# Contributing to the kit

The repository root is the package root: `package.json` is at the top, next to `Runtime/`, `Editor/`, `Tests/`, `Samples~/`, `Documentation~/` and `Tools~/`.
This page is under `Documentation~` because a Markdown file at the top level of a Unity package needs a `.meta` file made by Unity, and a hand written one is not allowed.
Releasing (tags, install lines, how a game updates) is in [RELEASING.md](RELEASING.md).

## Rules that are easy to break

- **One source.** This repository is canonical. A "classic" copy in a game (`Assets/FutureDigitalPlus/UIKit`) is generated from it and never edited by hand; port a fix made there back here.
- **`.meta` files are part of the kit.** Commit them; GUIDs must not change. Never write a `.meta` by hand and never regenerate an existing one. A new file gets its `.meta` from Unity: open a scratch project that references the kit by path (never a game), let Unity import, copy the new `.meta` files back. Folders and files inside a `~` folder or starting with a dot have no `.meta` (the lint fails on one).
- **Nothing refers to a game.** No game namespaces, no absolute or home paths, no game repository paths in code or guides. `Tests/Editor/Lint` and `Tools~/CompileCheck` enforce part of this, the scan listed in [Verification.md](Verification.md) ("Static scan of the package folder") checks the rest; repeat it by hand before a release.
- **Do not hard-code the install folder.** Editor code finds the kit with `KitLocation` (the kit is `Packages/com.futuredigitalplus.uikit` as a UPM package and `Assets/FutureDigitalPlus/UIKit` as a classic copy).
- **Upstream code stays diffable.** The four upstream modules (`DesignSystem`, `Components`, `Animation`, `Extras`) change only as `Documentation~/Upgrading.md` describes: a `FutureDigitalPlus:` comment at the change and an entry in the module's `SOURCE.md`.
- **Do not add a licence file or a licence claim** until the owner has decided the package licence (open item 1 in the README).

## Change the kit

1. Edit the source, with its tests (pure logic goes in the `*.Core` assemblies so `Tools~/PureTests` can run it without Unity) and its guide in `Documentation~`.
2. Run the checks that need no Unity, before every commit (the `<temp dir>` for dotnet output is any folder outside the repository):

   ```sh
   Tools~/CompileCheck/run-compile-check.sh --samples                       # Roslyn compile of every assembly and sample, XML-doc and name-hazard checks
   dotnet test Tools~/CompileCheck/LintRunner --artifacts-path <temp dir>   # USS, variable, class, resource and .meta rules
   dotnet test Tools~/PureTests --artifacts-path <temp dir>                 # the pure logic of the Core assemblies
   python3 Tools~/rename/rename.py --root . --verify-only                   # names are still consistent
   python3 Tools~/DocsCheck/check-docs.py                                   # Markdown links, anchors, quoted paths, index
   python3 -B -m unittest discover -s Tools~ -p "test_make_classic_kit.py"  # the classic converter (no Unity)
   ```
3. Run the Unity checks that match the change. They run on a scratch Unity 6000.3 project, never on a game: the full EditMode and PlayMode suites (see "Checks (test project)" in [Upgrading.md](Upgrading.md)), `Tools~/Matrix/run-matrix.sh <variant>` for pipeline, colour space, input or optional-package changes ([Tools~/Matrix/README.md](../Tools~/Matrix/README.md)), `Tools~/SamplesSmoke` after an API a sample uses changes, and `Tools~/DocsCheck/run-docs-check.sh <scratch project> --run` after a change to an API that a guide shows. The result must be 0 compiler errors, 0 warnings and no failed test.
4. Run the generated classic copy check when `Tools~/make_classic_kit.py`, `Tools~/classic/` or anything it rewrites changed (the converter tests above, and `Tools~/Matrix/run-matrix.sh --classic`).
5. Update [CHANGELOG.md](../CHANGELOG.md) (Keep a Changelog sections `Added`, `Changed`, `Fixed`; one dated heading per version) and the guide that describes the behaviour.

## Version bump

The version lives in `package.json` only (the classic copy's `VERSION` file is generated from it). Semantic Versioning, as the rule in [Upgrading.md](Upgrading.md) "Releasing a new package version" says: while the major is 0, an upstream update and a breaking change of the kit's API are minor bumps; from 1.0.0 a breaking change is a major bump. The version, its CHANGELOG entry and the passing checks go in one commit, which is the one that gets the tag. Steps and the tag: [RELEASING.md](RELEASING.md).

## Regenerate the classic copy

Only a game that carries the kit as a plain folder needs this. From a checkout of this repository at the commit or tag to adopt:

```sh
python3 Tools~/make_classic_kit.py --src . --dst <game>/Assets/FutureDigitalPlus/UIKit
```

It is deterministic and idempotent, mirrors removals, keeps every `.meta` byte for byte and refuses (exit 2) a copy with local changes unless `--force`; `--check` exits 3 when the copy differs; `--dry-run` prints the plan. The converter skips `.git`, so the repository can be the `--src` as it is. Details: [Upgrading.md](Upgrading.md) "Updating a classic copy".
