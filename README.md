# StationeersGF

Private Stationeers source snapshot and the working extraction foundation for a Universal Unity Modding Studio.

- [Verified game analysis](docs/GAME_ANALYSIS.md)
- [Architecture and remaining work](docs/ARCHITECTURE.md)
- [Run the scanner, search, UnityPy exporter and native recipe-mod exporter](docs/USAGE.md)

The backend currently indexes Mono metadata, XML recipes and Unity assets; decodes Thing-derived components using UnityPy; searches a generated SQLite database; and exports separate native XML recipe mods. The desktop editor, runtime bridge, assembly decompiler and IL2CPP adapter remain future work.

UnityPy is pinned to 1.25.3 and TypeTreeGeneratorAPI to 0.0.10 in `tools/requirements-assets-lock.txt`. Generated large extracts stay out of Git and can be rebuilt from this source snapshot. Original game files are input evidence and are not modified by extraction.
