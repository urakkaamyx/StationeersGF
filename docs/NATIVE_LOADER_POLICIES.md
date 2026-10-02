# Native mod compilation policy

This policy was traced against the managed loader in the included game snapshot. It describes this implementation, not a promise of compatibility with every Stationeers update.

## Output boundary

The compiler only writes a separate mod archive. It does not write to the game installation. The archive contains About metadata, selected preview artwork and selected GameData records. Source catalogs, source XML files, extracted artwork archives and editable project JSON are excluded.

| Domain | Native loader behavior | Compiler output |
| --- | --- | --- |
| Recipes | Selected recipe is replaced within its machine section | Exact recipe record, preserving required unchanged fields |
| Supported prefab properties | Native ThingModData / StackableModData property overrides | Selected property deltas |
| Start conditions and spawn packages | DataCollection registers whole objects by ID | New definition or exact selected definition replacement |
| Difficulty | Whole DifficultySetting object registered by ID | One selected definition under DifficultySettings |
| World settings | Whole world registration and re-registration | One selected World under WorldSettings; existing resource paths referenced |
| Weather, ore veins, life requirements | Whole registered objects by ID | Exact selected definitions |
| Other catalog domains | Loader/resource policy not yet fully supported by compiler | Inspection only; export rejected |

DataCollection.Register replaces an existing object when the incoming definition comes from a mod. It does not merge missing scalar fields. For these domains, keeping the selected definition's unchanged context is necessary. Siblings from the original file are never emitted.

## Starting-condition graph

The serializer uses StartCondition and Spawn. Historical StartingConditions/RespawnConditions examples are not used as the export schema. NewWorld, NewPlayer, NewPlayerKit, RespawnPlayer and RespawnPlayerKit remain separate event bindings. World injection uses exact world IDs, including Mars2, Lunar, Europa3, Vulcan2, MimasHerschel and Venus.

Spawn references can share packages. Follow reference deliberately edits a shared definition; fork reference creates a unique local child and rewires the selected parent in one transaction. Reachable unresolved references and cycles block export. Unrelated broken source branches do not block a mod that does not depend on them.

World exports retain atmosphere and preview context and reuse existing game resource references. New arbitrary resource paths are rejected. Weather validation requires cooldown, delay, duration, temperature offset, solar ratio and wind strength. Quantity, numeric finiteness, source field types and selected boolean fields are checked.

## Verification and limits

Integration checks parse actual export XML and archives, compare source records, exercise local forks and reopen projects. Rendered UI checks exercise bound Avalonia controls with actual catalog artwork. Windows binaries are cross-published from Linux; Windows execution and in-game loading are not verified in this environment.

Source-derived validation is intentionally narrower than a complete game schema validator. New asset import, executable mods, live runtime inspection and full dependency reporting across every catalog domain remain future work. Do not treat inspection-only categories as implemented export capabilities.
