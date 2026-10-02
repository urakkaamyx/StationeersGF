# Stationeers extraction and Universal Unity Modding Studio

## Implemented foundation

This repository now contains a reproducible offline scanner, SQLite search database generator, selective UnityPy exporter, and native Stationeers recipe-mod exporter. These are the first working backend components. A desktop editor, runtime bridge, assembly decompiler, live object inspector, and full universal game support remain future work.

Game binaries are input evidence. The extractor reads them without launching the game or loading managed assemblies into the Python process. TypeTreeGenerator uses its own metadata loader to generate serialization layouts; it does not instantiate game objects.

## Separate shared backends from game adapters

| Layer | Current implementation | Extension point |
|---|---|---|
| File inventory | Paths, sizes, LFS pointer detection, selected SHA-256 hashes | Platform and installation discovery |
| Unity Mono metadata | ECMA-335 TypeDef, inheritance, fields, methods, references and tokens | Signature decoding, IL disassembly, property/event metadata |
| Unity assets | Serialized object IDs, class IDs, ranges; GameObject names and components; MonoScript identities | UnityPy parsing and selective export |
| UnityPy | Pinned 1.25.3; independent verification; textures, sprites, meshes, TextAssets and MonoBehaviour JSON export | Copy-based asset patching with round-trip verification |
| Stationeers adapter | XML semantic trees, 621 recipe records, native mod-folder export | Plants, traders, environments, attributes and start-condition editors |
| Search | Read-only SQLite CLI | C# desktop application and JSON service |
| Runtime | Existing BepInEx installation observed | BepInEx bridge and tested game-specific hooks |
| IL2CPP | Not implemented | Native metadata adapter and independent capability detection |

The XML extraction and recipe exporter are Stationeers-specific. PE metadata inspection and Unity serialized-file inspection are reusable building blocks, not a promise that every Unity game supports the same edits.

## Source model

Use `(source SHA-256, asset-relative path, path ID)` for an asset object identity. Use `(assembly SHA-256, metadata token)` for managed members. Recipe IDs use source path, section and ordinal and must always be paired with the source hash: ordinals and metadata tokens are not stable across game updates.

Keep snapshots immutable. Store game build/Unity version, file hashes, parser versions, parse errors and verification status. New scans form new snapshots. Future version comparison must match semantic keys and classify ambiguous matches; never carry a token or path ID blindly across updates.

The current generated output replaces the selected output directory's database and JSON files. For historical runs, choose a distinct output directory per snapshot. Do not run concurrent scans into the same output directory. Semantic XML JSON preserves tags, attributes, text and child order. Original XML remains the authority for exact whitespace, comments and bytes.

## Asset relationships

GameObjects contain component PPtrs. Resolve a PPtr with both `file_id` and `path_id`; IDs are local to serialized files. `file_id=0` refers to the same asset. External-file references require the asset's external reference table, which is not normalized in the current scanner. Do not treat unresolved external pointers as missing game objects.

GameObject names are not unique. Scene instances, visual children and prefab-like objects can share names. There are 153,161 serialized objects in this snapshot, not 153,161 independent items or prefabs. A future prefab registry should combine `Prefab.AllPrefabs`, Thing.PrefabName/PrefabHash, GameObject components and recipe references.

Stationeers strips embedded type trees in the inspected assets. UnityPy's optional TypeTreeGeneratorAPI reconstructs MonoBehaviour layouts using the provided Mono assemblies. The shipped DLL directory contains aliases with duplicate module names; the adapter loads one canonical file per module name, preferring the matching filename. It does not suppress genuine DLL-loading errors.

## Editing and packaging

Prefer the shipped native XML mod system for recipe and supported attribute edits. `ExampleMod.zip` demonstrates `About/About.xml`, `GameData/*.xml` and a partial recipe override. Its readme warns duplicate recipes are discarded and mod load order matters. `AttributesExampleMod.zip` demonstrates ThingModData, StackableModData, render distance, stack quantity and reagent changes.

The recipe exporter requires an exact source file, section and prefab. It rejects multiple matching recipes, unknown fields, nested fields, duplicate changes, nonfinite or negative values, existing output directories, and outputs inside game directories. It exports a separate mod folder and ZIP, records old/new values and source hash, and checks source preservation. It does not apply or enable a mod.

For future Unity binary edits: select exact source hash and object identity; parse with UnityPy; produce a patch journal; write a copy in staging; reopen with UnityPy; compare object counts, IDs, pointers and changed fields; validate resource sidecars; install only with a recoverable backup. Meshes, textures and custom components have different serialization constraints. Successful parsing does not prove a binary patch is safe in-game.

## Runtime architecture to implement

Use a small C# BepInEx plugin within the game's compatible runtime. Keep the offline tool/UI independent of game assemblies. The bridge should expose commands and events with build fingerprint and capability discovery. Dispatch Unity operations to the main thread. Each Stationeers mutation must respect the game's authority and synchronization paths, rather than just setting a field on a local object.

Observed candidate surfaces include Prefab registration and lookup, Thing interaction and save/network serialization, SimpleFabricatorBase recipes and logic methods, ModConfig enable/load-order methods, atmospheric managers, inventory, programmable chips and station saves. These are metadata candidates. Their behavior and signatures need IL analysis and in-game verification before becoming public adapter operations.

Runtime commands should carry request IDs, object reference IDs, expected state and result/error details. Separate read-only inspection from mutating commands. Reconcile inventory, spawning, recipes and machine changes against server authority. Do not describe multiplayer consistency as solved until tested with host and client.

## Desktop editor plan

The default implementation direction is C# for the desktop/tool service and game bridge, with Python/UnityPy as a versioned extraction worker. The UI should expose Game Profile, Machines/Recipes, Items/Prefabs, Assets, Classes, World Data, Mods and Runtime pages. Selecting a recipe shows its machine section, prefab, source file/hash, material fields, constraints, time/energy fields, alternative machine recipes and export preview. Selecting an object shows exact asset/path ID, component references, decoded fields and class metadata.

Preserve source values and names. Units and numeric semantics require game-code verification. An unresolved reference, unavailable custom layout or unsupported patch must appear explicitly in the capability report. The current CLI is functional; this UI has not yet been built.

## Current decode limitations

The normalization pass decoded 1,574 Thing components with 1,569 distinct prefab names and linked all 621 recipes. Four components have null scripts. One Book component (resources.assets path ID 108952) fails strict byte-consumption validation and remains unresolved. Generic TypeSpec base signatures are not followed in the normalization pass.

## Next implementation milestones

1. Complete current extraction and pin parser dependencies (implemented and validated).
2. Normalize item/recipe/machine links and decode custom prefab components in batches with per-object failure records.
3. Decode managed signatures and IL, then verify XML loader precedence, field units and runtime hooks.
4. Build the C# database service and desktop data/asset browser.
5. Add native XML editors with diff, provenance, validation and mod export.
6. Build and test the BepInEx runtime inspector on the user's Windows game installation.
7. Add copy-based Unity asset editing and round-trip checks.
8. Add another Unity game adapter and IL2CPP support to validate shared-core boundaries.
