namespace TheAdventure.Models;


public class CatObject : AnimalObject
{
    public CatObject(SpriteSheet spriteSheet, int x, int y)
        : base(spriteSheet, x, y, speed: 80.0)
    {
    }

    public override string AnimalName => "Cat";
    public override int Points => 15;
}
