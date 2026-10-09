# Releasing the kit

How the kit is published from its own repository, how a game installs a version and how a game updates. How to change the kit and run its checks is in [CONTRIBUTING.md](CONTRIBUTING.md).

## Open items

1. **Package licence: owner decision, not made.** The code and guides written by Future Digital Plus have no licence yet (`package.json` `license` says "TODO (owner to decide)", README "Licence"). No `LICENSE` file is in the repository on purpose: do not add one, and do not invent a text, until the owner decides. The bundled upstream components keep their own licences (`THIRD_PARTY_NOTICES.md`, the licence file in each upstream module folder). Decide this before the repository is shared outside the company or a version is announced as released.

## The repository

- Remote: `git@github.com:futuredigitalplus/Unity-Ui-Kit.git` (SSH) or `https://github.com/futuredigitalplus/Unity-Ui-Kit.git` (HTTPS). Default branch `main`.
- **The repository root is the package root** (`package.json` is at the top), so a Unity git URL needs no `?path=` suffix.
- Versions are tags `vX.Y.Z` that equal `package.json` `version` (the first one is `v0.2.2`). The version lives in `package.json` only.
- What is committed: every file of the package including all `.meta` files, the four Roslyn DLLs (and their PDBs) and the NSubstitute DLLs of the Components tests. `.gitignore` and `.gitattributes` keep it that way: they do not ignore `.meta`, `~` folders or DLLs, they keep binaries binary, and they keep the ten CRLF files that are verbatim upstream stylesheets (`Samples~/Extras/UpstreamMiscStylesheets`) byte for byte.

## Make a release

1. Make the change and run the checks (CONTRIBUTING.md, "Change the kit"), including the Unity runs that match the change.
2. Set `version` in `package.json`. Add a dated `## [X.Y.Z] - YYYY-MM-DD` section to `CHANGELOG.md`. Update `THIRD_PARTY_NOTICES.md` and the `SOURCE.md` of a module only if an upstream part changed ([Upgrading.md](Upgrading.md)).
3. Commit (`.meta` files included) and push `main`.
4. Tag and push the tag:

   ```sh
   git tag -a vX.Y.Z -m "X.Y.Z"
   git push origin vX.Y.Z
   ```
5. Check the tag as a consumer would (a scratch project, never a game): add the URL below to its `Packages/manifest.json`, open it in Unity, and confirm the package resolves and compiles with no warning from the kit.

A tag is never moved or deleted once a game has used it: ship a new patch version instead.

## Install a version in a game (UPM)

In the game's `Packages/manifest.json`, `dependencies`. Always pin a tag or a commit for a game that ships.

```json
"com.futuredigitalplus.uikit": "https://github.com/futuredigitalplus/Unity-Ui-Kit.git#v0.2.2"
```

```json
"com.futuredigitalplus.uikit": "git@github.com:futuredigitalplus/Unity-Ui-Kit.git#v0.2.2"
```

The first line is the HTTPS form, the second the SSH form (needs an SSH key that can read the repository; `ssh://git@github.com/futuredigitalplus/Unity-Ui-Kit.git#v0.2.2` is the long form of the same). To follow the branch while developing, use `#main` in place of `#v0.2.2`:

```json
"com.futuredigitalplus.uikit": "https://github.com/futuredigitalplus/Unity-Ui-Kit.git#main"
```

Notes. Unity needs a git client on the PATH to resolve a git dependency. Unity records the commit it resolved in `Packages/packages-lock.json`, so commit that file; a `#main` install does not move until the package is updated in the Package Manager (or its entry in the lock file is removed). These Unity behaviours come from the Unity manual and were not run from this repository, which is still unpublished; step 5 above is the check. A game that must build from its own repository alone should use the classic copy (below) instead of a git dependency. The kit needs only `com.unity.modules.unitywebrequest` and `com.unity.modules.vectorgraphics`, which are built in and come with the package.

