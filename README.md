# StationeersGF — Orbital Forge

Orbital Forge is a native C# desktop mod creator for the included Stationeers snapshot. Its sci-fi workbench uses actual game thumbnails and planet artwork, searchable recipe and prefab galleries, an override inspector, a project workspace and configurable appearance.

- [Use, build and validate the mod creator](docs/MOD_CREATOR.md)
- [C# architecture and extension points](docs/MOD_CREATOR_ARCHITECTURE.md)
- [Verified game analysis](docs/GAME_ANALYSIS.md)
- [Extraction architecture](docs/ARCHITECTURE.md)
- [Scanner, UnityPy exporter and CLI tools](docs/USAGE.md)

The desktop tool edits native recipe and supported numeric prefab-property overrides, saves editable projects, and exports separate Stationeers mod ZIPs. It includes 621 recipes, 1,569 prefab definitions and 1,558 extracted prefab thumbnails. Generated catalogs and artwork are rebuilt with the pinned UnityPy environment; the prepared Windows download includes them.

C# source uses one type per file, interfaces for repositories, persistence, validation, drafting and export, and constructor dependency injection. Original game files are inputs. Runtime scripting, assembly patching, new 3D asset import and a general IL2CPP adapter remain future work.
