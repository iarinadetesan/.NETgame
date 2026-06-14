namespace TheAdventure.Models;

public interface ICatchable
{
    bool IsCaught { get; }
    int Points { get; }

    void Catch();
}