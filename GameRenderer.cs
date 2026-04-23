
using System;
using System.Collections.Generic;
using System.IO;
using Silk.NET.Maths;
using Silk.NET.SDL;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using TheAdventure.Models;

namespace TheAdventure;
public class GameRenderer
{
    private static GameRenderer? _instance;
    
    private readonly Sdl _sdl;
    private readonly IntPtr _renderer;
    private readonly GameLogic _gameLogic;
    
    
    private readonly Dictionary<int, IntPtr> _texturePointers = new();
    private readonly Dictionary<int, TextureData> _textureInformation = new();
    private int _index = 0;
    
    private DateTimeOffset _lastFrameRenderedAt = DateTimeOffset.MinValue;
    
    private readonly GameCamera _camera = new();
    
    
    private readonly Dictionary<string, int> _uiItemTextures = new();
    
    
    private readonly int _hotbarTextureId;
    private readonly TextureData _hotbarTextureData;
    private int _selectedHotbarIndex = 0;
    
    public GameRenderer(Sdl sdl, GameWindow gameWindow, GameLogic gameLogic)
    {
        _sdl = sdl;
        _renderer = gameWindow.CreateRenderer();
        _gameLogic = gameLogic;
        _instance = this;
        
        _camera.X = 0;
        _camera.Y = 0;

        var windowSize = gameWindow.Size;
        _camera.Width = windowSize.Width;
        _camera.Height = windowSize.Height;
        
        _hotbarTextureId = LoadTexture(Path.Combine("Assets", "Sprite sheet for Basic Pack.png"), out _hotbarTextureData);
        
    }
    public static int LoadTexture(string fileName, out TextureData textureData)
    {
        return _instance!._LoadTextureInternal(fileName, out textureData);
    }
    
    private unsafe int _LoadTextureInternal(string fileName, out TextureData textureData)
    {
        using var fStream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var image = Image.Load<Rgba32>(fStream);
        
        textureData = new TextureData()
        {
            Width = image.Width, 
            Height = image.Height
        };

        var imageRawData = new byte[textureData.Width * textureData.Height * 4];
        image.CopyPixelDataTo(imageRawData.AsSpan());
        Texture* imageTexture;
        
        fixed (byte* data = imageRawData)
        {
            var imageSurface = _sdl.CreateRGBSurfaceWithFormatFrom(
                data, textureData.Width, 
                textureData.Height,
                8, 
                textureData.Width * 4, 
                (uint)PixelFormatEnum.Rgba32);
            imageTexture = _sdl.CreateTextureFromSurface((Renderer*)_renderer, imageSurface);
            _sdl.FreeSurface(imageSurface); // surface is only needed to create the texture, free it immediately
        }

        _texturePointers[_index] = (IntPtr)imageTexture;
        _textureInformation[_index] = textureData;
        return _index++;
    }
    
    
    public unsafe void Render()
    {
        
        var playerPos = _gameLogic.GetPlayerPosition();

        int mapWidth = _gameLogic.GetMapWidthInPixels();
        int mapHeight = _gameLogic.GetMapHeightInPixels();

        int halfScreenWidth = _camera.Width / 2;
        int halfScreenHeight = _camera.Height / 2;

        _camera.X = Math.Clamp(playerPos.X, halfScreenWidth, mapWidth - halfScreenWidth);
        _camera.Y = Math.Clamp(playerPos.Y, halfScreenHeight, mapHeight - halfScreenHeight);
        
        var renderer = (Renderer*)_renderer;
        
        _sdl.SetRenderDrawColor(renderer, 255, 255, 255, 255);
        _sdl.RenderClear(renderer);

        var timeSinceLastFrame = 0;
        var now = DateTimeOffset.UtcNow;

        if (_lastFrameRenderedAt > DateTimeOffset.MinValue)
        {
            timeSinceLastFrame = (int)now.Subtract(_lastFrameRenderedAt).TotalMilliseconds;
        }
        
        _gameLogic.RenderTerrain(this); //intai randam terenul, ca background!
        
        _gameLogic.RenderAllObjects(timeSinceLastFrame, this);
        
        //doar ca sa vad colision objects
       /* foreach (var col in _gameLogic.GetCollisionObjects())
        {
            RenderDebugRect(col.Bounds, 255, 0, 0);
        }

        var playerBounds = _gameLogic.GetPlayerCollisionBounds();
        RenderDebugRect(playerBounds, 0, 0, 255);*/
        
       RenderHotbar();
       
        _lastFrameRenderedAt = now;
        _sdl.RenderPresent(renderer);
    }

    public unsafe void RenderGameObject(RenderableGameObject gameObject)
    {
        var renderer = (Renderer*)_renderer;
        
        if (gameObject.TextureId > -1 &&
            _texturePointers.TryGetValue(gameObject.TextureId, out var texturePointer))
        {
            var textureDest = _camera.ToScreenCoordinates(gameObject.TextureDestination);
            _sdl.RenderCopyEx(
                renderer,
                (Texture*)texturePointer,
                gameObject.TextureSource,
                gameObject.TextureDestination,
                0,
                new Silk.NET.SDL.Point(0, 0),
                RendererFlip.None
            );
        }
    }
    
    
    public unsafe void RenderTexture(int textureId, Rectangle<int> src, Rectangle<int> dst)
    {
        if (_texturePointers.TryGetValue(textureId, out var texture))
        {
            var translatedDst = _camera.ToScreenCoordinates(dst);
            _sdl.RenderCopy((Renderer*)_renderer, (Texture*)texture, in src, in translatedDst);
        }
    }
    
