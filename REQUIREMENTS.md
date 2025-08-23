# Memory Game with Arch ECS + Godot .NET — Implementation Guide (Checklist)

> Goal: Learn Arch ECS by keeping **all game logic** in a pure .NET library and using **Godot** only for input and rendering. Keep it simple, deterministic, and testable.

---

## 0) Scope & Constraints

* 2D memory game (flip tiles to match pairs).
* Logic is deterministic and lives in `Stranne.EcsMemory.Core` (no Godot types).
* Godot is a thin shell: maps clicks → commands, pulls a render model → updates sprites.
* A few focused unit tests validate core behavior.

---

## 1) Solution Layout

```text
/Stranne.EcsMemory/
  Stranne.EcsMemory.Core            // Arch ECS logic (no Godot references)
  Stranne.EcsMemory.Core.Tests      // Unit tests for Stranne.EcsMemory.Core
  Stranne.EcsMemory.GodotAdapter    // Thin bridge: queues commands, ticks world, returns RenderModel
  Stranne.EcsMemory.GodotGame       // Godot 4.3 C# project (scenes, sprites, input)
```

### ✅ Checklist

* [x] Create four projects (Core, GodotAdapter, GodotGame, Core.Tests).
* [x] Reference `Stranne.EcsMemory.Core` from `Stranne.EcsMemory.GodotAdapter` and `Stranne.EcsMemory.Core.Tests`.
* [x] Reference both and `Stranne.EcsMemory.GodotAdapter` from `Stranne.EcsMemory.GodotGame`.
* [x] Add Arch ECS NuGet to `Stranne.EcsMemory.Core` (and tests if needed).

---

## 2) Core Domain Model (Stranne.EcsMemory.Core)

### Components (minimal, immutable-friendly)

* `CardId { int Value }`
* `PairKey { int Value }`
* `GridPos { int X, int Y }`
* `FaceState { bool IsFaceUp }`
* `Matched { bool Value }`

### Singletons / Tags

* `GameState { int FlipsThisTurn; int Moves; bool IsLocked; Entity? FirstFlipped; }`
* `PendingEvaluation { int TicksLeft }` *(added only while waiting)*
* `Config { int Cols; int Rows; int EvalDelayTicks; int Seed; }`

### Commands

* `FlipAtGrid(int X, int Y)`
* `ICommandQueue` + `CommandQueue` (FIFO of objects)

### ✅ Checklist

* [x] Define all components as small structs/classes.
* [x] Define `GameState`, `Config` singletons.
* [x] Define `ICommandQueue` and `CommandQueue`.

---

## 3) Systems (Stranne.EcsMemory.Core)

1. **BoardSetupSystem**

   * Creates pair keys (N = cols\*rows/2).
   * Creates 2 entities per pair.
   * Shuffles order using `Rng` and assigns `GridPos`.
   * Sets `FaceState.IsFaceUp = false`, `Matched.Value = false`.

2. **FlipRequestSystem**

   * Consumes `FlipAtGrid` commands if not locked.
   * Ignores clicks on matched or already face-up cards.
   * On first flip: stores `FirstFlipped`, increments `FlipsThisTurn`.
   * On second flip: sets `IsLocked = true`, adds `PendingEvaluation(TicksLeft = EvalDelayTicks)`.

3. **MatchSystem**

   * Runs only when `PendingEvaluation` exists and ticks it down.
   * When it reaches 0:

     * If two face-up cards share the same `PairKey` → mark both `Matched=true`.
     * Else → flip both back to face-down.
     * Reset `FlipsThisTurn`, clear `FirstFlipped`, `IsLocked=false`, increment `Moves`.
     * Remove `PendingEvaluation`.

4. **WinCheckSystem**

   * Sets a flag in render model (or store as `AllMatched`) if all cards are matched.

5. **RenderQuerySystem**

   * Projects ECS state into a pure DTO `RenderModel`.

### ✅ Checklist

* [x] Implement `BoardSetupSystem`.
* [x] Implement `FlipRequestSystem` with command dequeue.
* [x] Implement `MatchSystem` with countdown lock.
* [x] Implement `WinCheckSystem`.
* [x] Implement `RenderQuerySystem` that builds `RenderModel`.

---

## 4) Render DTO (Stranne.EcsMemory.Core → Adapter)

