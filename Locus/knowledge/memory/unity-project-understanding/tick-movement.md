---
id: kd_c8dc2478-7b7d-480b-b423-0e530460550e
type: memory
path: unity-project-understanding/tick-movement.md
title: tick-movement
inheritInjectMode: true
summaryEnabled: true
commandEnabled: false
readOnly: false
aiMaintained: true
explicitMaintenanceRules: true
createdAt: 1777981085245
updatedAt: 1778061184926
---

# tick-movement

## Summary
Tick-based enemy movement, input IR, free player movement mask traits, target marker, timed RUAAA broadcast, and current visual helper locations.

<!-- locus:maintain-rules:start -->
Record only stable project-structure facts and lookup info for the tick/grid/free movement system. Update if movement ownership, input IR, tick order, cadence, broadcast, timed rage, or visual helper responsibilities change.
<!-- locus:maintain-rules:end -->

<!-- locus:body:start -->
- Project uses a tick-based grid movement model for enemies: `TickManager` fires `OnPlayerTick` first, then enemy move/plan tick events.
- `TickManager.CurrentTick` is the world tick counter. `OnEnemyMoveTick(int)` and `OnEnemyPlanTick(int)` let enemies run a two-phase cadence: execute already planned move, then show the next planned landing cell.
- Player grid logic lives in `Assets/_Scripts/PlayerController.cs`; enemies use `Assets/_Scripts/EnemyAI.cs`; grid occupancy is updated immediately on tick through `GameMap` nodes.
- `Assets/_Scripts/PlayerInputReader.cs` provides a small input IR: `PlayerInputSignal` with `DigitalMove` (`Vector2Int`, e.g. `(1,-1)`), normalized `AnalogMove`, ordered held moves (`FirstHeldMove` / `SecondHeldMove` for press-order-sensitive masks), and `RuaaaPressed` (F2). Use this layer before applying mask movement transforms.
- `PlayerController.freeMovementMode` lets the player move continuously outside tick cadence while still updating `currentGridPos` for enemy sensing. Free movement preserves mask traits: Hawk 2x speed, Turtle 0.5x speed, Ox diagonal-only projection, Fox knight-like direction projection where the first held move is the long `2` axis.
- Free movement collision currently checks the logical `GameMap` target cell before moving; non-Hawk movement cannot enter `Wall`/`Void`, Hawk can still bypass wall/void logic. Wall tilemaps in active prototype scenes have `TilemapCollider2D` for physics collision as well.
- Visual movement is separated from grid logic: both player and enemies keep `currentGridPos` authoritative and interpolate only `transform.position` over `moveAnimationDuration` when grid mode is used.
- `Assets/_Scripts/TickMoveVisuals.cs` is a runtime visual helper added by player/enemy scripts if missing; it creates trail and burst particle feedback using colors mapped by `MaskType`.
- `Assets/_Scripts/MoveTargetMarker.cs` creates runtime grid target markers. Player uses it for grid input landing preview; enemies use it to show planned landing cells before movement.
- `EnemyAI` has `normalMoveInterval` and `turtleMoveInterval`; default intent is most animals move every 2 ticks, Turtle every 5 ticks.
- `Assets/_Scripts/RuaaaBroadcast.cs` exposes `RuaaaBroadcast.Broadcast(Vector3, float duration = 10f)` and `IRuaaaReceiver`. F2 on player calls broadcast; `EnemyAI` implements receiver, turns red/enraged for a timed duration, and refreshes `enragedUntilTime` using max expiry so overlapping broadcasts extend to the latest end time. `Assets/_Scripts/TickRoarWave.cs` is the large map-space visual wave.
- `EnemyAI` rage visuals intentionally use white as the normal SpriteRenderer baseline, not the currently serialized scene tint, so red scene leftovers do not get cached as “normal.” `Assets/Scenes/Test_EnemyShowcase.unity` enemy SpriteRenderers should be white at rest.
<!-- locus:body:end -->
