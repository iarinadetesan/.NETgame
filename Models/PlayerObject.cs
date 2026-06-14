using System;
using System.IO;
using Silk.NET.Maths;

namespace TheAdventure.Models;

public class PlayerObject : RenderableGameObject
{
    private const int BaseSpeed = 128;
    private const double SpeedBoostMultiplier = 1.5;
    private const double SpeedBoostDurationSeconds = 5.0;

    private string _currentAnimation = "IdleDown";
    private double _speedBoostRemainingSeconds;

    public Rectangle<int> CollisionBounds => new(Position.X, Position.Y, 32, 32);
    public double SpeedBoostRemainingSeconds => _speedBoostRemainingSeconds;

    public PlayerObject(SpriteSheet spriteSheet, int x, int y) : 
        base(spriteSheet, (x, y))
    {
        SpriteSheet.ActivateAnimation(_currentAnimation);
    }

    public void ApplySpeedBoost()
    {
        _speedBoostRemainingSeconds = SpeedBoostDurationSeconds;
    }

    public void ResetForRound(int x, int y)
    {
        Position = (x, y);
        _speedBoostRemainingSeconds = 0;
        _currentAnimation = "IdleDown";
        SpriteSheet.ActivateAnimation(_currentAnimation);
    }
    
    public void UpdatePosition(double up, double down, double left, double right, 
        int width, int height, double time, Engine engine)
    {
        double elapsedSeconds = time / 1000.0;
        _speedBoostRemainingSeconds = Math.Max(
            0.0,
            _speedBoostRemainingSeconds - elapsedSeconds
        );

        if (up + down + left + right == 0)
        {
            return;
        }

        double speedMultiplier = _speedBoostRemainingSeconds > 0
            ? SpeedBoostMultiplier
            : 1.0;
        var pixelsToMove = BaseSpeed * speedMultiplier * elapsedSeconds;
        var x = Position.X + (int)(right * pixelsToMove);
        x -= (int)(left * pixelsToMove);
        var y = Position.Y + (int)(down * pixelsToMove);
        y -= (int)(up * pixelsToMove);
        
        var newAnimation = _currentAnimation;
        if (y < Position.Y && _currentAnimation != "MoveUp")
        {
            newAnimation = "MoveUp";
        }
        if (y > Position.Y && newAnimation != "MoveDown")
        {
            newAnimation = "MoveDown";
        }
        if (x < Position.X && newAnimation != "MoveLeft")
        {
            newAnimation = "MoveLeft";
        }
        if (x > Position.X && newAnimation != "MoveRight")
        {
            newAnimation = "MoveRight";
        }
        if (x == Position.X && y == Position.Y && newAnimation != "IdleDown")
        {
            newAnimation = "IdleDown";
        }
        if (newAnimation != _currentAnimation)
        {
            _currentAnimation = newAnimation;
            SpriteSheet.ActivateAnimation(_currentAnimation);
        }
        var futureBounds = new Rectangle<int>(x, y, width, height);

        if (IsInsideMap(futureBounds, engine) && !engine.IsBlocked(futureBounds))
        {
            Position = (x, y);
        }

        
    }
   

   

    
    
    private static bool IsInsideMap(Rectangle<int> bounds, Engine engine)
    {
        return bounds.Origin.X >= 0 &&
               bounds.Origin.Y >= 0 &&
               bounds.Origin.X + bounds.Size.X <= engine.GetMapWidthInPixels() &&
               bounds.Origin.Y + bounds.Size.Y <= engine.GetMapHeightInPixels();
    }
    
    

}