```csharp
public sealed class RenderModel {
  public required List<RenderCard> Cards { get; init; }
  public bool IsLocked { get; init; }
  public bool IsWon { get; init; }
  public int Moves { get; init; }
}

public sealed class RenderCard {
  public required int Id { get; init; }
  public required int X { get; init; }
  public required int Y { get; init; }
  public required bool IsFaceUp { get; init; }
  public required bool IsMatched { get; init; }
  // PairKey may be omitted from the client-facing model to avoid “cheating”
}
```

### ✅ Checklist

* [x] Create DTOs above in `Stranne.EcsMemory.Core` (or a shared project the adapter can see).
* [x] Ensure DTOs are engine-agnostic and serializable.

---

## 5) World Lifecycle (Stranne.EcsMemory.Core)

Create a small builder/helper:

* `WorldFactory.Create(Config cfg, ICommandQueue q)`:

  * Creates `World`.
  * Registers singletons (`Config`, `Rng`, `GameState`) and systems in order.
  * Calls `BoardSetupSystem` once.
* `Tick(int steps = 1)` to run update systems in a fixed sequence:

  * Typical order per tick: `FlipRequestSystem → MatchSystem → WinCheckSystem → RenderQuerySystem`.

### ✅ Checklist

* [x] Implement a factory to build and seed the world.
* [x] Decide a fixed system order and keep it consistent (determinism).
* [x] Expose `Tick()` and `GetRenderModel()`.

---

## 6) Adapter Layer (Stranne.EcsMemory.GodotAdapter)

Responsibilities:

* Owns the `World` and `ICommandQueue`.
* Public API:

  * `Start(NewGameConfig cfg)` → builds world with seed.
  * `QueueFlip(int x, int y)` → enqueues `FlipAtGrid`.
  * `Tick()` → advances ECS once (or N fixed steps).
  * `RenderModel GetRenderModel()` → current snapshot from ECS.

Implementation notes:

* Keep the adapter dumb; **no** game logic beyond coordinating calls.
* Optionally provide a `Reset(seed?)` method for replayability.

### ✅ Checklist

* [x] Implement the adapter class with above API.
* [x] Ensure no Godot types leak into adapter.

---

## 7) Godot Client (Stranne.EcsMemory.GodotGame)

### Scene Skeleton

* Root: `Node2D` (or `Control`) → grid of `CardNode`s.
* `CardNode` can be a `Button` or `Sprite2D` with click handling.
* Simple visuals:

  * Back side: solid color or single texture.
  * Front side: numeric label or small icon tied to `PairKey` (if you include it).
  * Matched cards: reduce opacity (e.g., 0.4).

### Game Loop

* On `_Process(delta)`:

  * `adapter.Tick();`
  * `var model = adapter.GetRenderModel();`
  * `UpdateView(model);` (loop cards and update nodes)

### Input

* On card click → compute logical grid `(x,y)` → `adapter.QueueFlip(x,y)`.

### Layout

* Convert `(x,y)` to screen coords using a fixed cell size (e.g., `Cell = 96 px`).
* Position with `cardNode.Position = new Vector2(x * Cell, y * Cell)`.

### ✅ Checklist

* [ ] Create a root `GameNode` script: holds adapter, does `_Process`.
* [ ] Create a `CardNode` with `UpdateFrom(RenderCard c)` and click → `(x,y)`.
* [ ] Map grid size from `Config` to actual grid of nodes.
* [ ] Add a minimal HUD: moves counter, “New Game” button (calls `Start` with new seed).

---

## 8) Tests (Stranne.EcsMemory.Core.Tests)

Focus on **Core**; no Godot types.

### Suggested Tests

1. **Setup creates balanced pairs**

   * For a 4×3 board: 12 cards → 6 pair keys, each appears exactly twice.
2. **Matching flow**

   * Flip two matching cards → both marked `Matched=true`, `IsLocked=false`, `Moves++`.
3. **Mismatch flow with delay**

   * Flip two non-matching cards → `IsLocked=true`.
   * After `EvalDelayTicks` → both flip back down, `IsLocked=false`.
4. **Win condition**

   * After all matched → `IsWon=true` in `RenderModel`.

### Helpers

