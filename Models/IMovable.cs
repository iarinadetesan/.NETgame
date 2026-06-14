namespace TheAdventure.Models;


public interface IMovable
{
    void Update(double elapsedSeconds, Engine engine);
}

