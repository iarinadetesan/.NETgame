using TheAdventure.Models;

namespace TheAdventure;

public partial class Engine
{
    private const double GemTimeBonusSeconds = 10.0;
    private const int CollectibleSize = 24;
    private const int SpawnEdgeMargin = 32;
    private const int MinimumPlayerDistance = 96;
    private const int MaximumSpawnAttempts = 300;

    private sealed record CollectibleSpawnDefinition(
        string ItemType,
        string TexturePath,
        int MinimumCount,
        int MaximumCount
    );

    private static readonly CollectibleSpawnDefinition[] SpawnDefinitions =
    {
        new("Coin", "coin.png", 6, 12),
        new("Apple", "apple.png", 2, 5),
        new("Gem", "gem.png", 1, 3)
    };

    private void InitializeCollectibles()
    {
        _collectibles.Clear();

        foreach (var definition in SpawnDefinitions)
        {
            for (int index = 0; index < definition.MaximumCount; index++)
            {
                _collectibles.Add(new CollectibleObject(
                    _renderer,
                    definition.ItemType,
                    definition.TexturePath,
                    0,
                    0
                ));
            }
        }
    }
// AI-generated
    private void RandomizeCollectibles()
    {
        var occupiedBounds = new List<Silk.NET.Maths.Rectangle<int>>();

        foreach (var collectible in _collectibles)
        {
            collectible.Deactivate();
        }

        foreach (var definition in SpawnDefinitions)
        {
            int count = Random.Shared.Next(
                definition.MinimumCount,
                definition.MaximumCount + 1
            );
            var collectiblesToSpawn = _collectibles
                .Where(collectible =>
                    collectible.ItemType == definition.ItemType
                )
                .Take(count);

            foreach (var collectible in collectiblesToSpawn)
            {
                var position = FindCollectibleSpawnPosition(occupiedBounds);
                collectible.ResetPosition(position.X, position.Y);
                occupiedBounds.Add(collectible.Bounds);
            }
        }
    }

    private (int X, int Y) FindCollectibleSpawnPosition(
        IReadOnlyCollection<Silk.NET.Maths.Rectangle<int>> occupiedBounds)
    {
        int tileWidth = _currentLevel.TileWidth
            ?? throw new InvalidOperationException("Missing tile width.");
        int tileHeight = _currentLevel.TileHeight
            ?? throw new InvalidOperationException("Missing tile height.");

        int minimumColumn = DivideRoundUp(SpawnEdgeMargin, tileWidth);
        int minimumRow = DivideRoundUp(SpawnEdgeMargin, tileHeight);
        int maximumColumn =
            (GetMapWidthInPixels() - CollectibleSize - SpawnEdgeMargin) /
            tileWidth;
        int maximumRow =
            (GetMapHeightInPixels() - CollectibleSize - SpawnEdgeMargin) /
            tileHeight;

        if (maximumColumn < minimumColumn || maximumRow < minimumRow)
        {
            throw new InvalidOperationException(
                "The map is too small for collectible spawning."
            );
        }

        for (int attempt = 0; attempt < MaximumSpawnAttempts; attempt++)
        {
            int x = Random.Shared.Next(
                minimumColumn,
                maximumColumn + 1
            ) * tileWidth;
            int y = Random.Shared.Next(
                minimumRow,
                maximumRow + 1
            ) * tileHeight;
            var bounds = new Silk.NET.Maths.Rectangle<int>(
                x,
                y,
                CollectibleSize,
                CollectibleSize
            );

            bool overlapsCollectible = occupiedBounds.Any(existing =>
                Intersects(bounds, existing)
            );
            bool overlapsAnimal = _animals.Any(animal =>
                Intersects(bounds, animal.CollisionBounds)
            );

            if (!IsBlocked(bounds) &&
                !overlapsCollectible &&
                !overlapsAnimal &&
                !IsNearPlayer(bounds))
            {
                return (x, y);
            }
        }

        throw new InvalidOperationException(
            "Could not find a valid collectible spawn position."
        );
    }
//end AI-generated
    private bool IsNearPlayer(Silk.NET.Maths.Rectangle<int> bounds)
    {
        if (_player == null)
        {
            return false;
        }

        var playerBounds = _player.CollisionBounds;
        var protectedArea = new Silk.NET.Maths.Rectangle<int>(
            playerBounds.Origin.X - MinimumPlayerDistance,
            playerBounds.Origin.Y - MinimumPlayerDistance,
            playerBounds.Size.X + MinimumPlayerDistance * 2,
            playerBounds.Size.Y + MinimumPlayerDistance * 2
        );

        return Intersects(bounds, protectedArea);
    }

    // AI-generated
    private static int DivideRoundUp(int value, int divisor)
    {
        return (value + divisor - 1) / divisor;
    }
    // end AI-generated
    public bool TryCollectAtPlayer()
    {
        if (_player == null)
        {
            return false;
        }

        var playerBounds = _player.CollisionBounds;
        var collectionArea = new Silk.NET.Maths.Rectangle<int>(
            playerBounds.Origin.X - 12,
            playerBounds.Origin.Y - 12,
            playerBounds.Size.X + 24,
            playerBounds.Size.Y + 24
        );

        foreach (var collectible in _collectibles)
        {
            if (collectible.IsCollected ||
                !Intersects(collectionArea, collectible.Bounds))
            {
                continue;
            }

            collectible.Collect();
            ApplyCollectibleEffect(collectible.ItemType);
            return true;
        }

        return false;
    }

    private void ApplyCollectibleEffect(string itemType)
    {
        switch (itemType)
        {
            case "Coin":
                _money++;
                Console.WriteLine($"Coin collected! Money: {_money}.");
                break;
            case "Gem":
                _timeRemaining += GemTimeBonusSeconds;
                Console.WriteLine(
                    $"Gem collected! +{GemTimeBonusSeconds:0} seconds."
                );
                break;
            case "Apple":
                _player?.ApplySpeedBoost();
                Console.WriteLine("Apple collected! Speed boosted for 5 seconds.");
                break;
            default:
                throw new InvalidOperationException(
                    $"Unknown collectible type: {itemType}."
                );
        }
    }

    private void LoadGame()
    {
        if (!File.Exists(SaveFilePath))
        {
            return;
        }

        var saveData = SaveData.Load(SaveFilePath);
        _highScore = saveData.HighScore;
        _money = saveData.Money;
    }

    public void SaveGame()
    {
        var saveData = new SaveData
        {
            HighScore = _highScore,
            Money = _money
        };

        saveData.Save(SaveFilePath);
    }
}
