

namespace TheAdventure.Models;

using System.Threading;


public class GameObject
{
    public int Id { get; private set; }
    private static int _nextId = -1;
    public GameObject()
    {
        Id = Interlocked.Increment(ref _nextId);
    }
}