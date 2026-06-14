using Silk.NET.Maths;

namespace TheAdventure;


public class Camera
{
    private int _x;
    private int _y;

    private Rectangle<int> _worldBounds = new();

    public int X => _x;
    public int Y => _y;

    public int Width { get;  }
    public int Height { get;  }

public Camera(int width, int height)
{
    Width = width;
    Height = height;
}
public void SetWorldBounds(Rectangle<int> bounds)
{
    var marginLeft = Width / 2;
    var marginTop = Height / 2;
    if (marginLeft * 2 > bounds.Size.X)
    {
        marginLeft = 48;
    }
    if (marginTop * 2 > bounds.Size.Y)
    {
        marginTop = 48;
    }
    _worldBounds = new Rectangle<int>(marginLeft, marginTop, bounds.Size.X - 
                                                             marginLeft * 2,
        bounds.Size.Y - marginTop * 2);
    _x = marginLeft;
    _y = marginTop;
}

    public void LookAt(int x, int y)
    {
        
        if (_worldBounds.Contains(new Vector2D<int>(_x, y)))
        {
            _y = 
                y;
        }
        if (_worldBounds.Contains(new Vector2D<int>(x, _y)))
        {
            _x = 
                x;
        }
    }

    public Rectangle<int> ToScreenCoordinates(Rectangle<int> rect)
    {
        int screenX = Width / 2 + rect.Origin.X - X;
        int screenY = Height / 2 + rect.Origin.Y - Y;

        return new Rectangle<int>(
            screenX,
            screenY,
            rect.Size.X,
            rect.Size.Y
        );
    }
}
