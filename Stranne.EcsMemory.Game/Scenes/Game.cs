using System;
using System.Linq;
using Godot;
using Godot.Collections;
using Microsoft.Extensions.Logging;
using Stranne.EcsMemory.Adapter;
using Stranne.EcsMemory.Contracts;
using Stranne.EcsMemory.Contracts.Event;
using Stranne.EcsMemory.Contracts.Snapshots;
using Stranne.EcsMemory.Game.Utils;

namespace Stranne.EcsMemory.Game.Scenes;
public sealed partial class Game : Control, IGameEvents
{
    private readonly GameAdapter _gameAdapter;
    private readonly Dictionary<int, Button> _cardButtons = [];
    private readonly ILogger<Game> _logger = GodotLoggerFactory.Instance.CreateLogger<Game>();

    [Export] private int _columns = 5;
    [Export] private int _rows = 4;
    [Export] private int _seed = 0;
    /// <summary>
    /// Number of update cycles to wait before evaluating matches. Higher values give more time to see both cards.
    /// </summary>
    [Export] private int _evaluationDelayUpdates = 30;
    [Export] private int _minCardSize = 96;

    private GridContainer _grid = null!;
    private Label _movesLabel = null!;
    private Button _newGameButton = null!;

    public Game()
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(_columns);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(_rows);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(_evaluationDelayUpdates);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(_minCardSize);

        var seed = _seed == 0 
            ? null as int?
            : _seed;
        var gameConfiguration = new GameConfiguration(_evaluationDelayUpdates);
        _gameAdapter = GameAdapter.LoadOrCreateNewGame(_columns, _rows, seed, gameConfiguration, this, GodotLoggerFactory.Instance);
    }

    public override void _Ready()
    {
        _grid = GetNode<GridContainer>("%CardGrid");
        _movesLabel = GetNode<Label>("%MovesLabel");
        _newGameButton = GetNode<Button>("%NewGameButton");

        _newGameButton.Pressed += StartNewGame;

        // Initialize view with current state (loaded save or new game)
        _gameAdapter.Update(0);
        RebuildAndUpdateView(_gameAdapter.GetGameSnapshot());
    }

    public override void _PhysicsProcess(double delta) =>
        Update((float)delta);

    private void StartNewGame()
    {
        var seed = _seed == 0
            ? Random.Shared.Next()
            : _seed;
        _logger.LogDebug("{StartNewGameName} seed: {Seed}.", nameof(StartNewGame), seed);

        _gameAdapter.StartNewGame(_columns, _rows, seed);

        _gameAdapter.Update(0);
        RebuildAndUpdateView(_gameAdapter.GetGameSnapshot());
    }

    private void Update(float delta = 0)
    {
        _gameAdapter.Update(delta);

        if (_gameAdapter.HasSnapshotChanged())
            UpdateChangedCards(_gameAdapter.GetGameSnapshot());
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

    private void RebuildAndUpdateView(GameSnapshot model)
    {
        BuildGrid(model);
        UpdateMovesLabel(model);
        foreach (var card in model.Cards)
            UpdateCard(model, card);
    }

    private void UpdateChangedCards(GameSnapshot model)
    {
        UpdateMovesLabel(model);
        foreach (var card in model.Cards.Where(x => x.HasChanged))
            UpdateCard(model, card);
    }

    private void UpdateMovesLabel(GameSnapshot model) => 
        _movesLabel.Text = $"Moves: {model.Moves}";

    private void UpdateCard(GameSnapshot model, CardSnapshot card)
    {
        if (!_cardButtons.TryGetValue(card.Id, out var button))
            return;

        button.Text = card.IsRevealed
            ? card.PairKey is { } pairKey ? (pairKey + 1).ToString() : "?"
            : "";

        button.Disabled = model.IsLocked || card.IsRevealed || card.IsMatched;
        button.Modulate = card.IsMatched
            ? new Color(1, 1, 1, 0.6f)
            : Colors.White;
    }

    private void OnCardPress(int x, int y)
    {
        _logger.LogDebug("{OnCardPressName} ({X}, {Y}).", nameof(OnCardPress), x, y);
        _gameAdapter.FlipCardAt(x, y);
        Update();
    }

    public void OnGameWon(int moves, int totalCards) =>
        _logger.LogInformation("You won after {WonEventMoves} moves! 🎉", moves);

    public override void _ExitTree()
    {
        _newGameButton.Pressed -= StartNewGame;

        _gameAdapter.SaveGame();
        _gameAdapter.Dispose();
    }
}
