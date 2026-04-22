using System;
using System.IO;
using Silk.NET.Maths;

namespace TheAdventure.Models;

public class PlayerObject : GameObject
{
    public int X { get; set; } = 100;
    public int Y { get; set; } = 100;

    private Rectangle<int> _source;
    private Rectangle<int> _target;

    private readonly int _textureId;

    private const int Speed = 128;

    private const int FrameWidth = 24;
    private const int FrameHeight = 24;

    private int _currentFrame = 0;
    private int _animationTimer = 0;
    private const int FrameDuration = 100;

    private AnimationState _currentState = AnimationState.Idle;

    private enum AnimationState
    {
        Idle,
        WalkLeft,
        WalkRight,
        Rear
    }

    public PlayerObject(int id) : base(id)
    {
        _textureId = GameRenderer.LoadTexture(Path.Combine("Assets", "player.png"), out _);
        if (_textureId < 0)
        {
            throw new Exception("Failed to load player texture");
        }

        _source = new Rectangle<int>(0, 0, FrameWidth, FrameHeight);
        _target = new Rectangle<int>(0, 0, FrameWidth, FrameHeight);

        UpdateTarget();
        UpdateSource();
    }

    public void UpdatePosition(double up, double down, double left, double right, int time, GameLogic gameLogic)
    {
        var pixelsToMove = Speed * (time / 1000.0);
        bool isMoving = false;

        
        int newX = X;
        int newY = Y;

        if (left > 0)
        {
            newX -= (int)(pixelsToMove * left);
            _currentState = AnimationState.WalkLeft;
            isMoving = true;
        }
        else if (right > 0)
        {
            newX += (int)(pixelsToMove * right);
            _currentState = AnimationState.WalkRight;
            isMoving = true;
        }

        if (up > 0)
        {
            newY -= (int)(pixelsToMove * up);
            _currentState = AnimationState.Rear;
            isMoving = true;
        }

        if (down > 0)
        {
            newY += (int)(pixelsToMove * down);
            _currentState = AnimationState.Idle;
            isMoving = true;
        }

        if (!isMoving)
        {
            _currentState = AnimationState.Idle;
        }
        var futureBounds = new Rectangle<int>(newX + 4, newY + 8, 16, 16);

        if (!gameLogic.IsBlocked(futureBounds))
        {
            X = newX;
            Y = newY;
        }
        
        UpdateAnimation(time, isMoving);
        UpdateTarget();
        UpdateSource();
    }
    public void UpdatePosition(double up, double down, double left, double right, int time)
    {
        var pixelsToMove = Speed * (time / 1000.0);

        bool isMoving = false;

        if (left > 0)
        {
            X -= (int)(pixelsToMove * left);
            _currentState = AnimationState.WalkLeft;
            isMoving = true;
        }
        else if (right > 0)
        {
            X += (int)(pixelsToMove * right);
            _currentState = AnimationState.WalkRight;
            isMoving = true;
        }

        if (up > 0)
        {
            Y -= (int)(pixelsToMove * up);
            _currentState = AnimationState.Rear;
            isMoving = true;
        }

        if (down > 0)
        {
            Y += (int)(pixelsToMove * down);
            isMoving = true;
        }

        if (!isMoving)
        {
            _currentState = AnimationState.Idle;
        }

        UpdateAnimation(time, isMoving);
        UpdateTarget();
        UpdateSource();
    }

    private void UpdateAnimation(int time, bool isMoving)
    {
        _animationTimer += time;

        int startFrame = 0;
        int endFrame = 0;

        switch (_currentState)
        {
            case AnimationState.Idle:
                startFrame = 0;
                endFrame = 3;
                break;

            case AnimationState.WalkRight:
                startFrame = 8;
                endFrame = 11;
                break;

            case AnimationState.WalkLeft:
                startFrame = 4;
                endFrame = 7;
                break;
            case AnimationState.Rear:
                startFrame = 12;
                endFrame = 15;
                break;
        }

        if (!isMoving && _currentState != AnimationState.Idle)
        {
            _currentFrame = startFrame;
            return;
        }

        if (_currentFrame < startFrame || _currentFrame > endFrame)
        {
            _currentFrame = startFrame;
        }

        if (_animationTimer >= FrameDuration)
        {
            _animationTimer = 0;
            _currentFrame++;

            if (_currentFrame > endFrame)
            {
                _currentFrame = startFrame;
            }
        }
    }

    private void UpdateSource()
    {
        _source = new Rectangle<int>(
            _currentFrame * FrameWidth,
            0,
            FrameWidth,
            FrameHeight
        );
    }

    
    private void UpdateTarget()
    {
        _target = new Rectangle<int>(X, Y, FrameWidth , FrameHeight );
    }

    public void Render(GameRenderer renderer)
    {
        renderer.RenderTexture(_textureId, _source, _target);
    }
    public Rectangle<int> CollisionBounds
    {
        get
        {
            return new Rectangle<int>(X , Y , 24, 24);
        }
    }
}