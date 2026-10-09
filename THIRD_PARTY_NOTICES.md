# Third-party notices

This package bundles code and assets from four open-source projects. Each is used under its own permissive
licence, reproduced in full below. The upstream copyright notices are also kept in the source files and in a
licence file inside each module folder. Code, tests, samples and documentation written for this package by
Future Digital Plus are new files; the package's own licence is still to be chosen by the owner (see README).

The upstream licences are kept intact (this file, the licence file inside each module folder, the `SOURCE.md` of each module).
The module code is not byte-identical to upstream any more: `Tools~/rename/rename.py` renamed namespaces, assembly names,
Resources paths and USS names so they cannot collide with a game's, and four binaries were rebuilt (section 2). Every changed
C# file starts with a `FutureDigitalPlus:` comment, and every module's `SOURCE.md` ends with a "Rename record".

| # | Module | Upstream | Licence | Commit |
|---|---|---|---|---|
| 1 | DesignSystem | https://github.com/sinanata/unity-ui-toolkit-design-system | MIT | `65f621155ba757c181829b406f65786c85199554` |
| 2 | Components | https://github.com/jonisavo/uicomponents | MIT | `dc64355910f52fd42924b9af44dca9f20340dfd9` |
| 3 | Animation | https://github.com/instance-id/ElementAnimationToolkit | MIT | `a5d518427e8c7fa7a30e3345d62107356e26e046` |
| 4 | Extras | https://github.com/plyoung/UIElements | Unlicense (public domain dedication) | `8fac81337c5929010c84d8038160fe3406b7137c` |

Per-module detail (what was copied, excluded and modified) is in each module's `Runtime/<Module>/SOURCE.md`.

