using Silk.NET.SDL;
namespace TheAdventure;
using System;
using System.Collections.Generic;
using System.IO;
using Silk.NET.SDL;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
public class GameRenderer
{
    private readonly Sdl _sdl;
    private readonly IntPtr _renderer;
    private readonly GameLogic _gameLogic;
    private readonly Dictionary<int, IntPtr> _texturePointers = new();
    private readonly Dictionary<int, TextureInfo> _textureInformation = new();
    private int _index = 0;
    public readonly struct TextureInfo
    {
        public int Width { get; init; }
        public int Height { get; init; }
        public int PixelDataSize => Width * Height * 4; // RGBA, 4 bytes per pixel
    }
    public GameRenderer(Sdl sdl, GameWindow gameWindow, GameLogic gameLogic)
    {
        _sdl = sdl;
        _renderer = gameWindow.CreateRenderer();
        _gameLogic = gameLogic;
    }
    
    
    public unsafe int LoadTexture(string fileName, out TextureInfo textureInfo)
    {
        using var fStream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var image = Image.Load<Rgba32>(fStream);
        
        textureInfo = new TextureInfo() { Width = image.Width, Height = image.Height };

        var imageRawData = new byte[textureInfo.PixelDataSize];
        image.CopyPixelDataTo(imageRawData.AsSpan());
        Texture* imageTexture;
        fixed (byte* data = imageRawData)
        {
            var imageSurface = _sdl.CreateRGBSurfaceWithFormatFrom(data, textureInfo.Width, textureInfo.Height,
                8, textureInfo.Width * 4, (uint)PixelFormatEnum.Rgba32);
            imageTexture = _sdl.CreateTextureFromSurface((Renderer*)_renderer, imageSurface);
            _sdl.FreeSurface(imageSurface); // surface is only needed to create the texture, free it immediately
        }

        _texturePointers[_index] = (IntPtr)imageTexture;
        _textureInformation[_index] = textureInfo;
        return _index++;
    }
    
    
    public unsafe void Render()
    {
        var renderer = (Renderer*)_renderer;
        _sdl.RenderClear(renderer);

        foreach (var renderable in _gameLogic.GetRenderables())
        {
            if (renderable.TextureId > -1 &&
                _texturePointers.TryGetValue(renderable.TextureId, out var texturePointer))
            {
                _sdl.RenderCopyEx(
                    renderer,
                    (Texture*)texturePointer,
                    renderable.TextureSource,
                    renderable.TextureDestination,
                    0,
                    new Silk.NET.SDL.Point(0, 0),
                    RendererFlip.None
                );
            }
        }

        _sdl.RenderPresent(renderer);
    }

}