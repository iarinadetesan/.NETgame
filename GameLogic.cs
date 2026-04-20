using System.Collections;
using Silk.NET.Maths;

namespace TheAdventure;

public class GameLogic
{
    
    private int _frameCount;
    private List<GameObject> _gameObjects = new();

    public void ProcessFrame()
    {
        var renderableObject = (RenderableGameObject)_gameObjects.First();
        var i = _frameCount % 10;  // column
        var j = _frameCount / 10;  // row
        var cellWidth = renderableObject.TextureInformation.Width / 10;
        var cellHeight = renderableObject.TextureInformation.Height / 10;

        renderableObject.TextureSource = new Rectangle<int>(i * cellWidth, j * cellHeight, cellWidth, cellHeight);
        renderableObject.TextureDestination = new Rectangle<int>(0, 0, cellWidth, cellHeight);

        if (++_frameCount == 100) _frameCount = 0;
    }
    public void InitializeGame(GameRenderer gameRenderer)
    {
        var textureId = gameRenderer.LoadTexture("image.png", out var textureInfo);
        var sampleRenderableObject = new RenderableGameObject(textureId,
            new Rectangle<int>(0, 0, textureInfo.Width, textureInfo.Height),
            new Rectangle<int>(0, 0, textureInfo.Width, textureInfo.Height), textureInfo);
        _gameObjects.Add(sampleRenderableObject);
    }

    public IEnumerable<RenderableGameObject> GetRenderables()
    {
        foreach (var obj in _gameObjects)
        {
            if (obj is RenderableGameObject renderable)
                yield return renderable;
        }
    }
}