using Silk.NET.Maths;
using TheAdventure.Models;
using TheAdventure.Models.Data;

namespace TheAdventure;

public partial class Engine
{
    private const double RoundDurationSeconds = 15.0;

    private static readonly string SaveFilePath = Path.GetFullPath(
        Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "savegame.json"
        )
    );

    private readonly Input _input;
    private readonly GameRenderer _renderer;
    private readonly Dictionary<int, GameObject> _gameObjects = new();
    private readonly List<(int FirstGid, TileSet TileSet)> _loadedTileSets = new();
    private readonly List<CollisionObject> _collisionObjects = new();
    private readonly List<CollectibleObject> _collectibles = new();
    private readonly List<AnimalObject> _animals = new();
    private readonly Dictionary<string, int> _caughtAnimals = new();

    private Level _currentLevel = new();
    private PlayerObject? _player;
    private DateTimeOffset _lastUpdate;
    private double _timeRemaining = RoundDurationSeconds;
    private int _score;
    private int _highScore;
    private int _money;

    public GameState State { get; private set; } = GameState.MainMenu;

    public Engine(GameRenderer renderer, Input input)
    {
        _renderer = renderer;
        _input = input;
        _input.OnMouseClick += (_, coordinates) =>
            HandleMouseClick(coordinates.x, coordinates.y);
    }

    public void ProcessFrame()
    {
        if (State != GameState.Playing)
        {
            return;
        }

        var currentTime = DateTimeOffset.Now;
        double elapsedMilliseconds = (currentTime - _lastUpdate).TotalMilliseconds;
        _lastUpdate = currentTime;

        _timeRemaining -= elapsedMilliseconds / 1000.0;
        if (_timeRemaining <= 0)
        {
            EndGame();
            return;
        }

        double up = _input.IsWPressed() ? 1.0 : 0.0;
        double down = _input.IsSPressed() ? 1.0 : 0.0;
        double left = _input.IsAPressed() ? 1.0 : 0.0;
        double right = _input.IsDPressed() ? 1.0 : 0.0;

        _player?.UpdatePosition(
            up,
            down,
            left,
            right,
            24,
            24,
            elapsedMilliseconds,
            this
        );

        foreach (var animal in _animals)
        {
            animal.Update(elapsedMilliseconds / 1000.0, this);
        }
    }

    public void RenderFrame()
    {
        switch (State)
        {
            case GameState.MainMenu:
                RenderMainMenu();
                return;
            case GameState.GameOver:
                RenderGameOver();
                return;
            case GameState.Playing:
                RenderPlayingState();
                return;
            default:
                throw new InvalidOperationException(
                    $"Unsupported game state: {State}."
                );
        }
    }

    private void RenderMainMenu()
    {
        _renderer.SetDrawColor(82, 146, 88, 255);
        _renderer.ClearScreen();
        _renderer.RenderMainMenu();
        _renderer.PresentFrame();
    }

    private void RenderGameOver()
    {
        _renderer.SetDrawColor(214, 226, 174, 255);
        _renderer.ClearScreen();
        _renderer.RenderGameOver(
            _score,
            _highScore,
            _caughtAnimals.Values.Sum(),
            _caughtAnimals.GetValueOrDefault("Fox"),
            _caughtAnimals.GetValueOrDefault("Dog"),
            _caughtAnimals.GetValueOrDefault("Cat"),
            _caughtAnimals.GetValueOrDefault("Bunny"),
            _money
        );
        _renderer.PresentFrame();
    }
// AI-generated
    private void RenderPlayingState()
    {
        if (_player == null)
        {
            throw new InvalidOperationException("The player is not initialized.");
        }

        _renderer.CameraLookAt(_player.Position.X, _player.Position.Y);
        _renderer.SetDrawColor(255, 255, 255, 255);
        _renderer.ClearScreen();

        RenderTerrain();
        RenderAllObjects();
        _renderer.RenderGameStatus(
            Math.Max(0, (int)Math.Ceiling(_timeRemaining)),
            _score,
            _money,
            _player.SpeedBoostRemainingSeconds
        );
        _renderer.PresentFrame();
    }
// end AI-generated
    private void HandleMouseClick(int screenX, int screenY)
    {
        if (State == GameState.MainMenu)
        {
            if (_renderer.IsPlayButtonClicked(screenX, screenY))
            {
                StartRound();
            }

            return;
        }

        if (State == GameState.GameOver)
        {
            if (_renderer.IsRetryButtonClicked(screenX, screenY))
            {
                StartRound();
            }

            return;
        }

        if (State == GameState.Playing)
        {
            if (!TryCatchAnimalAtPlayer())
            {
                TryCollectAtPlayer();
            }
        }
    }

    private void StartRound()
    {
        State = GameState.Playing;
        _score = 0;
        _caughtAnimals.Clear();
        _timeRemaining = RoundDurationSeconds;
        _player?.ResetForRound(100, 100);
        SpawnAnimals();
        RandomizeCollectibles();
        _lastUpdate = DateTimeOffset.Now;
    }

    private void EndGame()
    {
        _timeRemaining = 0;
        State = GameState.GameOver;
        _highScore = Math.Max(_highScore, _score);

        SaveGame();
        Console.WriteLine(
            $"Game over! Score: {_score}. High score: {_highScore}. " +
            $"Animals caught: {_caughtAnimals.Values.Sum()}."
        );
    }
}
