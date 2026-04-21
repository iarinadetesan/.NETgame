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

    private readonly Dictionary<string, TileSet> _loadedTileSets = new();
    private readonly Dictionary<int, Tile> _tileIdMap = new();
    private Level _currentLevel = new();
    
    
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

        foreach (var tileSetRef in level.TileSets)
        {
            var tileSetContent = File.ReadAllText(Path.Combine("Assets", tileSetRef.Source));
            var tileSet = JsonSerializer.Deserialize<TileSet>(tileSetContent);
            if (tileSet == null)
            {
                throw new Exception("Failed to load tile set");
            }

            tileSet.TextureId = GameRenderer.LoadTexture(Path.Combine("Assets", tileSet.Image), out _);
            _loadedTileSets.Add(tileSet.Name, tileSet);
            
        }

        _currentLevel = level;
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
            foreach (var tileSetRef in _currentLevel.TileSets)
            {
                var tileSet = _loadedTileSets.Values.FirstOrDefault();
                if (tileSet == null)
                {
                    continue;
                }

                int tileWidth = tileSet.TileWidth ?? 0;
                int tileHeight = tileSet.TileHeight ?? 0;
                int columns = tileSet.Columns ?? 1;

                for (int i = 0; i < (_currentLevel.Width ?? 0); ++i)
                {
                    for (int j = 0; j < (_currentLevel.Height ?? 0); ++j)
                    {
                        int dataIndex = j * (currentLayer.Width ?? 0) + i;
                        var tileValue = currentLayer.Data[dataIndex];

                        if (tileValue == null || tileValue.Value == 0)
                        {
                            continue;
                        }

                        int currentTileId = tileValue.Value - 1;

                        int column = currentTileId % columns;
                        int row = currentTileId / columns;

                        var sourceRect = new Silk.NET.Maths.Rectangle<int>(
                            column * tileWidth,
                            row * tileHeight,
                            tileWidth,
                            tileHeight
                        );

                        var destRect = new Silk.NET.Maths.Rectangle<int>(
                            i * tileWidth,
                            j * tileHeight,
                            tileWidth,
                            tileHeight
                        );

                        renderer.RenderTexture(tileSet.TextureId, sourceRect, destRect);
                    }
                }

                break;
            }
        }
    }
    
    
    public void UpdatePlayerPosition(double up, double down, double left, double right, int timeSinceLastUpdateInMs)
    {
        _player?.UpdatePosition(up, down, left, right, timeSinceLastUpdateInMs);
    }

    public (int X, int Y) GetPlayerPosition()
    {
        if (_player == null)
        {
            return (0, 0);
        }

        return (_player.X, _player.Y);
    }
}