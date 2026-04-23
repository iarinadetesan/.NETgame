using System;
using System.Diagnostics;
using Silk.NET.SDL;

namespace TheAdventure;

public static class Program
{
    public static void Main()
    {
        var sdl = new Sdl(new SdlContext());

        UInt64 framesRenderedCounter = 0;
        var timer = new Stopwatch();

        var sdlInitResult = sdl.Init(
            Sdl.InitVideo |
            Sdl.InitAudio |
            Sdl.InitEvents |
            Sdl.InitTimer |
            Sdl.InitGamecontroller |
            Sdl.InitJoystick
        );

        if (sdlInitResult < 0)
        {
            throw new InvalidOperationException("Failed to initialize SDL.");
        }

        var gameWindow = new GameWindow(sdl);
        var gameLogic = new GameLogic();
        var gameRenderer = new GameRenderer(sdl, gameWindow, gameLogic);
        var inputLogic = new InputLogic(sdl,  gameLogic);

        gameLogic.InitializeGame();
        

        bool quit = false;

        while (!quit)
        {
            quit = inputLogic.ProcessInput();
            if (quit)
                break;
            
            gameLogic.ProcessFrame(); //nu face nmc acum ca e gol
            
            gameRenderer.SetSelectedHotbarIndex(inputLogic.SelectedHotbarIndex);
            
            gameRenderer.Render();
            
            ++framesRenderedCounter;
            
            System.Threading.Thread.Sleep(16);
        }

        gameWindow.Destroy();
        sdl.Quit();
    }
}