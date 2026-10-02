# C# architecture

One class, record or interface lives in each correspondingly named file. Methods perform a named task; composition and UI orchestration delegate data, drafting, validation, persistence and export to focused services. No global service locator is used.

| Project / folder | Responsibility |
| --- | --- |
| Core / Models | Catalog definitions, editable projects, patches and appearance settings |
| Core / Interfaces | Contracts for catalogs, artwork, drafts, validation, project/settings stores and native export |
| Core / Services | Implementations independent of Avalonia |
| Desktop / ViewModels | Observable screen state and commands |
| Desktop / Views | Styled Avalonia UI and the explicit discard dialog |
| Desktop / Interfaces | Bitmap, native file-picker and theme contracts |
| Desktop / Services | Constructor composition, platform adapters and rendered verification |
| Checks | Integration checks against the actual prepared catalog |

`CompositionRoot` explicitly wires interface-based constructor dependencies. `MainViewModel` coordinates the five screens; individual card and field view models each occupy their own file. `NativeGameDataWriter` handles XML; `ModExporter` handles packaging; `AtomicFileWriter` handles replacement of saved files. These tasks are separate from the presentation layer.

The immutable source catalog retains original XML, field values, object identifiers and source SHA-256 hashes. `DraftService` applies edits to a candidate clone, validates it, and swaps it into the project only on success. Source-equivalent changes remove an override. Project validation checks the catalog fingerprint, per-source hashes, supported fields, nonnegative finite numeric values, positive integer stack quantities, duplicates and condition bounds.

Recipe identity includes its machine section and source file, so editing an autolathe product does not change the same product in another printer. Export clones the complete original recipe record and changes only selected leaf fields. Thing property overrides use the native `ThingModData` or inherited `StackableModData` schema. Every exported project has its own folder and safe ZIP paths.

Artwork is decoded from the source game using UnityPy and packaged in an indexed ZIP. Cached Avalonia bitmaps reuse the same images throughout the galleries, inspector and preview. Colors are dynamic resources; persisted preferences set the accent, contrast, font size and card density.

Extensions should implement an existing interface where possible. New editor domains should receive their own models, validator and writer rather than placing new serialization logic in the window. General code patches, new meshes/textures, live game inspection and IL2CPP are outside this version's native XML scope.
