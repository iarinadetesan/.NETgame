using Silk.NET.Maths;
using Silk.NET.SDL;
using TheAdventure.Models;

namespace TheAdventure.Models;


public class RenderableGameObject : GameObject
{
    public int TextureId { get; }
    public Rectangle<int> TextureSource { get; set; }
    public Rectangle<int> TextureDestination { get; set; }
    public TextureData TextureInformation { get; }
    
    
    public SpriteSheet SpriteSheet { get; set; }
    public (int X, int Y) Position { get; set; }
    public double Angle { get; set; }
    public Point RotationCenter { get; set; }


    public RenderableGameObject(SpriteSheet spriteSheet, (int X, int Y)
            position, double angle = 0.0,
        Point rotationCenter = new())
        :
        base()
    {

        SpriteSheet = spriteSheet;
        Position = position;
        Angle = angle;
        RotationCenter = rotationCenter;
    }

    public virtual void Render(GameRenderer renderer)
    {
        SpriteSheet.Render(renderer, Position, Angle, RotationCenter);
    }

}