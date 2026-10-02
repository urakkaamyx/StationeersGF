# Stationeers starting and respawn inventory

Inspected snapshot: Unity 2022.3.62f3, game files supplied for Orbital Forge. This is a code-and-asset trace, not an in-game test.

## How XML reaches inventory

A StartCondition binds Spawn references to five distinct events: NewWorld, NewPlayer, NewPlayerKit, RespawnPlayer and RespawnPlayerKit. The two Kit events populate the existing character; the player events control arrival separately. WorldSetting executes start bindings, followed by world bindings. If an event has no bindings, it uses the default fallback. Bindings that exist but fail their predicates do not trigger that fallback.

SpawnData follows a reference when it has no populated spawn content. A populated Spawn executes Items, DynamicThings, then nested Spawns. A spawned item executes its nested DynamicThings, Items, and Spawns in that order. Species conditions on kit references inspect the parent character. Item conditions execute without a character parent. Difficulty predicates use the registered difficulty order and comparison operator; an omitted comparison means EqualOrGreater.

DynamicSpawnData selects the receiving slot in this order:

1. Explicit zero-based SlotIndex on the immediate parent.
2. First unoccupied slot with the matching SlotId hash.
3. First unoccupied slot of the requested SlotClass.
4. First unoccupied slot compatible with the prefab's SlotType.

SlotIndex therefore refers to the character when the item is directly in the kit, and to the suit, belt, backpack or other container when nested under that item. It is not a global inventory index. The slot Location transform anchors the model after MoveToSlot.

## Character slots

| Index | Slot | Human / Zrilian class | Robot class |
|---:|---|---|---|
| 0 | LeftHand | None | None |
| 1 | RightHand | None | None |
| 2 | Helmet | Helmet | Helmet |
| 3 | Suit | Suit | Suit |
| 4 | Back | Back | Back |
| 5 | Brain | Organ | Organ |
| 6 | Uniform | Uniform | Battery |
| 7 | Lungs | Organ | Organ |
| 8 | Belt | Belt | Belt |
| 9 | Glasses | Glasses | Glasses |
| 10 | Stomach | Organ | Organ |

Robot uses Character, not the RobotMining prefab. Human.SetSpeciesSpecificSlots changes slot 6's type and key to Battery. Organ contents are species-managed and are inspection-only in this editor.

## Common nested containers

| Container | Slot arrangement |
|---|---|
| Standard / emergency EVA suit | 0 air canister, 1 waste canister, 2 life-support battery, 3–5 gas filters |
| Basic jetpack | 0 propellant canister, 1–9 general storage |
| Uniform | 0–1 general storage, 2 access card, 3 credit card |
| Tool belt | Eight Tool slots |
| Mining belt | Two Tool slots and eight Ore slots |
| Tablet | Battery and cartridge |

Default Human and Zrilian starting kits include duct tape in the right hand, helmet, suit, uniform, jetpack and tool belt. Their breathing gases and filters differ. Normal and Stationeer respawn use emergency equipment; Creative and Easy use the full kit. Robot kits populate the battery slot. The editor resolves XML predicates instead of trusting description text.

## Editor and mod export

Open **Start & respawn**, choose the start, event, species and difficulty, then load. Click equipment and use **Open item contents** to navigate its inventory. Empty slots offer compatible prefabs. Items can be removed or moved into a compatible empty destination; nested contents move with the container. Quantity, battery charge, gases and existing XML fields can be edited. Save preserves unfinished edits. Export requires staging.

Staging creates a new Forge-prefixed start profile, an event router and the selected context's kit. The router keeps the original references for the other species/difficulty contexts. NewPlayer, RespawnPlayer and the lander remain separate from inventory editing. The native loader receives only selected definitions, never the whole source GameData file. No installed game files are written.

Select the new start in game. For worlds that require a fixed StartCondition, use World Studio to assign the new start to the exact world definition. The inventory editor does not automatically rewrite world definitions.

## Coverage and limits

Metadata covers 1,567 prefabs and 296 containers. ItemPotato and SeedBag_Potato are in an external asset not included in this extraction and are excluded from the picker. The bundled metadata contains prefab IDs/hashes, MonoBehaviour path IDs, slot classes, specific-prefab restrictions, stored slot hashes and attachment names.

The preview checks class compatibility, explicit prefab restrictions, collisions and capacity. It does not simulate survival, pressure, gases, device behavior, biological organ creation or every subclass override of CanEnter. World-level kit bindings and other mods are not merged into this start-profile preview. Spawn actions/world content, nested predicates and mixed execution orders that cannot safely be flattened are blocked. Verify exported mods in game and confirm load order.

## Evidence

Traced classes: WorldSetting, SpawnData, ThingSpawnData, DynamicSpawnData, Slot, Thing, DynamicThing, Human, SpeciesCondition, DifficultyCondition and ConditionComparable. The associated proprietary assembly/decompiled files are not distributed in the source package.

- resources.assets SHA-256: `5f7b9db7361a4797881a752e6eb05ccc7ac04c3e4b544d2a2e60c95c71fe65cb`
- Assembly-CSharp.dll SHA-256: `6caeb8414a83cf47bdc86e0978b0b011ee3ced898a2d7940a9dc66003acf0850`

Validation: existing 17 core/native checks, all 24 default species/difficulty/event combinations, additive context preservation, repeated event edits, occupied/incompatible/hash-selected slots, and seven rendered UI interaction checks. Windows self-contained publishing succeeds; Windows execution and in-game behavior remain untested.
