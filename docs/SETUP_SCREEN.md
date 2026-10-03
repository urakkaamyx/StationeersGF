# Mod setup screen

Orbital Forge opens on Mod setup. The game-artwork cards open the actual editors:

- **World setup:** choose a world, inspect its fields and stage only the world definition you change.
- **Starting setup:** configure first-join equipment for the selected start, species and difficulty. This editor is fixed to NewPlayerKit.
- **Respawn setup:** configure equipment received after death. This editor is fixed to RespawnPlayerKit.
- **Recipes by machine:** choose a production machine, then edit that machine's recipes. The same item on two machines retains two separate recipe identities.

Starting and respawn use separate editor instances. Switching pages keeps each page's current container, fields and unfinished inventory. Save project captures both drafts; reopening restores both. Staging one inventory does not stage, restore or clear the other. Export requires all pending editors to be staged or restored.

For both inventories to belong to the same start profile, use the same new mod-owned start ID on both pages, such as Forge.MyStart. Each page still has separate species and difficulty choices. A kit's event, species and difficulty are part of its exported identity. Inventory staging preserves the original references for unselected contexts.

The **Start conditions & lander** shortcut opens the starting-condition editor. Configure arrival objects and lander cargo there. To assign a custom start to a world, open the start's definition, assign the exact world, then stage. The inventory pages only configure equipment.

The setup screen is a navigation and editing workbench. Opening a card does not stage a mod. Review the workspace's exact export plan before exporting. Installed game files are never modified; exported mods contain only selected native definitions and property overrides, never entire source GameData files.

## Compatibility and verification

Existing format-2 projects remain supported. A legacy pending respawn draft stored in PendingInventory is routed to the respawn editor when opened; subsequent saves use a separate PendingRespawnInventory field.

Checks cover bound setup-card routing, simultaneous starting/respawn edits, saving and reopening both drafts, independent staging, exported kit identities, and routing a machine card to the corresponding machine's recipes. Existing core/native/inventory checks also pass. Windows publishing succeeds; Windows execution and in-game testing remain pending.
