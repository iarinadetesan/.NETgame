using Silk.NET.Maths;

namespace TheAdventure;

public class GameCamera
{
    public int X { get; set; }
    public int Y { get; set; }

    public int Width { get; set; }
    public int Height { get; set; }

    public float Zoom { get; set; } = 1.0f;
    

    
    public Rectangle<int> ToScreenCoordinates(Rectangle<int> rect)
    {
        float screenX = Width / 2f + (rect.Origin.X - X) * Zoom;
        float screenY = Height / 2f + (rect.Origin.Y - Y) * Zoom;

        return new Rectangle<int>(
            (int)MathF.Round(screenX),
            (int)MathF.Round(screenY),
            (int)MathF.Round(rect.Size.X * Zoom),
            (int)MathF.Round(rect.Size.Y * Zoom)
        );
    }

    public Vector2D<int> ToWorldCoordinates(Vector2D<int> point)
    {
        float worldX = (point.X - Width / 2f) / Zoom + X;
        float worldY = (point.Y - Height / 2f) / Zoom + Y;

        return new Vector2D<int>(
            (int)MathF.Round(worldX),
            (int)MathF.Round(worldY)
        );
    }
    

}