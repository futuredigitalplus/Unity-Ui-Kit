# Tools~/DocsCheck: do the guides tell the truth?

A new engineer follows the guides literally. This folder makes the guides pass that test on their own.

| Check | How | Fails when |
|---|---|---|
| Links, anchors, quoted paths, index | `check-docs.py` (Python 3, no Unity) | a relative Markdown link or `#anchor` is broken, a path in backticks (`Runtime/...`, `Samples~/...`, `Tools~/...`) does not exist, a guide under `Documentation~` is not linked from `index.md` |
| Recipes compile and run | `extract-doc-code.py` writes every C# block of `Documentation~/Recipes.md` as `Recipes.generated.cs` (each recipe in its own namespace `RecipeN`), plus the UXML of recipe 1 and of GettingStarted step 2; `Unity/Tests/RecipeTests.cs` then builds the bootstrap (recipe 0) on a real panel and runs recipes 1 to 9 against it, loads the GettingStarted UXML with `UIKitHost` and measures the layout, and builds `Unity/Resources/DocsUxml.uxml` (the UXML snippets of the Hud, Controls, Language and Extras guides) and reads the attributes back | a block no longer compiles, or a recipe logs an error, or the result differs from what the recipe says (classes on the root, `Hello` in the title, 24 slots, the language switch, the brand theme, the panel scale) |
| Module guide snippets compile | `Unity/ModuleGuideSnippets.cs`, `FacadeThemingAnimationExtrasSnippets.cs`, `ComponentsSnippets.cs` are the code blocks of the module guides, copied verbatim (usings hoisted) | a guide names an API that changed |

```
Tools~/DocsCheck/check-docs.py                                  # static checks only, a second
Tools~/DocsCheck/run-docs-check.sh <scratch project>            # static checks, then copies the Unity files into <project>/Assets/UIKitDocsCheck
Tools~/DocsCheck/run-docs-check.sh <scratch project> --run      # ...and runs them in PlayMode, then deletes the copy (exit code 0 = everything passed)
```

Use a scratch project that references the package (never a game); the optional packages do not matter, the file `Unity/Resources/DocsCheckRuntimeTheme.tss` gives the test panels the runtime theme every `PanelSettings` needs.
`UNITY` overrides the editor path. A run takes about two minutes. The files in `Unity/` are not part of any package assembly (`Tools~` is hidden from Unity).

When you change a recipe, run it: a block that does not compile fails the assembly, and the test of the same name tells which behaviour changed. When a module guide's snippet changes, change the copy in `Unity/` too:
the compile error of the copy is the reminder that the guide and the code have drifted.

Found by this check so far: a file that has `using FutureDigitalPlus.UIKit;` and declares a `UIComponent` class did not compile (the Components generator wrote names relative to the file's usings); fixed in the rebuilt generator (0.2.0).

Not covered: how a page looks (no screenshot), input from a real device, platforms other than the editor's.
