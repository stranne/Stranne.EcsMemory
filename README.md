# Memory Game (Arch ECS + Godot .NET)

![Screenshot of the game](docs/screenshot.png)

## 🎯 Purpose

This project is an **exploration** of using [Arch ECS](https://arch-ecs.gitbook.io/arch) together with [Godot 4](https://godotengine.org/) (.NET).

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

- **Contracts**: Shared DTOs (like RenderModel) used to pass data from Core to GodotGame.
- **Core**: Arch ECS logic (deterministic, engine-agnostic).
- **GodotAdapter**: Thin bridge between Core and Godot.  
- **GodotGame**: Visuals and input using Godot.

## 🚀 Run the game

From the repository root:

```bash
godot --path Stranne.EcsMemory.GodotGame
```

Requires [Godot .NET](https://godotengine.org/download/) to be installed. On some systems use `godot4` or `Godot.exe`.
