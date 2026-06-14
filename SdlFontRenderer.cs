using System.Numerics;
using FontStashSharp;
using FontStashSharp.Interfaces;
using Silk.NET.Maths;
using Silk.NET.SDL;
using DrawingPoint = System.Drawing.Point;
using DrawingRectangle = System.Drawing.Rectangle;

namespace TheAdventure;
// AI-generated
public unsafe class SdlFontRenderer : IFontStashRenderer, IDisposable
{
    private readonly Sdl _sdl;
    private readonly Renderer* _renderer;
    private readonly SdlFontTextureManager _textureManager;

    public ITexture2DManager TextureManager => _textureManager;

    public SdlFontRenderer(Sdl sdl, Renderer* renderer)
    {
        _sdl = sdl;
        _renderer = renderer;
        _textureManager = new SdlFontTextureManager(sdl, renderer);
    }

    public void Draw(
        object texture,
        Vector2 position,
        DrawingRectangle? sourceRectangle,
        FSColor color,
        float rotation,
        Vector2 scale,
        float depth)
    {
        var fontTexture = (SdlFontTexture)texture;
        var source = sourceRectangle ?? new DrawingRectangle(
            0,
            0,
            fontTexture.Width,
            fontTexture.Height
        );

        var sourceRect = new Rectangle<int>(
            source.X,
            source.Y,
            source.Width,
            source.Height
        );
        var destinationRect = new Rectangle<int>(
            (int)MathF.Round(position.X),
            (int)MathF.Round(position.Y),
            (int)MathF.Round(source.Width * scale.X),
            (int)MathF.Round(source.Height * scale.Y)
        );

        _sdl.SetTextureColorMod(fontTexture.Texture, color.R, color.G, color.B);
        _sdl.SetTextureAlphaMod(fontTexture.Texture, color.A);

        var center = new Silk.NET.SDL.Point();
        _sdl.RenderCopyEx(
            _renderer,
            fontTexture.Texture,
            in sourceRect,
            in destinationRect,
            rotation * 180.0 / Math.PI,
            in center,
            RendererFlip.None
        );
    }

    public void Dispose()
    {
        _textureManager.Dispose();
        GC.SuppressFinalize(this);
    }
}

internal unsafe sealed class SdlFontTextureManager : ITexture2DManager, IDisposable
{
    private readonly Sdl _sdl;
    private readonly Renderer* _renderer;
    private readonly List<SdlFontTexture> _textures = new();
    private bool _disposed;

    public SdlFontTextureManager(Sdl sdl, Renderer* renderer)
    {
        _sdl = sdl;
        _renderer = renderer;
    }

    public object CreateTexture(int width, int height)
    {
        var texture = _sdl.CreateTexture(
            _renderer,
            (uint)PixelFormatEnum.Rgba32,
            (int)TextureAccess.Streaming,
            width,
            height
        );

        if (texture == null)
        {
            throw new InvalidOperationException("Failed to create font texture.");
        }

        _sdl.SetTextureBlendMode(texture, BlendMode.Blend);
        var fontTexture = new SdlFontTexture(texture, width, height);
        _textures.Add(fontTexture);
        return fontTexture;
    }

    public DrawingPoint GetTextureSize(object texture)
    {
        var fontTexture = (SdlFontTexture)texture;
        return new DrawingPoint(fontTexture.Width, fontTexture.Height);
    }

    public void SetTextureData(
        object texture,
        DrawingRectangle bounds,
        byte[] data)
    {
        var fontTexture = (SdlFontTexture)texture;
        var destination = new Rectangle<int>(
            bounds.X,
            bounds.Y,
            bounds.Width,
            bounds.Height
        );

        fixed (byte* dataPointer = data)
        {
            int result = _sdl.UpdateTexture(
                fontTexture.Texture,
                in destination,
                dataPointer,
                bounds.Width * 4
            );

            if (result < 0)
            {
                throw new InvalidOperationException("Failed to update font texture.");
            }
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        foreach (var texture in _textures)
        {
            _sdl.DestroyTexture(texture.Texture);
        }

        _textures.Clear();
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}

internal unsafe sealed class SdlFontTexture
{
    public Texture* Texture { get; }
    public int Width { get; }
    public int Height { get; }

    public SdlFontTexture(Texture* texture, int width, int height)
    {
        Texture = texture;
        Width = width;
        Height = height;
    }
}
// end AI-generated