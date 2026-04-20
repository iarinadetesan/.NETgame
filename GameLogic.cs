using System.Collections;
using Silk.NET.Maths;
using TheAdventure.Models;

namespace TheAdventure;

public class GameLogic
{
    
    
    private Dictionary<int,GameObject> _gameObjects = new();
    private int _bombIds = 100;

    public void ProcessFrame()
    {
        
    }

    public IEnumerable<RenderableGameObject> GetRenderables()
    {
        foreach (var obj in _gameObjects.Values)
        {
            if (obj is RenderableGameObject renderable)
                yield return renderable;
        }
    }
    public void RenderAllObjects(int timeSinceLastFrame, GameRenderer renderer)
    {
        List<int> itemsToRemove = new List<int>();
        foreach (var gameObject in GetRenderables())
        {
            if (gameObject.Update(timeSinceLastFrame))
            {
                gameObject.Render(renderer);
            }
            else
            {
                itemsToRemove.Add(gameObject.Id);
            }
        }

        foreach (var item in itemsToRemove)
        {
            _gameObjects.Remove(item);
        }
    }
    
    public void AddBomb(int x, int y)
    {
        AnimatedGameObject bomb = new AnimatedGameObject(
            "spritesheet1.png", 
            2, 
            _bombIds, 
            81, 
            9, 
            9, 
            x, 
            y);
        _gameObjects.Add(bomb.Id, bomb);
        ++_bombIds;
    }
}