* `TestWorld(seed, cols, rows, delay)` to create a ready world.
* Utilities: find card by `(x,y)`, by `PairKey`, force specific flips.

### ✅ Checklist

* [ ] Add the 4 tests above.
* [ ] Use fixed seeds for deterministic behavior.
* [ ] Keep assertions small and focused.

---

## 9) Determinism & Timing

* Use a **fixed** number of `Tick()` calls to simulate time (no real-time dependence).
* Store `EvalDelayTicks` in `Config` (e.g., 10).
* If you need animations, keep them in Godot only (don’t affect logic).

### ✅ Checklist

* [ ] No `DateTime.Now` or frame-time in Core.
* [ ] All randomness via `Rng.Random` seeded from `Config.Seed`.
* [ ] One source of truth for tick progression (adapter).

---

## 10) Minimal Code Sketches (illustrative)

### `ICommandQueue`

```csharp
public interface ICommandQueue {
  void Enqueue(object command);
  bool TryDequeue(out object command);
}

public sealed class CommandQueue : ICommandQueue {
  private readonly Queue<object> _q = new();
  public void Enqueue(object cmd) => _q.Enqueue(cmd);
  public bool TryDequeue(out object cmd) => _q.TryDequeue(out cmd!);
}
```

### Flip Flow (outline)

```csharp
// FlipRequestSystem.Update(world):
// - if locked: return
// - while commands:
//    - if FlipAtGrid(x,y):
//        - find card at (x,y)
//        - ignore if matched or face-up
//        - flip face-up
//        - gs.FlipsThisTurn++
//        - if first flip: gs.FirstFlipped = entity
//        - if second flip:
//            gs.IsLocked = true
//            add PendingEvaluation { TicksLeft = cfg.EvalDelayTicks }
```

### Match Resolution (outline)

```csharp
// MatchSystem.Update(world):
// - if no PendingEvaluation: return
// - if --TicksLeft > 0: return
// - get two face-up, unmatched cards
// - if same PairKey: set both Matched = true
//   else: set both FaceUp = false
// - gs.Moves++; gs.FlipsThisTurn = 0; gs.FirstFlipped = null; gs.IsLocked = false
// - remove PendingEvaluation
```

---

## 11) Milestones (learning-first)

1. **Core ready**: setup + flip/match/mismatch + render projection.
2. **Adapter ready**: start/tick/queue flip/get model.
3. **Client ready**: show grid, click to flip, update sprites.
4. **Polish**: add moves counter, “New Game” button, simple flip tween in Godot.

### ✅ Checklist

* [ ] Milestone 1: All core tests green.
* [ ] Milestone 2: Adapter methods callable from a console smoke test.
* [ ] Milestone 3: Godot shows playable game.
* [ ] Milestone 4: Small UX polish (HUD + tween).

---

## 12) Nice-to-have (optional, still simple)

* **Seed control**: replay exact board.
* **Board sizes**: 2×2, 4×3, 4×4 toggle.
* **Stats**: best moves for each size (client-side only).
* **Snapshot/restore**: serialize `RenderModel` for save/resume (outside Core logic).

### ✅ Checklist

* [ ] Seed entry in UI.
* [ ] 1–2 alternate board sizes.
* [ ] Simple “best score” memory in client.

---

## 13) Common Pitfalls (avoid)

* Mixing Godot types into Core.
* Using real-time delays in Core (stick to tick counters).
* Non-deterministic shuffles (don’t forget seed).
* Letting animations drive logic (they should only reflect it).

---

## 14) Definition of Done

* All core logic covered by unit tests (setup, flip, mismatch, match, win).
* Playable in Godot (mouse click → flip → lock → resolve).
* No Godot types in Core or Adapter.
* Deterministic behavior from a given seed.
* Small, readable systems with a fixed update order.

---

### Quick Start (TL;DR)

* [ ] Implement Core components/singletons/commands.
* [ ] Add systems: `BoardSetup`, `FlipRequest`, `Match`, `WinCheck`, `RenderQuery`.
* [ ] Build `WorldFactory` + `Tick()` and `GetRenderModel()`.
* [ ] Create Adapter (`Start`, `QueueFlip`, `Tick`, `GetRenderModel`).
* [ ] Godot: grid of clickable nodes → adapter queue → tick → update view.
* [ ] Write the 4 core tests and keep them green.
