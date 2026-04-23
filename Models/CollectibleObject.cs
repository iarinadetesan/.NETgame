namespace TheAdventure.Models;

using System.IO;
using Silk.NET.Maths;



public class CollectibleObject : GameObject
{
    public string ItemType { get; }
    public bool IsCollected { get; private set; }

    public int X { get; }
    public int Y { get; }

    private readonly int _textureId;
    private readonly Rectangle<int> _source;
    private Rectangle<int> _target;

    private const int SpriteWidth = 16;
    private const int SpriteHeight = 16;
    private const int RenderSize = 24;

    public CollectibleObject(int id, string itemType, string texturePath, int x, int y) : base(id)
    {
        ItemType = itemType;
        X = x;
        Y = y;

        _textureId = GameRenderer.LoadTexture(Path.Combine("Assets", texturePath), out _);

        _source = new Rectangle<int>(0, 0, SpriteWidth, SpriteHeight);
        _target = new Rectangle<int>(X, Y, RenderSize, RenderSize);
    }
    
    public Rectangle<int> Bounds =>
        new Rectangle<int>(X, Y, RenderSize, RenderSize);

    public void Collect()
    {
        IsCollected = true;
    }

    public void Render(GameRenderer renderer)
    {
        if (IsCollected)
        {
            return;
        }

        renderer.RenderTexture(_textureId, _source, _target);
    }
}