namespace TheAdventure.Models;


public class BunnyObject : AnimalObject
{
    public BunnyObject(SpriteSheet spriteSheet, int x, int y)
        : base(spriteSheet, x, y, speed: 92.0)
    {
    }

    public override string AnimalName => "Bunny";
    public override int Points => 20;
}

