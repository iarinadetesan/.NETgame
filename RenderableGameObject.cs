using Silk.NET.Maths;
using Silk.NET.SDL;
namespace TheAdventure;


public class RenderableGameObject : GameObject
{
    public int TextureId { get; }
    public Rectangle<int> TextureSource { get; set; }
    public Rectangle<int> TextureDestination { get; set; }
    public GameRenderer.TextureInfo TextureInformation { get; }
    
    public RenderableGameObject(
        int textureId,
        Rectangle<int> source,
        Rectangle<int> destination,
        GameRenderer.TextureInfo textureInfo)
    {
        TextureId = textureId;
        TextureSource = source;
        TextureDestination = destination;
        TextureInformation = textureInfo;
    }
}