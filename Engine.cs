using System.Collections;
using System.Text.Json;
using Silk.NET.Maths;
using TheAdventure.Models;
using TheAdventure.Models.Data;
namespace TheAdventure;

public class Engine
{
    private static readonly string SaveFilePath =
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "savegame.json"));


    private Input _input;
    
    private Dictionary<int,GameObject> _gameObjects = new();

    
    private readonly List<(int FirstGid, TileSet TileSet)> _loadedTileSets = new();
    private readonly Dictionary<int, Tile> _tileIdMap = new();
    private Level _currentLevel = new();
    
    private readonly List<CollisionObject> _collisionObjects = new();
    
    private PlayerObject? _player;
    
    private readonly List<CollectibleObject> _collectibles = new();
    private readonly Dictionary<string, int> _inventory = new();
    
    
    public IReadOnlyDictionary<string, int> Inventory => _inventory;
    
    private readonly GameRenderer _renderer;
    private DateTimeOffset _lastFrameRenderedAt = DateTimeOffset.MinValue;
    private DateTimeOffset _lastUpdate;


    public Engine(GameRenderer renderer, Input input)
    {
        _renderer = renderer;
        _input = input;
        _input.OnMouseClick += (_, coords) => AddBomb(coords.x, coords.y);
    }

    public void ProcessFrame()
    {
        var currentTime = DateTimeOffset.Now;
        var msSinceLastFrame=(currentTime - _lastUpdate).TotalMilliseconds;
        _lastUpdate = currentTime;
        
        double up = _input.IsWPressed() ? 1.0 : 0.0;
        double down = _input.IsSPressed() ? 1.0 : 0.0;
        double left = _input.IsAPressed() ? 1.0 : 0.0;
        double right = _input.IsDPressed() ? 1.0 : 0.0;
        _player?.UpdatePosition(up, down, left, right, 24,24,(int)msSinceLastFrame,this);
        
        if (_input.IsEPressed())
        {
            TryCollectAtPlayer();
        }
    }
    
    public void RenderFrame()
    {
       /* if (_player != null)
        {
            _renderer.CameraLookAt(_player.X, _player.Y);
        } */
        var playerPosition = _player!.Position;
        _renderer.CameraLookAt(playerPosition.X, playerPosition.Y);
        _renderer.SetDrawColor(255, 255, 255, 255);
        _renderer.ClearScreen();

        var timeSinceLastFrame = 0.0;
        var now = DateTimeOffset.UtcNow;

        if (_lastFrameRenderedAt > DateTimeOffset.MinValue)
        {
            timeSinceLastFrame = now.Subtract(_lastFrameRenderedAt).TotalMilliseconds;

        }

        RenderTerrain();
        RenderAllObjects();

        _renderer.RenderUi(GetHotbarSlots());

        _lastFrameRenderedAt = now;
        _renderer.PresentFrame();
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
    
    public void RenderAllObjects()
    {
        List<int> itemsToRemove = new List<int>();
        foreach (var gameObject in GetRenderables())
        {
            gameObject.Render(_renderer);
            if (gameObject is TemporaryGameObject {IsExpired: true} tempGameObject)
            {
                itemsToRemove.Add(gameObject.Id);
            }
        }

        foreach (var item in itemsToRemove)
        {
            _gameObjects.Remove(item);
        }
        
        foreach (var collectible in _collectibles)
        {
            collectible.Render(_renderer);
        }
        
        _player?.Render(_renderer);
    }
    
    private void AddBomb(int screenX, int screenY)
    {
        var worldCoords = _renderer.ToWorldCoordinates(screenX, screenY);
        SpriteSheet spriteSheet = SpriteSheet.Load(_renderer, "BombExploding.json", 
            "Assets");
        spriteSheet.ActivateAnimation("Explode");
        TemporaryGameObject bomb = new(spriteSheet, 2.1, (worldCoords.X, 
            worldCoords.Y));
        _gameObjects.Add(bomb.Id, bomb);
    }
    
    
    public void SetupWorld()
    {
        
       // _player = new PlayerObject(_renderer);
        _player = new(SpriteSheet.Load(_renderer, "Player.json", "Assets"), 100, 100);
        
        var levelContent = File.ReadAllText(Path.Combine("Assets", "terrain.tmj"));
        var level = JsonSerializer.Deserialize<Level>(levelContent);
        if (level == null)
        {
            throw new Exception("Failed to load level");
        }
        
        
        if (level.Width == null || level.Height == null)
        {
            throw new Exception("Invalid level dimensions");
        }
        if (level.TileWidth == null || level.TileHeight == null)
        {
            throw new Exception("Invalid tile dimensions");
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

            tileSet.TextureId = _renderer.LoadTexture(Path.Combine("Assets", tileSet.Image), out _);
            _loadedTileSets.Add((tileSetRef.FirstGID ?? 1, tileSet));
            _loadedTileSets.Sort((a, b) => a.FirstGid.CompareTo(b.FirstGid));
            
        }

        /*_collectibles.Add(new CollectibleObject(_collectibleIds++, "Gem", "gem.png", 180, 180));
        _collectibles.Add(new CollectibleObject(_collectibleIds++, "Coin", "coin.png", 260, 220));
        _collectibles.Add(new CollectibleObject(_collectibleIds++, "Apple", "apple.png", 320, 260));*/
        _currentLevel = level;
        LoadCollisionObjectsFromLevel();
        LoadCollectiblesFromLevel();
        _renderer.SetCameraWorldBounds(new Rectangle<int>(
            0,
            0,
            GetMapWidthInPixels(),
            GetMapHeightInPixels()
        ));
        
        LoadGame();

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
    
    public void RenderTerrain()
    {
        foreach (var currentLayer in _currentLevel.Layers)
        {
            if (currentLayer.Type != "tilelayer")
            {
                continue;
            }
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

                    _renderer.RenderTexture(tileSet.TextureId, sourceRect, destRect);
                }
            }
        }
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
    public void TryCollectAtPlayer()
    {
        if (_player == null)
        {
            return;
        }

        var playerBounds = _player.CollisionBounds;

        foreach (var collectible in _collectibles)
        {
            if (collectible.IsCollected)
            {
                continue;
            }

            if (Intersects(playerBounds, collectible.Bounds))
            {
                collectible.Collect();

                if (!_inventory.ContainsKey(collectible.ItemType))
                {
                    _inventory[collectible.ItemType] = 0;
                }

                _inventory[collectible.ItemType]++;
                
                Console.WriteLine($"{collectible.ItemType} collected!");
                Console.WriteLine($"Gem: {_inventory.GetValueOrDefault("Gem", 0)}");
                Console.WriteLine($"Apple: {_inventory.GetValueOrDefault("Apple", 0)}");
                Console.WriteLine($"Coin: {_inventory.GetValueOrDefault("Coin", 0)}");
                break;
            }
        }
    }
    
    
    private void LoadCollectiblesFromLevel()
    {
        _collectibles.Clear();

        var collectiblesLayer = _currentLevel.Layers.FirstOrDefault(l => l.Name == "Collectibles");
        if (collectiblesLayer == null)
        {
            return;
        }

        foreach (var obj in collectiblesLayer.Objects)
        {
            string itemType = obj.Name;

            if (string.IsNullOrWhiteSpace(itemType))
            {
                continue;
            }

            int x = (int)(obj.X ?? 0);
            int y = (int)(obj.Y ?? 0);

            string texturePath = itemType switch
            {
                "Gem" => "gem.png",
                "Apple" => "apple.png",
                "Coin" => "coin.png",
                _ => ""
            };

            if (string.IsNullOrWhiteSpace(texturePath))
            {
                continue;
            }

            _collectibles.Add(new CollectibleObject(
                _renderer,

                itemType,
                texturePath,
                x,
                y
            ));
        }
    }
    
    
    public List<(string ItemType, int Count)> GetHotbarItems(int maxSlots = 5)
    {
        var items = new List<(string ItemType, int Count)>();

        foreach (var kvp in _inventory)
        {
            if (kvp.Value > 0)
            {
                items.Add((kvp.Key, kvp.Value));
            }

            if (items.Count == maxSlots)
            {
                break;
            }
        }

        return items;
    }
    
    public List<HotbarSlot> GetHotbarSlots(int maxSlots = 5)
    {
        var slots = new List<HotbarSlot>();

        foreach (var kvp in _inventory)
        {
            if (kvp.Value <= 0)
            {
                continue;
            }

            slots.Add(new HotbarSlot
            {
                ItemType = kvp.Key,
                Count = kvp.Value
            });

            if (slots.Count == maxSlots)
            {
                break;
            }
        }

        while (slots.Count < maxSlots)
        {
            slots.Add(new HotbarSlot());
        }

        return slots;
    }
    
    private void LoadGame()
    {
        if (!File.Exists(SaveFilePath))
        {
            return;
        }

        var saveData = SaveData.Load(SaveFilePath);

        _inventory.Clear();

        foreach (var item in saveData.Inventory)
        {
            _inventory[item.Key] = item.Value;
        }
    }

    
    public void SaveGame()
    {
        var saveData = new SaveData
        {
            Inventory = new Dictionary<string, int>(_inventory)
        };

        saveData.Save(SaveFilePath);
    }



}