namespace TheAdventure.Models;


public class FoxObject : AnimalObject
{
    public FoxObject(SpriteSheet spriteSheet, int x, int y)
        : base(spriteSheet, x, y, speed: 72.0)
    {
    }

    public override string AnimalName => "Fox";
    public override int Points => 30;
}

