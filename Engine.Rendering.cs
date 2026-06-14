using TheAdventure.Models;

namespace TheAdventure;

public partial class Engine
{
    public IEnumerable<RenderableGameObject> GetRenderables()
    {
        foreach (var obj in _gameObjects.Values)
        {
            if (obj is RenderableGameObject renderable)
            {
                yield return renderable;
            }
        }
    }

    public IEnumerable<CollisionObject> GetCollisionObjects()
    {
        return _collisionObjects;
    }

    public void RenderAllObjects()
    {
        var itemsToRemove = new List<int>();
        foreach (var gameObject in GetRenderables())
        {
            gameObject.Render(_renderer);
            if (gameObject is TemporaryGameObject { IsExpired: true })
            {
                itemsToRemove.Add(gameObject.Id);
            }
        }

        foreach (var item in itemsToRemove)
        {
            _gameObjects.Remove(item);
        }

        foreach (var collectible in _collectibles)
        {
            collectible.Render(_renderer);
        }

        foreach (var animal in _animals)
        {
            animal.Render(_renderer);
        }

        _player?.Render(_renderer);
    }

}
