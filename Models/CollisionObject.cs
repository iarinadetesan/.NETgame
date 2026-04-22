
using Silk.NET.Maths;

namespace TheAdventure.Models;

public class CollisionObject
{
    public Rectangle<int> Bounds { get; }

    public CollisionObject(Rectangle<int> bounds)
    {
        Bounds = bounds;
    }
}