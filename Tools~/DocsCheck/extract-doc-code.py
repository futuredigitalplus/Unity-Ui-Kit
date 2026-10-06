#!/usr/bin/env python3
"""Turns the code blocks of the documentation into files a Unity scratch project can compile and run.

  extract-doc-code.py <package root> <output folder>

Writes into <output folder>:
  Recipes.generated.cs                    every ```csharp block of Documentation~/Recipes.md, each in its own namespace RecipeN (usings hoisted, `using Recipe0;` added after the first)
  Resources/Screens/Home.uxml             the UXML block of Recipes.md (recipe 1)
  Resources/GS/MainMenu.uxml              the UXML block of Documentation~/GettingStarted.md (step 2)
"""
import os
import re
import sys
import textwrap


def blocks(path, lang):
    with open(path, encoding="utf-8") as f:
        return re.findall(r"```%s\n(.*?)```" % lang, f.read(), re.S)


def main():
    if len(sys.argv) != 3:
        sys.exit(__doc__)
    root, out = sys.argv[1:3]
    docs = os.path.join(root, "Documentation~")
    code = blocks(os.path.join(docs, "Recipes.md"), "csharp")
    if not code:
        sys.exit("no csharp blocks found in Recipes.md")
    parts = ["// Generated from Documentation~/Recipes.md by Tools~/DocsCheck/extract-doc-code.py. Do not edit."]
    for i, block in enumerate(code):
        lines = block.split("\n")
        usings = sorted({l for l in lines if l.startswith("using ") and l.rstrip().endswith(";")})
        body = "\n".join(l for l in lines if l not in usings)
        if i > 0:
            usings.append("using Recipe0;")
        parts.append("namespace Recipe%d\n{\n%s\n%s\n}\n" % (i, "\n".join(usings), body))
    os.makedirs(os.path.join(out, "Resources", "Screens"), exist_ok=True)
    os.makedirs(os.path.join(out, "Resources", "GS"), exist_ok=True)
    with open(os.path.join(out, "Recipes.generated.cs"), "w", encoding="utf-8") as f:
        f.write("\n".join(parts))
    home = blocks(os.path.join(docs, "Recipes.md"), "xml")
    menu = blocks(os.path.join(docs, "GettingStarted.md"), "xml")
    if not home or not menu:
        sys.exit("the UXML blocks of Recipes.md or GettingStarted.md are missing")
    with open(os.path.join(out, "Resources", "Screens", "Home.uxml"), "w", encoding="utf-8") as f:
        f.write(textwrap.dedent(home[0]))
    with open(os.path.join(out, "Resources", "GS", "MainMenu.uxml"), "w", encoding="utf-8") as f:
        f.write(textwrap.dedent(menu[0]))
    print("%d C# blocks, 2 UXML files -> %s" % (len(code), out))


if __name__ == "__main__":
    main()
