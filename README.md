# StationeersGF — Orbital Forge

Orbital Forge is a native C# desktop mod creator for the included Stationeers snapshot. Its sci-fi workbench uses actual game thumbnails and planet artwork, searchable recipe and prefab galleries, an override inspector, a project workspace and configurable appearance.

- [Use, build and validate the mod creator](docs/MOD_CREATOR.md)
- [C# architecture and extension points](docs/MOD_CREATOR_ARCHITECTURE.md)
- [Verified game analysis](docs/GAME_ANALYSIS.md)
- [Extraction architecture](docs/ARCHITECTURE.md)
- [Scanner, UnityPy exporter and CLI tools](docs/USAGE.md)

The desktop tool edits recipes, supported numeric prefab properties, worlds, starting conditions, lander cargo, loadouts, respawn packages, difficulty, weather, ore veins and plant requirements, saves editable projects, and exports separate Stationeers mod ZIPs. It includes 621 recipes, 1,569 prefab definitions and 1,558 extracted prefab thumbnails, plus 569 native definitions across 16 categories. Generated catalogs and artwork are rebuilt with the pinned UnityPy environment; the prepared Windows download includes them.

C# source uses one type per file, interfaces for repositories, persistence, validation, drafting and export, and constructor dependency injection. Original game files are inputs. Runtime scripting, assembly patching, new 3D asset import and a general IL2CPP adapter remain future work.

## Starting and respawn inventory

The Start & respawn workbench opens real equipment/container slots, resolves species and difficulty kits, and exports local kit changes through a new start profile. See [inventory mapping](docs/Inventory-System-Mapping.md) for slot indices, export behavior and coverage.

Regenerate metadata with `python tools/prepare_inventory_catalog.py GAME_ROOT --slot-source LOCAL_SLOT_DECOMPILATION.cs`. Install `tools/requirements-assets-lock.txt`; the supplied source package already includes the extracted metadata and artwork.
