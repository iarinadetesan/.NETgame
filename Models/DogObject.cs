namespace TheAdventure.Models;


public class DogObject : AnimalObject
{
    public DogObject(SpriteSheet spriteSheet, int x, int y)
        : base(spriteSheet, x, y, speed: 58.0)
    {
    }

    public override string AnimalName => "Dog";
    public override int Points => 10;
}