**Code written for this package (no third-party source).** Foundation, Interaction, Language, Hud, Controls, Gameplay, the UIKit facade, the tests, the tools and the samples are new files. Inside the
upstream modules these files are also ours, with no upstream header: DesignSystem `Theme/Data/ThemeData.Runtime.cs`, `ThemeSheetPatcher.cs`, `ThemeVariants.cs`, `Theme/Applier/RuntimeThemeSheets.cs`, `ThemeManager.cs`,
`Theme/Palette/*.cs`, `Typography/DsOsFonts.cs`, `DsOsFontOrder.cs`, `Editor/Theme/ThemeBuildCheck.cs`, `Styles/Lists.uss`, `Styles/Theming.uss` and the four theme assets `NeutralDark`, `NeutralLight`, `HighContrastDark`,
`HighContrastLight`; Animation `Animations/EatkMotion.cs`; Components `Core/EventInterfaces/IOn{NavigationSubmit,NavigationCancel,FocusIn,FocusOut,PointerDown,PointerUp,PointerMove,KeyDown}.cs`; Extras `BlurOrGlassEffect/BlurSource.cs` and
`Shaders/Kawase.shader`. Changes to upstream-derived files are listed in each module's `SOURCE.md` and marked `FutureDigitalPlus:` in the file.
Licence review of 2026-10-06: every text file of the package (C#, USS, UXML, shaders, asmdef, scripts) was compared, by 10-token shingles, with every text file of the four upstream clones at the commits above. Files we wrote
share nothing with the clones beyond one-line boilerplate (asmdef templates, `[assembly: InternalsVisibleTo]`, UXML headers); no licence, copyright or repository text was found in them; and no font, icon, texture or binary was added
for them: the samples draw with the 120 design system icons covered by section 1 and with procedural shapes. Foundation, Language and the font-order code were modelled on, and partly carried over from, the card game's own UI foundation (`ColorSolitaire.UI`, Future
Digital Plus' own code, which carries no third-party text). The NeutralDark and NeutralLight themes use colour values of the Tailwind CSS default palette (greys and most accents); colour values are facts, no Tailwind code or CSS is included.
If a file is later copied from another project, add it here with its licence before it is merged.

---

## 1. DesignSystem: unity-ui-toolkit-design-system

- Upstream: https://github.com/sinanata/unity-ui-toolkit-design-system
- Commit: `65f621155ba757c181829b406f65786c85199554`
- Licence: MIT
- Copyright: (c) 2026 Sinan Ata / Leap of Legends
- Package folders from this upstream: `Runtime/DesignSystem/` (including `Resources/`), `Editor/DesignSystem/`,
  `Documentation~/DesignSystem/` (the six upstream guides) and `Samples~/DesignSystemShowcase/` (its UXML page is an
  excerpt of the upstream showcase; its C# bootstrap is new code). `Tests/Runtime/DesignSystem/` and
  `Tests/Editor/DesignSystem/` are new tests written for this package, not upstream files.
- Licence file kept in the module: `Runtime/DesignSystem/LICENSE.md`
- Icons: the 120 SVGs carry no per-file header, metadata or attribution (checked), and the upstream repository has no
  separate icon licence or third-party icon list. Their MIT status rests on the author's own statement in the upstream
  README ("released under the same MIT licence"). The package cannot verify where the artwork originally came from.
- Note: the upstream README states that its 120 SVG icons are released under the same MIT licence. Verified for this
  package: the third-party IDE-theme palettes (Codigrate) and the Noto fonts of the upstream showcase are NOT
  bundled, and no other font files are. The two upstream themes (Dark, Light) were generated from hex values in the
  upstream `ThemePresets.cs` and have since been changed for contrast; the other four themes are ours (see above). The Google Fonts importer and runtime fetcher are bundled as code only: they download
  fonts from google/fonts on request, and those fonts carry their own licences (mostly SIL OFL).
  `Runtime/DesignSystem/SOURCE.md` has the full exclusion list.

### Licence text

```
MIT License

Copyright (c) 2026 Sinan Ata / Leap of Legends (https://leapoflegends.com)

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.

```

---

## 2. Components: UIComponents

- Upstream: https://github.com/jonisavo/uicomponents
- Commit: `dc64355910f52fd42924b9af44dca9f20340dfd9`
- Licence: MIT
- Copyright: (c) 2022 Joni Savolainen
- Package folders from this upstream: `Runtime/Components/` (including the rebuilt Roslyn DLLs in
  `Runtime/Components/Core/Roslyn/`), `Editor/Components/`, `Tests/Runtime/Components/`, `Tests/Editor/Components/`,
  `Samples~/Components/` and `Documentation~/Components/` (the upstream README and CHANGELOG, verbatim).
  `Runtime/Components/SOURCE.md` lists what was copied, excluded and modified.
- Licence file kept in the module: `Runtime/Components/LICENSE.md`

### Roslyn generators and analyzers (rebuilt by us from the upstream source)

Upstream ships four prebuilt DLLs (`UIComponents.Roslyn.{Common,Generation,Analyzers,Analyzers.CodeFixes}`). They look every
framework type up by the literal metadata name `UIComponents.<X>`, so in a renamed namespace they would silently generate
nothing. The package therefore ships DLLs **built from upstream's `UIComponents.Roslyn/` source at the pinned commit above**
(MIT, same licence; the source is in the upstream repository) with exactly this patch, applied by `Tools~/rename/rename.py`:

- every string literal that starts with `"UIComponents` and is not `UIComponents.Roslyn...` now starts with `"FutureDigitalPlus.UIKit.Components`
  (21 source files); internal C# namespaces `UIComponents.Roslyn.*` are unchanged;
- one fix (0.2.0), marked `FutureDigitalPlus:` in the source: `AugmentGenerationContext.GetTypeName` returns a type of the framework namespace in full instead of shortening it by the usings of the component's file
  (with `using FutureDigitalPlus.UIKit;` the generated code said `Components.IAssetResolver`, which does not resolve: CS0246). Only `Generation.dll` and its PDB were rebuilt for it, on 2026-10-06, with the same SDK and packages; the other three DLLs are the 2026-10-05 build;
- the four assemblies are named `FutureDigitalPlus.UIKit.Components.Roslyn.{Common,Generation,Analyzers,Analyzers.CodeFixes}`;
- the test-coverage package reference is dropped, and `Microsoft.CodeAnalysis.*` is 4.0.1 instead of upstream's 3.8.0 (3.8.0 is not in the offline
  package cache of the build machine; the DLLs load in Unity 6000.3.25f1's compiler, which is the only version verified).

They contain only the project's own code and reference Microsoft.CodeAnalysis (MIT, supplied by the Unity compiler); no
third-party code is merged into them. They run inside the Unity compiler. Built on 2026-10-05 with .NET SDK 10.0.203,
target `netstandard2.0`, packages `Microsoft.CodeAnalysis.CSharp` 4.0.1, `Microsoft.CodeAnalysis.CSharp.Workspaces` 4.0.1,
`Microsoft.CodeAnalysis.Analyzers` 3.0.0, with:

```
python3 Tools~/rename/rename.py --generator-src <uicomponents clone at dc64355>/UIComponents.Roslyn --generator-out <dir> --roslyn 4.0.1 --nuget-source ~/.nuget/packages
python3 Tools~/rename/rename.py --root <package> --apply --install-generator <dir>/dll
```

A rebuild is not byte-reproducible (the assembly MVID and PDB path differ), so these hashes identify the files shipped in
`Runtime/Components/Core/Roslyn/` (the `.meta` files derive from upstream's, keeping the labels `RoslynAnalyzer` and `SourceGenerator`,
with new deterministic GUIDs):

```
b05d800665fdab4d9d90f698dd9b7a0de6f19680a7c75a8dd4fd4eb75538709a  FutureDigitalPlus.UIKit.Components.Roslyn.Analyzers.CodeFixes.dll
2e8817d9b8fc4b695bde9090cea04afd4aa368ec99cf4a53b06236702cfbb2db  FutureDigitalPlus.UIKit.Components.Roslyn.Analyzers.dll
410c290dc308eb20227bf5a65c47717268d9af810673efe3a781a50f9a344388  FutureDigitalPlus.UIKit.Components.Roslyn.Common.dll
bcd6bc36d417a61dbe5d9acf1e545caf086f73ddff2cc232611ad20086c7cc89  FutureDigitalPlus.UIKit.Components.Roslyn.Generation.dll
```

and their debug symbols:

```
00304aab31b5995ccb91a46588e4d859101b229cde77018e965fe56a6fece868  FutureDigitalPlus.UIKit.Components.Roslyn.Analyzers.CodeFixes.pdb
c3a7c4793ee0e32a36f930393e3f57d91e3917c697d91051f1edb6fdc5c6dc46  FutureDigitalPlus.UIKit.Components.Roslyn.Analyzers.pdb
0851b0ef0e5e74f833385f9493ee49fb2994a21a9aa72811b38aa0e3a953925a  FutureDigitalPlus.UIKit.Components.Roslyn.Common.pdb
5aef776262a720dda223560485eedc265bb1b1f05d5bc7d1f0edf58a9f43fe88  FutureDigitalPlus.UIKit.Components.Roslyn.Generation.pdb
```

For the record, the upstream prebuilt files at the pinned commit (no longer in the package) had these SHA-256 values:

```
98bdc1319828bc97accedf0e63c678af21a71dabce4856253bceaf730764337b  UIComponents.Roslyn.Analyzers.CodeFixes.dll
f7bed6709bef08cd23eafd5e119e1115bf88af723c01731dbf15204a859c1c26  UIComponents.Roslyn.Analyzers.dll
5c9209d00f883556502fd3f0a1319f7d69182062fd44c21ea61c7261c2c5663b  UIComponents.Roslyn.Common.dll
6da490729bf5c25422e24c4a7833e4f7040d375793f671904d30adec77d04824  UIComponents.Roslyn.Generation.dll
```

### Bundled test-only third-party libraries (Components tests)

Upstream UIComponents bundles these four managed DLLs for its own tests; they are kept, with their upstream `.meta`
files and licence files, in `Tests/Runtime/Components/NSubstitute/`. They are test-only: their importers are active
only when `UNITY_INCLUDE_TESTS` is defined (the package is listed under `testables`, or it is installed as the classic Assets folder) and they are explicitly
referenced, so they are never part of a game build. SHA-256 of the files as copied:

```
f5d48f82112370b524c7c47e94cf1855af4a3dbe2c500fac570c75525187db99  nsubstitute.4.3.0/NSubstitute.dll
557e4b582a4b57f0f4830a3ddeedd721c468935f6517eaa7e8cafe2a4a3c7636  castle.core.4.4.1/Castle.Core.dll
01748200f2400c742aa689f1f5101bd6298efdfd92c00c18f4fa473847235ba9  system.runtime.compilerservices.unsafe.6.0.0/System.Runtime.CompilerServices.Unsafe.dll
09901f63ebc4b8f0897b0128fb230c146529aee5c5cc90d7a7872e66a976bb5f  system.threading.tasks.extensions.4.5.4/System.Threading.Tasks.Extensions.dll
```

| Library | Licence | Copyright | Licence file in the package (under `Tests/Runtime/Components/NSubstitute/`) |
|---|---|---|---|
| NSubstitute 4.3.0 | BSD-3-Clause (text below) | (c) 2009 Anthony Egerton and David Tchepak | `nsubstitute.4.3.0/LICENSE.txt` |
| Castle.Core 4.4.1 | Apache License 2.0 | (c) 2004-2020 Castle Project (assembly copyright attribute; the `LICENSE` file shipped with the DLL says 2004-2016) | `castle.core.4.4.1/ASL - Apache Software Foundation License.txt` (full text) and `castle.core.4.4.1/LICENSE` |
| System.Runtime.CompilerServices.Unsafe 6.0.0 | MIT (text below) | (c) .NET Foundation and Contributors | `system.runtime.compilerservices.unsafe.6.0.0/LICENSE.TXT`, `THIRD-PARTY-NOTICES.TXT` |
| System.Threading.Tasks.Extensions 4.5.4 | MIT (text below) | (c) .NET Foundation and Contributors | `system.threading.tasks.extensions.4.5.4/LICENSE.TXT`, `THIRD-PARTY-NOTICES.TXT` |

Castle.Core is licensed under the Apache License, Version 2.0 (http://www.apache.org/licenses/LICENSE-2.0). The full
licence text is the file named in the table. The Castle.Core binary is distributed unmodified.

To verify before a public release: the upstream UIComponents repository ships `NSubstitute.dll` without a licence
file. The BSD-3-Clause text and copyright line for NSubstitute reproduced here and in `LICENSE.txt` were written
from the NSubstitute project's published licence, not copied from a file in the clone; compare them with the
licence inside the NSubstitute 4.3.0 package before publishing.

NSubstitute licence (BSD-3-Clause):

```
Copyright (c) 2009 Anthony Egerton (nsubstitute@delfish.com) and David Tchepak (dave@davesquared.net)
All rights reserved.

Redistribution and use in source and binary forms, with or without modification, are permitted provided
that the following conditions are met:

    * Redistributions of source code must retain the above copyright notice, this list of conditions and
      the following disclaimer.
    * Redistributions in binary form must reproduce the above copyright notice, this list of conditions
      and the following disclaimer in the documentation and/or other materials provided with the
      distribution.
    * Neither the name of the NSubstitute Team nor the names of its contributors may be used to endorse
      or promote products derived from this software without specific prior written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED
WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A
PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR
ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED
TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION)
HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE
POSSIBILITY OF SUCH DAMAGE.
```

System.Runtime.CompilerServices.Unsafe and System.Threading.Tasks.Extensions licence (MIT, .NET Foundation):

```
The MIT License (MIT)

Copyright (c) .NET Foundation and Contributors

All rights reserved.

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
```

### Licence text (UIComponents)

```
MIT License

Copyright (c) 2022 Joni Savolainen <joni@savolainen.io>

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
```

---

## 3. Animation: ElementAnimationToolkit

- Upstream: https://github.com/instance-id/ElementAnimationToolkit
- Commit: `a5d518427e8c7fa7a30e3345d62107356e26e046`
- Licence: MIT
- Copyright: (c) 2020 instance.id
- Package folders from this upstream: `Runtime/Animation/` only (no editor code was kept, so `Editor/Animation/` holds
  nothing). `Tests/Runtime/Animation/`, `Documentation~/Animation/` and `Samples~/Animation/` are new files written for
  this package, not upstream files.
- Licence file kept in the module: `Runtime/Animation/LICENSE.md`
- Note: upstream declares a dependency on `id.instance.extensions` (same author), which is not part of this package. No code
  uses it: the helper methods the toolkit needs are already bundled in the upstream repository under
  `Runtime/Scripts/Extensions` and are kept in the same place, so nothing had to be vendored from another source.
  `Runtime/Animation/SOURCE.md` records this and every dropped feature.
- Provenance notes: (1) `Animations/Easy.cs` was marked upstream "From com.unity.ui" and held decompiled code of Unity's own `Easing` class, which
  the MIT licence of the upstream repository cannot cover. Resolved in the licence review of 2026-10-06: the file now delegates the standard curves to Unity's public
  `UnityEngine.UIElements.Experimental.Easing`; only `EaseInOutQuint` and `OutExpo` (the upstream author's own) are code in the file. (2) `AnimatedFoldout` is modelled on Unity's
  built-in `Foldout` (its upstream `UxmlTraits` block was decompiler output of Foldout's, and is removed in this package; the USS class names and structure still mirror Foldout). (3) Not bundled because their licence is not stated or not compatible with a plain MIT notice: the
  *Source Code Pro* font files (SIL OFL), and `Colors.cs` (a colour-name list copied from a public gist, plus its helpers
  `ColorTool`, `ColorExtensions`, `MathEx`).

### Licence text

```
MIT License

Copyright (c) 2020 instance.id

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.

```

---

## 4. Extras: plyoung/UIElements

- Upstream: https://github.com/plyoung/UIElements
- Commit: `8fac81337c5929010c84d8038160fe3406b7137c`
- Licence: The Unlicense (public domain dedication)
- Copyright: none claimed (dedicated to the public domain by the author)
- Package folders from this upstream: `Runtime/Extras/`, `Editor/Extras/` (empty, no upstream editor code),
  `Documentation~/Extras/` (README excerpts and images), `Samples~/Extras/` (upstream style sheets, UXML sample and
  the legacy URP blur files; `ExtrasThemeVariables.uss` is derived from upstream values, the bridge file is new).
  `Tests/Runtime/Extras/` and `Tests/Editor/Extras/` hold new tests written for this package.
- Licence file kept in the module: `Runtime/Extras/UNLICENSE.md` (upstream file `UNLICENSE`)
- Attribution is not legally required for public-domain material, but it is kept here and in the module for
  provenance.

### Licence text

```
This is free and unencumbered software released into the public domain.

Anyone is free to copy, modify, publish, use, compile, sell, or
distribute this software, either in source code form or as a compiled
binary, for any purpose, commercial or non-commercial, and by any
means.

In jurisdictions that recognize copyright laws, the author or authors
of this software dedicate any and all copyright interest in the
software to the public domain. We make this dedication for the benefit
of the public at large and to the detriment of our heirs and
successors. We intend this dedication to be an overt act of
relinquishment in perpetuity of all present and future rights to this
software under copyright law.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
IN NO EVENT SHALL THE AUTHORS BE LIABLE FOR ANY CLAIM, DAMAGES OR
OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE,
ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
OTHER DEALINGS IN THE SOFTWARE.

For more information, please refer to <http://unlicense.org/>

```

---

## 5. Referenced but not bundled

Nothing below is copied into the package or shipped by it; it is listed so that nobody has to look for it here.

- **Optional Unity packages** (Input System, Unity Localization, Addressables, uGUI and TextMeshPro, URP, HDRP): the package refers to them by assembly name and
  `versionDefines` only, and compiles with none of them installed. Each is installed by the game under Unity's own terms. No Unity package source is included. Unity's public
  APIs are called (for example `UnityEngine.UIElements.Experimental.Easing` from `Animations/Easy.cs`); no Unity source or decompiled text is included.
- **Development tools** (`Tools~/`, not part of a player build): `CompileCheck`, `PureTests` and `LintRunner` use the NuGet packages Microsoft.CodeAnalysis.CSharp 4.0.1 (MIT),
  Microsoft.NET.Test.Sdk (MIT), NUnit 3.14.0 (MIT) and NUnit3TestAdapter (MIT), restored by `dotnet` on the developer's machine; none is checked in. The matrix tool starts the
  Unity editor and installs the Unity packages named in `Tools~/Matrix/variants.json` into scratch projects only.
- **Fonts**: none are bundled. `DsGoogleFonts` and the Google Fonts window download fonts from the `google/fonts` repository only when called; each font keeps its own licence
  (mostly SIL OFL), which the game must reproduce. The OS font fallback (`DsOsFonts`) uses fonts installed on the device, by family name.
- **Package licence**: the licence of this package's own code is still undecided (`package.json` says so). Until the owner chooses one, the package is for Future Digital Plus' own games.