    public static (int X, int Y) ToWorldCoordinates(int X, int Y)
    {
        if (_instance == null)
        {
            throw new InvalidOperationException("GameRenderer instance is not initialized.");
        }

        var worldCoords = _instance._camera.ToWorldCoordinates(new(X, Y));
        return (worldCoords.X, worldCoords.Y);
    }
    public unsafe void RenderDebugRect(Rectangle<int> rect, byte r, byte g, byte b)
    {
        var translated = _camera.ToScreenCoordinates(rect);

        _sdl.SetRenderDrawColor((Renderer*)_renderer, r, g, b, 255);
        _sdl.RenderDrawRect((Renderer*)_renderer, translated);
    }
    
    public unsafe void RenderUiTexture(int textureId, Rectangle<int> src, Rectangle<int> dst)
    {
        if (_texturePointers.TryGetValue(textureId, out var texture))
        {
            _sdl.RenderCopy((Renderer*)_renderer, (Texture*)texture, in src, in dst);
        }
    }
    
    private int GetUiTextureForItem(string itemType)
    {
        if (_uiItemTextures.TryGetValue(itemType, out var textureId))
        {
            return textureId;
        }

        string texturePath = itemType switch
        {
            
            "Apple" => "apple.png",
            "Gem" => "gem.png",
            "Coin" => "coin.png",
            _ => ""
        };

        if (string.IsNullOrWhiteSpace(texturePath))
        {
            return -1;
        }

        textureId = LoadTexture(Path.Combine("Assets", texturePath), out _);
        _uiItemTextures[itemType] = textureId;
        return textureId;
    }
    
    
 /* old method   
    private unsafe void RenderHotbar()
    {
        var renderer = (Renderer*)_renderer;

        const int slotCount = 5;
        const int slotSize = 48;
        const int spacing = 10;
        const int bottomMargin = 20;

        int totalWidth = slotCount * slotSize + (slotCount - 1) * spacing;
        int startX = (_camera.Width - totalWidth) / 2;
        int y = _camera.Height - slotSize - bottomMargin;

        var hotbarItems = _gameLogic.GetHotbarItems(slotCount);

        for (int i = 0; i < slotCount; i++)
        {
            int slotX = startX + i * (slotSize + spacing);
            var slotRect = new Silk.NET.Maths.Rectangle<int>(slotX, y, slotSize, slotSize);

            _sdl.SetRenderDrawColor(renderer, 40, 40, 40, 220);
            _sdl.RenderFillRect(renderer, slotRect);

            _sdl.SetRenderDrawColor(renderer, 255, 255, 255, 255);
            _sdl.RenderDrawRect(renderer, slotRect);

            if (i < hotbarItems.Count)
            {
                var item = hotbarItems[i];
                int textureId = GetUiTextureForItem(item.ItemType);

                if (textureId != -1)
                {
                    var src = new Silk.NET.Maths.Rectangle<int>(0, 0, 16, 16);
                    var dst = new Silk.NET.Maths.Rectangle<int>(slotX + 8, y + 8, 32, 32);
                    RenderUiTexture(textureId, src, dst);
                }

                DrawCountBars(item.Count, slotX, y, slotSize);
            }
        }
    }
    */
    private unsafe void DrawCountBars(int count, int slotX, int slotY, int slotSize)
    {
        var renderer = (Renderer*)_renderer;

        int barsToDraw = Math.Min(count, 5);

        for (int i = 0; i < barsToDraw; i++)
        {
            var barRect = new Silk.NET.Maths.Rectangle<int>(
                slotX + 6 + i * 7,
                slotY + slotSize - 10,
                5,
                4
            );

            _sdl.SetRenderDrawColor(renderer, 255, 215, 0, 255);
            _sdl.RenderFillRect(renderer, barRect);
        }
    }
    
    public void SetSelectedHotbarIndex(int index)
    {
        if (index < 0 || index > 4)
        {
            return;
        }

        _selectedHotbarIndex = index;
    }
    
    private unsafe void RenderHotbar()
    {
        const int slotCount = 5;
        const int uiSlotSize = 48;
        const int renderSlotSize = 64;
        const int spacing = -10;
        const int bottomMargin = 20;

        int totalWidth = slotCount * renderSlotSize + (slotCount - 1) * spacing;
        int startX = (_camera.Width - totalWidth) / 2;
        int y = _camera.Height - renderSlotSize - bottomMargin;

        var slots = _gameLogic.GetHotbarSlots(slotCount);

        for (int i = 0; i < slotCount; i++)
        {
            int slotX = startX + i * (renderSlotSize + spacing);

            var slotSrc = i == _selectedHotbarIndex
                ? new Silk.NET.Maths.Rectangle<int>(48, 0, 48, 48)
                : new Silk.NET.Maths.Rectangle<int>(0, 0, 48, 48);

            var slotDst = new Silk.NET.Maths.Rectangle<int>(slotX, y, renderSlotSize, renderSlotSize);

            RenderUiTexture(_hotbarTextureId, slotSrc, slotDst);

            var slot = slots[i];
            if (!string.IsNullOrWhiteSpace(slot.ItemType) && slot.Count > 0)
            {
                int itemTextureId = GetUiTextureForItem(slot.ItemType);
                if (itemTextureId != -1)
                {
                    var itemSrc = new Silk.NET.Maths.Rectangle<int>(0, 0, 16, 16);
                    var itemDst = new Silk.NET.Maths.Rectangle<int>(slotX + 16, y + 16, 32, 32);
                    RenderUiTexture(itemTextureId, itemSrc, itemDst);
                }

                DrawCountBars(slot.Count, slotX, y, renderSlotSize);
            }
        }
    }
    
}