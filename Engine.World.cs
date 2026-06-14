using System.Text.Json;
using Silk.NET.Maths;
using TheAdventure.Models;
using TheAdventure.Models.Data;

namespace TheAdventure;

public partial class Engine
{
    public void SetupWorld()
    {
        _player = new PlayerObject(
            SpriteSheet.Load(_renderer, "Player.json", "Assets"),
            100,
            100
        );

        var levelContent = File.ReadAllText(
            Path.Combine("Assets", "terrain.tmj")
        );
        var level = JsonSerializer.Deserialize<Level>(levelContent)
            ?? throw new InvalidOperationException("Failed to load level.");

        if (level.Width == null || level.Height == null)
        {
            throw new InvalidOperationException("Invalid level dimensions.");
        }

        if (level.TileWidth == null || level.TileHeight == null)
        {
            throw new InvalidOperationException("Invalid tile dimensions.");
        }

        foreach (var tileSetReference in level.TileSets)
        {
            var tileSetContent = File.ReadAllText(
                Path.Combine("Assets", tileSetReference.Source)
            );
            var tileSet = JsonSerializer.Deserialize<TileSet>(tileSetContent)
                ?? throw new InvalidOperationException(
                    "Failed to load tile set."
                );

            tileSet.TextureId = _renderer.LoadTexture(
                Path.Combine("Assets", tileSet.Image),
                out _
            );
            _loadedTileSets.Add((
                tileSetReference.FirstGID ?? 1,
                tileSet
            ));
        }

        _loadedTileSets.Sort((left, right) =>
            left.FirstGid.CompareTo(right.FirstGid)
        );

        _currentLevel = level;
        LoadCollisionObjectsFromLevel();
        InitializeCollectibles();

        _renderer.SetCameraWorldBounds(new Rectangle<int>(
            0,
            0,
            GetMapWidthInPixels(),
            GetMapHeightInPixels()
        ));

        LoadGame();
    }

    public bool IsBlocked(Rectangle<int> rectangle)
    {
        return _collisionObjects.Any(collision =>
            Intersects(rectangle, collision.Bounds)
        );
    }

    private static bool Intersects(Rectangle<int> first, Rectangle<int> second)
    {
        return first.Origin.X < second.Origin.X + second.Size.X &&
               first.Origin.X + first.Size.X > second.Origin.X &&
               first.Origin.Y < second.Origin.Y + second.Size.Y &&
               first.Origin.Y + first.Size.Y > second.Origin.Y;
    }

    public int GetMapWidthInPixels()
    {
        return (_currentLevel.Width ?? 0) * (_currentLevel.TileWidth ?? 0);
    }

    public int GetMapHeightInPixels()
    {
        return (_currentLevel.Height ?? 0) * (_currentLevel.TileHeight ?? 0);
    }

    public void RenderTerrain()
    {
        foreach (var layer in _currentLevel.Layers)
        {
            if (layer.Type != "tilelayer" || layer.Name == "Collision")
            {
                continue;
            }

            int mapTileWidth = _currentLevel.TileWidth ?? 0;
            int mapTileHeight = _currentLevel.TileHeight ?? 0;

            for (int x = 0; x < (_currentLevel.Width ?? 0); x++)
            {
                for (int y = 0; y < (_currentLevel.Height ?? 0); y++)
                {
                    int dataIndex = y * (layer.Width ?? 0) + x;
                    int? gid = layer.Data[dataIndex];

                    if (gid is null or 0)
                    {
                        continue;
                    }

                    var resolvedTileSet = ResolveTileSet(gid.Value);
                    if (resolvedTileSet == null)
                    {
                        continue;
                    }

                    var tileSet = resolvedTileSet.Value.TileSet;
                    int localTileId = resolvedTileSet.Value.LocalTileId;
                    int tileWidth = tileSet.TileWidth ?? 0;
                    int tileHeight = tileSet.TileHeight ?? 0;
                    int columns = tileSet.Columns ?? 1;

                    int column = localTileId % columns;
                    int row = localTileId / columns;
                    var source = new Rectangle<int>(
                        column * tileWidth,
                        row * tileHeight,
                        tileWidth,
                        tileHeight
                    );
                    var destination = new Rectangle<int>(
                        x * mapTileWidth,
                        (y + 1) * mapTileHeight - tileHeight,
                        tileWidth,
                        tileHeight
                    );

                    _renderer.RenderTexture(
                        tileSet.TextureId,
                        source,
                        destination
                    );
                }
            }
        }
    }

    private void LoadCollisionObjectsFromLevel()
    {
        _collisionObjects.Clear();

        var collisionLayer = _currentLevel.Layers.FirstOrDefault(layer =>
            layer.Name == "Collision"
        );
        if (collisionLayer == null)
        {
            return;
        }

        int tileWidth = _currentLevel.TileWidth ?? 0;
        int tileHeight = _currentLevel.TileHeight ?? 0;

        for (int y = 0; y < (collisionLayer.Height ?? 0); y++)
        {
            for (int x = 0; x < (collisionLayer.Width ?? 0); x++)
            {
                int index = y * (collisionLayer.Width ?? 0) + x;
                int? tileValue = collisionLayer.Data[index];

                if (tileValue is null or 0)
                {
                    continue;
                }

                _collisionObjects.Add(new CollisionObject(
                    new Rectangle<int>(
                        x * tileWidth,
                        y * tileHeight,
                        tileWidth,
                        tileHeight
                    )
                ));
            }
        }
    }

    private (TileSet TileSet, int LocalTileId)? ResolveTileSet(int gid)
    {
        if (gid == 0)
        {
            return null;
        }

        (int FirstGid, TileSet TileSet)? bestMatch = null;
        foreach (var entry in _loadedTileSets)
        {
            if (gid < entry.FirstGid)
            {
                break;
            }

            bestMatch = entry;
        }

        return bestMatch == null
            ? null
            : (
                bestMatch.Value.TileSet,
                gid - bestMatch.Value.FirstGid
            );
    }

}
