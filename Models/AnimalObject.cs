using Silk.NET.Maths;

namespace TheAdventure.Models;


public abstract class AnimalObject : RenderableGameObject, IMovable, ICatchable
{
    private const double MinimumDirectionTime = 1.2;
    private const double DirectionTimeVariation = 1.8;

    private static readonly (double X, double Y)[] Directions =
    {
        (1.0, 0.0),
        (-1.0, 0.0),
        (0.0, 1.0),
        (0.0, -1.0),
        (0.707, 0.707),
        (-0.707, 0.707),
        (0.707, -0.707),
        (-0.707, -0.707)
    };

    private readonly double _speed;
    private readonly int _collisionSize;
    private double _preciseX;
    private double _preciseY;
    private double _directionX;
    private double _directionY;
    private double _directionTimeRemaining;
    private string _currentAnimation = "MoveRight";

    protected AnimalObject(
        SpriteSheet spriteSheet,
        int x,
        int y,
        double speed,
        int collisionSize = 24)
        : base(spriteSheet, (x, y))
    {
        _speed = speed;
        _collisionSize = collisionSize;
        _preciseX = x;
        _preciseY = y;
        ChooseNewDirection();
    }

    public abstract string AnimalName { get; }
    public abstract int Points { get; }
    public bool IsCaught { get; private set; }

    public Rectangle<int> CollisionBounds =>
        new(Position.X, Position.Y, _collisionSize, _collisionSize);

    public void Catch()
    {
        IsCaught = true;
    }

    public void Update(double elapsedSeconds, Engine engine)
    {
        if (IsCaught)
        {
            return;
        }

        elapsedSeconds = Math.Min(elapsedSeconds, 0.1);
        _directionTimeRemaining -= elapsedSeconds;

        if (_directionTimeRemaining <= 0)
        {
            ChooseNewDirection();
        }

        double nextX = _preciseX + _directionX * _speed * elapsedSeconds;
        double nextY = _preciseY + _directionY * _speed * elapsedSeconds;
        int roundedX = (int)Math.Round(nextX);
        int roundedY = (int)Math.Round(nextY);
        var futureBounds = new Rectangle<int>(
            roundedX,
            roundedY,
            _collisionSize,
            _collisionSize
        );

        if (IsInsideMap(futureBounds, engine) && !engine.IsBlocked(futureBounds))
        {
            _preciseX = nextX;
            _preciseY = nextY;
            Position = (roundedX, roundedY);
            return;
        }

        ChooseNewDirection();
    }

    private void ChooseNewDirection()
    {
        var direction = Directions[Random.Shared.Next(Directions.Length)];
        _directionX = direction.X;
        _directionY = direction.Y;
        _directionTimeRemaining =
            MinimumDirectionTime + Random.Shared.NextDouble() * DirectionTimeVariation;

        string newAnimation = _directionX < 0 ? "MoveLeft" : "MoveRight";
        if (newAnimation != _currentAnimation)
        {
            _currentAnimation = newAnimation;
        }

        SpriteSheet.ActivateAnimation(_currentAnimation);
    }

    private static bool IsInsideMap(Rectangle<int> bounds, Engine engine)
    {
        return bounds.Origin.X >= 0 &&
               bounds.Origin.Y >= 0 &&
               bounds.Origin.X + bounds.Size.X <= engine.GetMapWidthInPixels() &&
               bounds.Origin.Y + bounds.Size.Y <= engine.GetMapHeightInPixels();
    }
}

