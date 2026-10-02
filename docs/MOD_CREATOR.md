# Orbital Forge — Stationeers Mod Creator

## Windows quick start

Extract the entire Windows ZIP, then run `Stationeers.ModForge.exe`. Keep its DLLs and `Content` folder beside the executable. The package includes its .NET runtime and bundled catalog/artwork; Python, UnityPy and a game installation are not required to open the editor.

1. Choose a machine in **Recipe laboratory**, search its products and select a game-art card.
2. Edit materials, time, energy or available furnace conditions in the inspector. **Stage recipe override** adds the changes to your project. **Restore source values** removes that recipe's changes.
3. In **Prefab attributes**, select an item and edit its supported numeric properties. Stack quantity is available for stackable items. Stage the item changes.
4. In **Asset library**, pick existing game artwork for your mod preview.
5. In **Mod workspace**, review the original → override values and enter a name, author, numeric version and description.
6. **Save project** writes an editable `.modforge.json` file. **Export mod** creates a separate ZIP containing native GameData XML, metadata, preview and an editable project copy.
7. Extract the exported mod folder into `%USERPROFILE%\Documents\My Games\Stationeers\Mods`. Enable it in Stationeers, set the appropriate load order and verify active values in game.

Ctrl+S saves, Ctrl+O opens and Ctrl+E exports. Unstaged edits block save/export so changes cannot silently disappear. Save before opening or creating another project. Closing an unsaved project requires an explicit discard decision.

**Appearance** offers Cyan, Amber, Violet and Rose accents, text size, compact cards and higher contrast. Settings persist per Windows user.

Only the catalog's supported native fields can be edited. Furnace mixtures and other unchanged nested recipe data are preserved. The tool does not modify the input game files or install mods. Exported XML and UI behavior were checked automatically; this build has not been launched in Windows or validated inside the running game.

## Build from source

Install the .NET 10 SDK. A supplied source ZIP contains `Content`; a Git checkout requires preparation from the included game snapshot first. Run from the repository root:

```bash
python tools/scan_game.py --game-root . --output analysis
python -m venv .venv
# On Windows substitute .venv/Scripts/python.exe for .venv/bin/python.
.venv/bin/python -m pip install -r tools/requirements-assets-lock.txt
.venv/bin/python tools/extract_prefabs.py
.venv/bin/python tools/prepare_mod_creator_data.py

dotnet build StationeersModForge.sln -c Release
dotnet run --project src/StationeersModCreator.Desktop -c Release
```

UnityPy installation reference: https://github.com/K0lb3/UnityPy#installation . The lock file pins UnityPy 1.25.3 and TypeTreeGeneratorAPI 0.0.10. Asset generation exports actual sprite/texture pixels, including external asset-file references. Its manifest reports every extraction failure (zero in this snapshot).

## Checks and packaging

```bash
dotnet run --project tests/StationeersModCreator.Checks -c Release -- src/StationeersModCreator.Desktop/Content
dotnet src/StationeersModCreator.Desktop/bin/Release/net10.0/Stationeers.ModForge.dll --ui-checks
dotnet src/StationeersModCreator.Desktop/bin/Release/net10.0/Stationeers.ModForge.dll --preview preview.png Recipes
dotnet publish src/StationeersModCreator.Desktop -c Release -r win-x64 --self-contained true -o releases/StationeersModForge-Windows-x64
```

Nine core integration checks cover native XML/ZIP shape, metadata escaping, source preservation, per-machine recipes, inherited stackable fields, transactional staging, malformed projects/values, furnace ranges, persistence and real artwork. Five rendered UI checks exercise actual bound controls and commands for search, edits, staging, save/open, export and settings. The Windows bundle is cross-published; the rendering checks run on Linux using Avalonia headless with real Skia drawing.
