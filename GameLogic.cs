using System.Collections;
using System.Text.Json;
using Silk.NET.Maths;
using TheAdventure.Models;
using TheAdventure.Models.Data;
namespace TheAdventure;

public class GameLogic
{
    
    
    private Dictionary<int,GameObject> _gameObjects = new();
    private int _bombIds = 100;

    
    private readonly List<(int FirstGid, TileSet TileSet)> _loadedTileSets = new();
    private readonly Dictionary<int, Tile> _tileIdMap = new();
    private Level _currentLevel = new();
    
    private readonly List<CollisionObject> _collisionObjects = new();
    
    private PlayerObject? _player;
    
    public void ProcessFrame()
    {
        
    }

    public IEnumerable<RenderableGameObject> GetRenderables()
    {
        foreach (var obj in _gameObjects.Values)
        {
            if (obj is RenderableGameObject renderable)
                yield return renderable;
        }
    }
    
    public IEnumerable<CollisionObject> GetCollisionObjects()
    {
        return _collisionObjects;
    }
    
    public void RenderAllObjects(int timeSinceLastFrame, GameRenderer renderer)
    {
        List<int> itemsToRemove = new List<int>();
        foreach (var gameObject in GetRenderables())
        {
            if (gameObject.Update(timeSinceLastFrame))
            {
                gameObject.Render(renderer);
            }
            else
            {
                itemsToRemove.Add(gameObject.Id);
            }
        }

        foreach (var item in itemsToRemove)
        {
            _gameObjects.Remove(item);
        }
        _player?.Render(renderer);
    }
    
    public void AddBomb(int x, int y)
    {
        AnimatedGameObject bomb = new AnimatedGameObject(
            Path.Combine("Assets", "BombExploding.png"),
            2, 
            _bombIds, 
            81, 
            9, 
            9, 
            x, 
            y);
        _gameObjects.Add(bomb.Id, bomb);
        ++_bombIds;
    }
    
    
    public void InitializeGame()
    {
        _player = new PlayerObject(1000);
        
        var levelContent = File.ReadAllText(Path.Combine("Assets", "terrain.tmj"));
        var level = JsonSerializer.Deserialize<Level>(levelContent);
        if (level == null)
        {
            throw new Exception("Failed to load level");
        }
       // _collisionObjects.Add(new CollisionObject(new Silk.NET.Maths.Rectangle<int>(200, 200, 32, 32)));
       // _collisionObjects.Add(new CollisionObject(new Silk.NET.Maths.Rectangle<int>(250, 200, 32, 32)));
       // _collisionObjects.Add(new CollisionObject(new Silk.NET.Maths.Rectangle<int>(300, 200, 32, 32)));
        foreach (var tileSetRef in level.TileSets)
        {
            var tileSetContent = File.ReadAllText(Path.Combine("Assets", tileSetRef.Source));
            var tileSet = JsonSerializer.Deserialize<TileSet>(tileSetContent);
            if (tileSet == null)
            {
                throw new Exception("Failed to load tile set");
            }

            tileSet.TextureId = GameRenderer.LoadTexture(Path.Combine("Assets", tileSet.Image), out _);
            _loadedTileSets.Add((tileSetRef.FirstGID ?? 1, tileSet));
            _loadedTileSets.Sort((a, b) => a.FirstGid.CompareTo(b.FirstGid));
            
        }

        
        _currentLevel = level;
        
        LoadCollisionObjectsFromLevel();
    }
    
    public bool IsBlocked(Silk.NET.Maths.Rectangle<int> rect)
    {
        foreach (var collisionObject in _collisionObjects)
        {
            if (Intersects(rect, collisionObject.Bounds))
            {
                return true;
            }
        }

        return false;
    }

    private bool Intersects(Silk.NET.Maths.Rectangle<int> a, Silk.NET.Maths.Rectangle<int> b)
    {
        return a.Origin.X < b.Origin.X + b.Size.X &&
               a.Origin.X + a.Size.X > b.Origin.X &&
               a.Origin.Y < b.Origin.Y + b.Size.Y &&
               a.Origin.Y + a.Size.Y > b.Origin.Y;
    }
    public int GetMapWidthInPixels()
    {
        return (_currentLevel.Width ?? 0) * (_currentLevel.TileWidth ?? 0);
    }

    public int GetMapHeightInPixels()
    {
        return (_currentLevel.Height ?? 0) * (_currentLevel.TileHeight ?? 0);
    }
    
    public void RenderTerrain(GameRenderer renderer)
    {
        foreach (var currentLayer in _currentLevel.Layers)
        {
            if (currentLayer.Name == "Collision")
            {
                continue;
            }

            int mapTileWidth = _currentLevel.TileWidth ?? 0;
            int mapTileHeight = _currentLevel.TileHeight ?? 0;
            
            
            for (int i = 0; i < (_currentLevel.Width ?? 0); ++i)
            {
                for (int j = 0; j < (_currentLevel.Height ?? 0); ++j)
                {
                    int dataIndex = j * (currentLayer.Width ?? 0) + i;
                    var gid = currentLayer.Data[dataIndex];

                    if (gid == null || gid.Value == 0)
                    {
                        continue;
                    }

                    var resolved = ResolveTileSet(gid.Value);
                    if (resolved == null)
                    {
                        continue;
                    }

                    var tileSet = resolved.Value.TileSet;
                    int localTileId = resolved.Value.LocalTileId;

                    int tileWidth = tileSet.TileWidth ?? 0;
                    int tileHeight = tileSet.TileHeight ?? 0;
                    int columns = tileSet.Columns ?? 1;

                    int column = localTileId % columns;
                    int row = localTileId / columns;

                    var sourceRect = new Silk.NET.Maths.Rectangle<int>(
                        column * tileWidth,
                        row * tileHeight,
                        tileWidth,
                        tileHeight
                    );

                    var destRect = new Silk.NET.Maths.Rectangle<int>(
                        i * mapTileWidth,
                        (j + 1) * mapTileHeight - tileHeight,
                        tileWidth,
                        tileHeight
                    );

                    renderer.RenderTexture(tileSet.TextureId, sourceRect, destRect);
                }
            }
        }
    }
    
    
    public void UpdatePlayerPosition(double up, double down, double left, double right, int timeSinceLastUpdateInMs)
    {
        _player?.UpdatePosition(up, down, left, right, timeSinceLastUpdateInMs, this); //sterg this daca vreau ca inainte
    }
    
    public (int X, int Y) GetPlayerPosition()
    {
        if (_player == null)
        {
            return (0, 0);
        }

        return (_player.X, _player.Y);
    }

    public Rectangle<int> GetPlayerCollisionBounds()
    {
        return _player.CollisionBounds;
    }
    
    private void LoadCollisionObjectsFromLevel()
    {
        _collisionObjects.Clear();

        var collisionLayer = _currentLevel.Layers.FirstOrDefault(l => l.Name == "Collision");
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
                var tileValue = collisionLayer.Data[index];

                if (tileValue == null || tileValue.Value == 0)
                {
                    continue;
                }

                var bounds = new Silk.NET.Maths.Rectangle<int>(
                    x * tileWidth,
                    y * tileHeight,
                    tileWidth,
                    tileHeight
                );

                _collisionObjects.Add(new TheAdventure.Models.CollisionObject(bounds));
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
            if (gid >= entry.FirstGid)
            {
                bestMatch = entry;
            }
            else
            {
                break;
            }
        }

        if (bestMatch == null)
        {
            return null;
        }

        int localTileId = gid - bestMatch.Value.FirstGid;
        return (bestMatch.Value.TileSet, localTileId);
    }
    
}