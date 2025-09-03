# Memory Game (Arch ECS + Godot .NET)

![Screenshot of the game](docs/screenshot.png)

## 🎯 Purpose

This project is an **exploration** of using [Arch ECS](https://arch-ecs.gitbook.io/arch) together with [Godot 4](https://godotengine.org/) (.NET).

*Features: [deterministic gameplay](Stranne.EcsMemory.Core/Utils/BoardSetupUtil.cs), [command pattern](Stranne.EcsMemory.Core/Commands/), [EventBus integration](Stranne.EcsMemory.Core/Events/), [complete state serialization (save/load)](Stranne.EcsMemory.Core/GameCore.cs), and engine-agnostic game logic.*

## 🕹️ How the game works

- It’s a classic **Memory** game:
  - Flip two cards at a time.
  - If they match, they stay visible.
  - If not, they flip back after a short delay.
  - The game ends when all cards are matched.

- The UI is minimal:  
  - A grid of cards (buttons).  
  - A move counter.  
  - A *New Game* button to restart with a new seed.

## 🧩 Project structure

- **Adapter**: Thin bridge between Core and Game (Godot).  
- **Contracts**: Shared DTOs and event interfaces for cross-assembly communication.
- **Core**: Arch ECS logic (deterministic, engine-agnostic) with EventBus for event handling.
- **Core.Tests**: Unit tests for Core game logic.
- **Game**: Visuals and input using Godot game engine.

## 🚀 Run the game

Before running, make sure you have [Godot .NET](https://godotengine.org/download/) installed.

- **From IDE**: open the solution in Visual Studio and run the `Stranne.EcsMemory.Game` project.  
- **From CLI**: from the repository root, run:

```bash
godot --path Stranne.EcsMemory.Game
```

On some systems use `godot4` or `Godot.exe`.