For work on the kit and a game side by side, a local clone also works: `"com.futuredigitalplus.uikit": "file:../../Unity-Ui-Kit"` (a path relative to the game's `Packages` folder, never an absolute one in a committed manifest).

## The classic copy (a plain folder in the game)

For a game that builds from its own repository alone (no kit repository, no git host in CI), the kit is generated into `Assets/FutureDigitalPlus/UIKit` and committed in the game. The game has no `manifest.json` entry for the kit and no `testables`.

```sh
git clone git@github.com:futuredigitalplus/Unity-Ui-Kit.git <somewhere outside the game>
git -C <that clone> checkout v0.2.2
python3 <that clone>/Tools~/make_classic_kit.py --src <that clone> --dst <game>/Assets/FutureDigitalPlus/UIKit
```

The script keeps every `.meta` (so GUIDs match the UPM package), writes `VERSION` and `.fdp-classic-manifest.json`, refuses a copy with local changes unless `--force`, and removes files that left the kit. It copies everything of the repository except `package.json`, so `.gitignore` and `.gitattributes` land in the copy too (Unity ignores dot files). The game needs the two built-in modules in its own manifest. `python3 ... --check` (exit 3 when the copy differs) is for CI. Full description: [Upgrading.md](Upgrading.md) "Updating a classic copy" and [GettingStarted.md](GettingStarted.md) section 1b.

## How a game updates

- **UPM, git dependency:** change `#vOLD` to `#vNEW` in `Packages/manifest.json`, open Unity (it resolves, imports and updates `packages-lock.json`), read the kit's `CHANGELOG.md` between the two versions, run the game's tests, commit `manifest.json` and `packages-lock.json`.
- **UPM, local path:** `git pull` (or check out the tag) in the kit clone; Unity reimports.
- **Classic copy:** check out the new tag in the kit clone, run `make_classic_kit.py` again as above, look at the game's `git status` (only changed kit files, their `.meta` files, `VERSION` and the manifest should show; a changed GUID in a `.meta` is a bug in the kit), run the game's tests, commit the folder.
- In a game that has PlayMode tests of its own, run the kit's PlayMode tests and the game's in separate runs ([Migration.md](Migration.md) section 10.7).

## Where this repository lives inside another repository

The kit repository was first a folder (`UnityPackages/com.futuredigitalplus.uikit`) of the Solitaire game repository. A git repository inside another repository's working tree needs a decision, and the kit repository does not make it for you:

- **Now: the game repository leaves `UnityPackages` out and the game uses the classic copy.** The kit's history is in the kit repository; the game commits the generated `Assets/FutureDigitalPlus/UIKit` (that is what the classic copy is for). In the game repository: stop tracking the kit folder without deleting it (`git rm -r --cached UnityPackages`; this changes only the game repository's index) and add `/UnityPackages/` to its `.gitignore`. Do not run `git add UnityPackages` in the game repository: git would record the nested repository as a gitlink ("embedded repository" warning) that points at no URL. A clone of the game repository then has no `UnityPackages` folder; the kit is cloned next to it (or anywhere) when someone needs to update the classic copy.
- **Later, if a game should track an exact kit commit: a submodule.** After the kit repository has its first push, move the folder away, remove it from the game repository's index, then `git submodule add git@github.com:futuredigitalplus/Unity-Ui-Kit.git UnityPackages/com.futuredigitalplus.uikit` and check out a tag inside it (`git -C UnityPackages/com.futuredigitalplus.uikit checkout v0.2.2`), then commit `.gitmodules` and the new gitlink in the game repository. The same path keeps every command in the older guides working. Clones need `git clone --recurse-submodules` (or `git submodule update --init`). Updating the kit in the game is then "check out a tag in the submodule, commit the changed gitlink".
- Either way, never copy the kit folder (with its `.git`) into a game by hand to update it: use the manifest line, the classic converter or the submodule.

## First publication (one time)

The repository is initialised locally with `origin` set and nothing committed. The first commit, tag and push are made by the owner (git uses the global `user.name` and `user.email`; check them first):

```sh
cd <path>/UnityPackages/com.futuredigitalplus.uikit
git config user.name && git config user.email
git status --short | head                      # everything is untracked; nothing under Library, Temp or .DS_Store
git add .
git commit -m "FDP UI Kit 0.2.2"
git tag -a v0.2.2 -m "0.2.2"
git push -u origin main
git push origin v0.2.2
```

Then do step 5 of "Make a release", and delete this section from the guide once it has been done.
