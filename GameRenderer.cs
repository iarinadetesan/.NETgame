
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
    
    public GameRenderer(Sdl sdl, GameWindow gameWindow, GameLogic gameLogic)
    {
        _sdl = sdl;
        _renderer = gameWindow.CreateRenderer();
        _gameLogic = gameLogic;
        _instance = this;
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

    public unsafe void RenderGameObject(RenderableGameObject renderableGameObject)
    {
        var renderer = (Renderer*)_renderer;

        if (renderableGameObject.TextureId > -1 &&
            _texturePointers.TryGetValue(renderableGameObject.TextureId, out var texturePointer))
        {
            _sdl.RenderCopyEx(
                renderer,
                (Texture*)texturePointer,
                renderableGameObject.TextureSource,
                renderableGameObject.TextureDestination,
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
            _sdl.RenderCopy((Renderer*)_renderer, (Texture*)texture, in src, in dst);
        }
    }
}