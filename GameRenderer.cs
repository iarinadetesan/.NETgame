
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
        _camera.X = playerPos.X;
        _camera.Y = playerPos.Y;
        
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
}