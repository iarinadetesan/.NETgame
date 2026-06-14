using Silk.NET.Maths;
using TheAdventure.Models;

namespace TheAdventure;

public partial class Engine
{
    private sealed record AnimalSpawnDefinition(
        string SpriteSheetFile,
        int MinimumCount,
        int MaximumCount,
        Func<SpriteSheet, int, int, AnimalObject> CreateAnimal
    );

    private static readonly AnimalSpawnDefinition[] AnimalSpawnDefinitions =
    {
        new(
            "Fox.json",
            0,
            2,
            (spriteSheet, x, y) => new FoxObject(spriteSheet, x, y)
        ),
        new(
            "Dog.json",
            1,
            3,
            (spriteSheet, x, y) => new DogObject(spriteSheet, x, y)
        ),
        new(
            "Cat.json",
            1,
            3,
            (spriteSheet, x, y) => new CatObject(spriteSheet, x, y)
        ),
        new(
            "Bunny.json",
            1,
            4,
            (spriteSheet, x, y) => new BunnyObject(spriteSheet, x, y)
        )
    };
// AI-generated
    private void SpawnAnimals()
    {
        _animals.Clear();

        foreach (var definition in AnimalSpawnDefinitions)
        {
            int count = Random.Shared.Next(
                definition.MinimumCount,
                definition.MaximumCount + 1
            );
            SpawnAnimalsOfType(
                count,
                definition.SpriteSheetFile,
                definition.CreateAnimal
            );
        }
    }

    private void SpawnAnimalsOfType(
        int count,
        string spriteSheetFile,
        Func<SpriteSheet, int, int, AnimalObject> createAnimal)
    {
        for (int i = 0; i < count; i++)
        {
            var spawnPosition = FindAnimalSpawnPosition();
            var spriteSheet = SpriteSheet.Load(
                _renderer,
                spriteSheetFile,
                "Assets"
            );
            _animals.Add(
                createAnimal(spriteSheet, spawnPosition.X, spawnPosition.Y)
            );
        }
    }

    private (int X, int Y) FindAnimalSpawnPosition()
    {
        const int animalSize = 24;
        const int edgeMargin = 32;
        const int maximumAttempts = 200;

        int maximumX = Math.Max(
            edgeMargin,
            GetMapWidthInPixels() - animalSize - edgeMargin
        );
        int maximumY = Math.Max(
            edgeMargin,
            GetMapHeightInPixels() - animalSize - edgeMargin
        );

        for (int attempt = 0; attempt < maximumAttempts; attempt++)
        {
            int x = Random.Shared.Next(edgeMargin, maximumX + 1);
            int y = Random.Shared.Next(edgeMargin, maximumY + 1);
            var bounds = new Rectangle<int>(x, y, animalSize, animalSize);

            bool overlapsAnotherAnimal = _animals.Any(animal =>
                Intersects(bounds, animal.CollisionBounds)
            );

            if (!IsBlocked(bounds) &&
                !overlapsAnotherAnimal &&
                !IsNearPlayer(bounds))
            {
                return (x, y);
            }
        }

        throw new InvalidOperationException(
            "Could not find a valid animal spawn position."
        );
    }
// end AI-generated
    private bool TryCatchAnimalAtPlayer()
    {
        if (_player == null)
        {
            return false;
        }

        var playerBounds = _player.CollisionBounds;
        var catchArea = new Rectangle<int>(
            playerBounds.Origin.X - 12,
            playerBounds.Origin.Y - 12,
            playerBounds.Size.X + 24,
            playerBounds.Size.Y + 24
        );

        // AI-generated
        var animal = _animals.FirstOrDefault(candidate =>
            !candidate.IsCaught &&
            Intersects(catchArea, candidate.CollisionBounds)
        );
        // end AI-generated
        if (animal == null)
        {
            return false;
        }

        animal.Catch();
        _animals.Remove(animal);
        _score += animal.Points;

        if (!_caughtAnimals.TryAdd(animal.AnimalName, 1))
        {
            _caughtAnimals[animal.AnimalName]++;
        }

        Console.WriteLine(
            $"{animal.AnimalName} caught! +{animal.Points} points"
        );
        return true;
    }
}
