# Stationeers game analysis — 2026-10-02

This report describes the uploaded snapshot, not a live Steam installation. Original game files remain unchanged.

## Verified snapshot

| Measure | Count |
|---|---:|
| Game files | 1,078 |
| Source bytes | 7,408,006,892 |
| Managed DLL files (including framework/vendor code) | 158 |
| All metadata types | 34,469 |
| All metadata methods | 305,927 |
| All metadata fields | 177,350 |
| Assembly-CSharp types | 5,127 |
| Assembly-CSharp methods | 37,193 |
| Assembly-CSharp fields | 31,664 |
| XML documents across game files | 175 |
| Core Data XML files | 43 |
| Recipe records | 621 |
| Serialized asset files | 9 |
| Serialized objects | 153,161 |
| Decoded Thing components | 1,574 |
| Distinct decoded prefab names | 1,569 |

Unity version: **2022.3.62f3**, confirmed by all nine serialized-file headers. Backend: **Mono**, supported by the Managed assemblies and MonoBleedingEdge installation. The uploaded BepInEx log reports version **5.4.23.3** and Windows x64. Assembly PE flags are metadata evidence; an individual AnyCPU DLL reporting PE32 does not make the game 32-bit.

## Recipe coverage

| XML section | Records |
|---|---:|
| AdvancedFurnaceRecipes | 10 |
| ArcFurnaceRecipes | 8 |
| AutolatheRecipes | 70 |
| AutomatedOvenRecipes | 19 |
| CentrifugeRecipes | 12 |
| ChemistryRecipes | 3 |
| ElectronicsPrinterRecipes | 130 |
| FurnaceRecipes | 20 |
| HydraulicPipeBenderRecipes | 130 |
| IngotRecipes | 17 |
| MicrowaveRecipes | 19 |
| PackagingMachineRecipes | 9 |
| RocketManufactoryRecipes | 36 |
| SecurityPrinterRecipes | 15 |
| TerraformingManufactoryRecipes | 2 |
| ToolManufactoryRecipes | 121 |

These are recipe definitions, including IngotRecipes and different machine sections. They are not all unique items. Furnace pressure/temperature/required-mixture structures, attributes and child order are retained in extracted records. Numeric units and processing semantics have not been verified by decompiling the game.

All 621 recipe records link by exact PrefabName to at least one decoded Thing component. The 1,569 distinct prefab names include structures, items, effects and other Thing subclasses; they are not all machines or player-craftable items.

## Asset coverage

| Asset | Objects |
|---|---:|
| rocketstation_Data/globalgamemanagers | 18 |
| rocketstation_Data/globalgamemanagers.assets | 3,520 |
| rocketstation_Data/level0 | 32 |
| rocketstation_Data/level1 | 23,344 |
| rocketstation_Data/level2 | 1,391 |
| rocketstation_Data/resources.assets | 113,264 |
| rocketstation_Data/sharedassets0.assets | 20 |
| rocketstation_Data/sharedassets1.assets | 11,331 |
| rocketstation_Data/sharedassets2.assets | 241 |

UnityPy 1.25.3 independently agrees with every asset object ID set, class count and GameObject/MonoScript name decoded by the scanner. This is a serialization-index consistency check, not in-game verification.

## Concrete autolathe evidence

`resources.assets` contains multiple GameObjects named StructureAutolathe; selecting solely by name is ambiguous. One GameObject at path ID **40323** refers to the MonoBehaviour at path ID **107170**. With TypeTreeGeneratorAPI 0.0.10, that component decoded into **165** top-level fields. Its values include:

| Field | Serialized value |
|---|---|
| PrefabName | StructureAutolathe |
| PrefabHash | 336213101 |
| UsedPower | 100.0 |
| HasPowerState | 1 |

Fields also include Slots, BuildStates, BrokenBuildStates, UpgradePrefab, ImportChute, ExportChute, PrintingEffect, QuickFabricate and QuickFabTime. These are source defaults, not measurements of a running machine.

Autolathe ItemIronFrames recipe: Iron **4**, Time **4**, Energy **200**. The same prefab also appears in TerraformingManufactoryRecipes. The tool preserves both records and requires section/source identity for edits.

## Mod and runtime evidence

The supplied ExampleMod and AttributesExampleMod ZIPs provide the native folder structure and examples. Their readmes identify local mod installation and load-order behavior. The recipe export round-trip produced one override, parsed its About.xml and recipe XML, and verified the original recipe file hash stayed unchanged. It was not enabled or tested in game.

Observed metadata candidates:

- `ModConfig`: `GetEnabledMods`, `MoveModUp`, `MoveModDown`.
- `ModData`: `GameDataFolder`, `GetAboutData`.
- `Assets.Scripts.Objects.Prefab`: `LoadAll`, `LoadCorePrefabs`, `Register`, `Find`, `TryFind`.
- `Assets.Scripts.Objects.Thing`: `InteractWith`, `SerializeSave`, `DeserializeSave`, `Serialize`, `BuildUpdate`, `ProcessUpdate`, `get_HasAuthority`.
- `Assets.Scripts.Objects.Electrical.SimpleFabricatorBase`: `get_Recipes`, `SetRecipeFromHash`, `SetRecipeHashSafe`, `GetLogicValue`, `SetLogicValue`, `SpawnCreatedItems`.

These method names are candidates; metadata alone does not establish exact hook behavior. No IL decompilation, runtime injection or game execution has been completed.

## Remaining limits

- Four MonoBehaviours in resources.assets have null script references: 106284, 108380, 108811 and 111644. They cannot be resolved to a class.
- Book component 108952 fails strict layout validation: 2,324 serialized bytes versus 2,216 decoded bytes. It remains unresolved, with its source identity and object range retained. The tool does not accept a partial layout as complete.
- Thing-derived types are followed through direct TypeDef/TypeRef inheritance. Generic TypeSpec inheritance needs signature decoding; components outside that known family are not normalized as Thing records.
- Assembly member signatures are stored as raw metadata blobs; properties/events/constants and decompiled bodies remain future work.
- Asset binary patching, external PPtr normalization, live runtime inspection, the desktop UI and IL2CPP support are not implemented.

## Reproducible outputs

See USAGE.md for extraction/search/export commands. JSON and SQLite outputs are generated locally; the smaller summaries and validation reports are kept in Git. The shipped game XML and binary files remain the original source authority.

## Provenance

| Analyzed source | SHA-256 |
|---|---|
| Assembly-CSharp.dll | 6caeb8414a83cf47bdc86e0978b0b011ee3ced898a2d7940a9dc66003acf0850 |
| resources.assets | 5f7b9db7361a4797881a752e6eb05ccc7ac04c3e4b544d2a2e60c95c71fe65cb |
| autolathe.xml | 9ee0665b1ff23eb1e48ffc9f7ecc5b8a6dd4ebeeffb2ab454978ac2093560171 |
