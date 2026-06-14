
using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using FontStashSharp;
using Silk.NET.Maths;
using Silk.NET.SDL;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using TheAdventure.Models;

namespace TheAdventure;
public class GameRenderer : IDisposable
{
    
    private readonly Sdl _sdl;
    private readonly IntPtr _renderer;
    
    
    
    private readonly Dictionary<int, IntPtr> _texturePointers = new();
    private readonly Dictionary<int, TextureData> _textureInformation = new();
    private readonly Dictionary<string, int> _textureIdsByPath =
        new(StringComparer.OrdinalIgnoreCase);
    private int _index = 0;
    
    private DateTimeOffset _lastFrameRenderedAt = DateTimeOffset.MinValue;
    
    private readonly Camera _camera ;

    private readonly int _playButtonTextureId;
    private readonly TextureData _playButtonTextureData;
    private readonly Rectangle<int> _playButtonBounds;
    private readonly Rectangle<int> _retryButtonBounds;

    private readonly FontSystem _fontSystem;
    private readonly SdlFontRenderer _fontRenderer;
    private bool _disposed;
    
    public GameRenderer(Sdl sdl, GameWindow gameWindow)
    {
        _sdl = sdl;
        _renderer = gameWindow.CreateRenderer();
        
        
        var windowSize = gameWindow.Size;

        _camera = new Camera(windowSize.Width, windowSize.Height);
        _camera.LookAt(0, 0);

        _playButtonTextureId = LoadTexture(
            Path.Combine(
                "Assets",
                "Sprout Lands - UI Pack - Basic pack",
                "Sprite sheets",
                "UI Big Play Button.png"
            ),
            out _playButtonTextureData
        );
        // AI-generated
        const int playButtonWidth = 288;
        const int playButtonHeight = 96;
        _playButtonBounds = new Rectangle<int>(
            (_camera.Width - playButtonWidth) / 2,
            (_camera.Height - playButtonHeight) / 2,
            playButtonWidth,
            playButtonHeight
        );
        _retryButtonBounds = new Rectangle<int>(
            (_camera.Width - 216) / 2,
            _camera.Height - 88,
            216,
            72
        );

        unsafe
        {
            _fontRenderer = new SdlFontRenderer(sdl, (Renderer*)_renderer);
        }

        _fontSystem = new FontSystem(new FontSystemSettings
        {
            FontResolutionFactor = 1,
            KernelWidth = 1,
            KernelHeight = 1
        });
        _fontSystem.AddFont(File.ReadAllBytes(Path.Combine(
            "Assets",
            "Sprout Lands - UI Pack - Basic pack",
            "fonts",
            "pixelFont-7-8x14-sproutLands.ttf"
        )));
        // end AI-generated
    }
   
    
    public unsafe int LoadTexture(string fileName, out TextureData textureData)

    {
        string fullPath = Path.GetFullPath(fileName);
        if (_textureIdsByPath.TryGetValue(fullPath, out int existingId))
        {
            textureData = _textureInformation[existingId];
            return existingId;
        }

        using var fStream = new FileStream(
            fullPath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read
        );
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
            _sdl.FreeSurface(imageSurface);
        }

        _texturePointers[_index] = (IntPtr)imageTexture;
        _textureInformation[_index] = textureData;
        _textureIdsByPath[fullPath] = _index;
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

    public void RenderMainMenu()
    {
        RenderCenteredText("ANIMAL RESCUE", 120, 3);
        RenderCenteredText("Oh, no! Pets are on the loose!", 180, 2);
        RenderCenteredText("Let's bring them back to their owners!", 220, 2);
        RenderPlayButton(_playButtonBounds);
    }

    private void RenderPlayButton(Rectangle<int> bounds)
    {
        int frameWidth = _playButtonTextureData.Width / 2;
        int frameHeight = _playButtonTextureData.Height / 2;
        var playButtonSource = new Rectangle<int>(
            0,
            frameHeight,
            frameWidth,
            frameHeight
        );

        RenderUiTexture(
            _playButtonTextureId,
            playButtonSource,
            bounds
        );
    }

    public bool IsPlayButtonClicked(int mouseX, int mouseY)
    {
        return PointInRect(mouseX, mouseY, _playButtonBounds);
    }

    public void RenderGameStatus(
        int secondsRemaining,
        int score,
        int money,
        double speedBoostRemainingSeconds)
    {
        RenderText($"TIME {secondsRemaining}", 20, 20, 2);
        RenderText($"SCORE {score}", 20, 52, 2);
        RenderText($"MONEY {money}", 20, 84, 2);

        if (speedBoostRemainingSeconds > 0)
        {
            int boostSeconds = (int)Math.Ceiling(speedBoostRemainingSeconds);
            RenderText($"SPEED BOOST {boostSeconds}", 20, 116, 2);
        }
    }

    public void RenderGameOver(
        int score,
        int highScore,
        int totalCaught,
        int foxesCaught,
        int dogsCaught,
        int catsCaught,
        int bunniesCaught,
        int money)
    {
        RenderCenteredText("GAME OVER", 80, 4);
        RenderCenteredText($"SCORE {score}", 160, 3);
        RenderCenteredText($"HIGH SCORE {highScore}", 205, 3);
        RenderCenteredText($"TOTAL CAUGHT {totalCaught}", 275, 2);
        RenderCenteredText($"FOXES {foxesCaught}", 325, 2);
        RenderCenteredText($"DOGS {dogsCaught}", 365, 2);
        RenderCenteredText($"CATS {catsCaught}", 405, 2);
        RenderCenteredText($"BUNNIES {bunniesCaught}", 445, 2);
        RenderCenteredText($"MONEY {money}", 485, 2);
        
        RenderPlayButton(_retryButtonBounds);
    }

    public bool IsRetryButtonClicked(int mouseX, int mouseY)
    {
        return PointInRect(mouseX, mouseY, _retryButtonBounds);
    }

    public void RenderText(string text, int x, int y, int scale = 2)
    {
        var font = _fontSystem.GetFont(14 * scale);
        font.DrawText(
            _fontRenderer,
            text,
            new Vector2(x, y),
            FSColor.Black
        );
    }

    private void RenderCenteredText(string text, int y, int scale)
    {
        var font = _fontSystem.GetFont(14 * scale);
        var size = font.MeasureString(text);
        int x = (int)MathF.Round((_camera.Width - size.X) / 2.0f);
        font.DrawText(
            _fontRenderer,
            text,
            new Vector2(x, y),
            FSColor.Black
        );
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

    public unsafe void RenderUiTexture(int textureId, Rectangle<int> src, Rectangle<int> dst)
    {
        if (_texturePointers.TryGetValue(textureId, out var texture))
        {
            _sdl.RenderCopy((Renderer*)_renderer, (Texture*)texture, in src, in dst);
        }
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
// AI-generated
    public unsafe void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _fontRenderer.Dispose();

        foreach (var texture in _texturePointers.Values)
        {
            if (texture != IntPtr.Zero)
            {
                _sdl.DestroyTexture((Texture*)texture);
            }
        }

        _texturePointers.Clear();
        _textureInformation.Clear();
        _textureIdsByPath.Clear();

        if (_renderer != IntPtr.Zero)
        {
            _sdl.DestroyRenderer((Renderer*)_renderer);
        }

        _disposed = true;
        GC.SuppressFinalize(this);
    }
    // end AI-generated
}
