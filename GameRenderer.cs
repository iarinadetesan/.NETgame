
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
    
    private readonly Sdl _sdl;
    private readonly IntPtr _renderer;
    
    
    
    private readonly Dictionary<int, IntPtr> _texturePointers = new();
    private readonly Dictionary<int, TextureData> _textureInformation = new();
    private int _index = 0;
    
    private DateTimeOffset _lastFrameRenderedAt = DateTimeOffset.MinValue;
    
    private readonly Camera _camera ;
    
    
    private readonly Dictionary<string, int> _uiItemTextures = new();
    
    
    private readonly int _hotbarTextureId;
    private readonly TextureData _hotbarTextureData;
    private int _selectedHotbarIndex = 0;
    
    private Silk.NET.Maths.Rectangle<int> _zoomInButton = new(700, 20, 40, 40);
    private Silk.NET.Maths.Rectangle<int> _zoomOutButton = new(750, 20, 40, 40);

    private readonly int _zoomButtonsTextureId;
    private readonly TextureData _zoomButtonsTextureData;
    private readonly float[] _zoomLevels = { 1.0f, 2.0f, 3.0f , 4.0f};
    private int _zoomLevelIndex = 0;
    
    public GameRenderer(Sdl sdl, GameWindow gameWindow)
    {
        _sdl = sdl;
        _renderer = gameWindow.CreateRenderer();
        
        
        var windowSize = gameWindow.Size;

        _camera = new Camera(windowSize.Width, windowSize.Height);
        _camera.Zoom = _zoomLevels[_zoomLevelIndex];
        _camera.LookAt(0, 0);


        
        _hotbarTextureId = LoadTexture(Path.Combine("Assets", "Sprite sheet for Basic Pack.png"), out _hotbarTextureData);
        _zoomButtonsTextureId =
            LoadTexture(Path.Combine("Assets", "Sprite sheet for Basic Pack.png"), out _zoomButtonsTextureData);
    }
   
    
    public unsafe int LoadTexture(string fileName, out TextureData textureData)

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
    
    public unsafe void SetDrawColor(byte r, byte g, byte b, byte a)
    {
        _sdl.SetRenderDrawColor((Renderer*)_renderer, r, g, b, a);
    }

    public unsafe void ClearScreen()
    {
        _sdl.RenderClear((Renderer*)_renderer);
    }

    public unsafe void PresentFrame()
    {
        _sdl.RenderPresent((Renderer*)_renderer);
    }

    public void CameraLookAt(int x, int y)
    {
        _camera.LookAt(x, y);
    }

    public void RenderUi(List<HotbarSlot> slots)
    {
        RenderHotbar(slots);
        RenderZoomButtons();
    }


    

    
    
    
    public unsafe void RenderTexture(
        int textureId,
        Rectangle<int> src,
        Rectangle<int> dst,
        RendererFlip flip = RendererFlip.None,
        double angle = 0.0,
        Silk.NET.SDL.Point center = default)
    {
        if (_texturePointers.TryGetValue(textureId, out var texture))
        {
            var translatedDst = _camera.ToScreenCoordinates(dst);

            _sdl.RenderCopyEx(
                (Renderer*)_renderer,
                (Texture*)texture,
                in src,
                in translatedDst,
                angle,
                in center,
                flip
            );
        }
    }

    
    public (int X, int Y) ToWorldCoordinates(int x, int y)
    {
        var worldCoords = _camera.ToWorldCoordinates(new(x, y));
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
    
    private unsafe void RenderZoomButtons()
    {
        var zoomInSrc = new Silk.NET.Maths.Rectangle<int>(837, 130, 30, 30);
        var zoomOutSrc = new Silk.NET.Maths.Rectangle<int>(837, 160, 30, 30);

        RenderUiTexture(_zoomButtonsTextureId, zoomInSrc, _zoomInButton);
        RenderUiTexture(_zoomButtonsTextureId, zoomOutSrc, _zoomOutButton);
    }
 
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
    
    private unsafe void RenderHotbar(List<HotbarSlot> slots)
    {
        const int slotCount = 5;
        const int uiSlotSize = 48;
        const int renderSlotSize = 64;
        const int spacing = -10;
        const int bottomMargin = 20;

        int totalWidth = slotCount * renderSlotSize + (slotCount - 1) * spacing;
        int startX = (_camera.Width - totalWidth) / 2;
        int y = _camera.Height - renderSlotSize - bottomMargin;
        

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
    
    public void ZoomIn()
    {
        if (_zoomLevelIndex < _zoomLevels.Length - 1)
        {
            _zoomLevelIndex++;
            _camera.Zoom = _zoomLevels[_zoomLevelIndex];
        }
    }

    public void ZoomOut()
    {
        if (_zoomLevelIndex > 0)
        {
            _zoomLevelIndex--;
            _camera.Zoom = _zoomLevels[_zoomLevelIndex];
        }
    }
    
    public bool IsZoomInButtonClicked(int mouseX, int mouseY)
    {
        return PointInRect(mouseX, mouseY, _zoomInButton);
    }

    public bool IsZoomOutButtonClicked(int mouseX, int mouseY)
    {
        return PointInRect(mouseX, mouseY, _zoomOutButton);
    }

    private bool PointInRect(int x, int y, Silk.NET.Maths.Rectangle<int> rect)
    {
        return x >= rect.Origin.X &&
               x <= rect.Origin.X + rect.Size.X &&
               y >= rect.Origin.Y &&
               y <= rect.Origin.Y + rect.Size.Y;
    }
    public void SetCameraWorldBounds(Rectangle<int> bounds)
    {
        _camera.SetWorldBounds(bounds);
    }

}