using System;
using Godot;
using Godot.Collections;
using Microsoft.Extensions.Logging;
using Stranne.EcsMemory.Adapter;
using Stranne.EcsMemory.Contracts.Event;
using Stranne.EcsMemory.Contracts.Snapshots;
using Stranne.EcsMemory.Game.Utils;

namespace Stranne.EcsMemory.Game.Scenes;
public sealed partial class Game : Control, IGameEvents
{
    private readonly MemoryAdapter _memoryAdapter;
    private readonly Dictionary<int, Button> _cardButtons = [];
    private readonly ILogger<Game> _logger = GodotLoggerFactory.Instance.CreateLogger<Game>();

    [Export] private int _columns = 5;
    [Export] private int _rows = 4;
    [Export] private int _seed = 0;
    [Export] private int _minCardSize = 96;

    private GridContainer _grid = null!;
    private Label _movesLabel = null!;
    private Button _newGameButton = null!;

    public Game() => 
        _memoryAdapter = new(this, GodotLoggerFactory.Instance);

    public override void _Ready()
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(_columns);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(_rows);

        _grid = GetNode<GridContainer>("%CardGrid");
        _movesLabel = GetNode<Label>("%MovesLabel");
        _newGameButton = GetNode<Button>("%NewGameButton");

        _newGameButton.Pressed += StartNewGame;

        StartNewGame();
    }

    public override void _PhysicsProcess(double delta) =>
        Update((float)delta);

    private void StartNewGame()
    {
        var seed = _seed == 0
            ? Random.Shared.Next()
            : _seed;
        _logger.LogDebug("{StartNewGameName} seed: {Seed}.", nameof(StartNewGame), seed);

        _memoryAdapter.StartNewGame(_columns, _rows, seed);

        _memoryAdapter.Update(0);
        BuildGrid(_memoryAdapter.GameSnapshot);
    }

    private void Update(float delta = 0)
    {
        _memoryAdapter.Update(delta);

        if (_memoryAdapter.HasRenderModelChanged())
            UpdateView(_memoryAdapter.GameSnapshot);
    }

    private void BuildGrid(GameSnapshot model)
    {
        _grid.Columns = model.Columns;

        foreach (var child in _grid.GetChildren())
            child?.QueueFree();

        _cardButtons.Clear();

        foreach (var card in model.Cards)
        {
            var button = new Button
            {
                Text = "",
                ToggleMode = false,
                FocusMode = FocusModeEnum.None,
                CustomMinimumSize = new Vector2(_minCardSize, _minCardSize),
                ClipText = true
            };

            int x = card.X, y = card.Y;
            button.Pressed += () => OnCardPress(x, y);

            _grid.AddChild(button);
            _cardButtons.Add(card.Id, button);
        }
    }

    private void UpdateView(GameSnapshot model)
    {
        _movesLabel.Text = $"Moves: {model.Moves}";

        foreach (var card in model.Cards)
        {
            if (!_cardButtons.TryGetValue(card.Id, out var button))
                continue;

            button.Text = card.IsFacedUp
                ? card.PairKey is { } pairKey ? (pairKey + 1).ToString() : "?"
                : "";

            button.Disabled = model.IsLocked || card.IsMatched;
            button.Modulate = card.IsMatched
                ? new Color(1, 1, 1, 0.8f)
                : Colors.White;
        }
    }

    private void OnCardPress(int x, int y)
    {
        _logger.LogDebug("{OnCardPressName} ({X}, {Y}).", nameof(OnCardPress), x, y);
        _memoryAdapter.FlipCardAt(x, y);
        Update();
    }

    public void OnGameWon(int moves, int totalCards) =>
        _logger.LogInformation("You won after {WonEventMoves} moves! 🎉", moves);

    public override void _ExitTree()
    {
        _newGameButton.Pressed -= StartNewGame;

        _memoryAdapter.Dispose();
    }
}
