using System;

using Silk.NET.SDL;

namespace TheAdventure;

public static class Program
{
    public static void Main()
    {
        var sdl = new Sdl(new SdlContext());

        

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

        using var gameWindow = new GameWindow(sdl);

        
        var gameRenderer = new GameRenderer(sdl, gameWindow);
        var gameLogic = new GameLogic(gameRenderer);
        var inputLogic = new InputLogic(sdl, gameLogic, gameRenderer);


        gameLogic.InitializeGame();
        
        


        bool quit = false;

        while (!quit)
        {
            quit = inputLogic.ProcessInput();
            
            if (inputLogic.ZoomInRequested)
            {
                gameRenderer.ZoomIn();
            }

            if (inputLogic.ZoomOutRequested)
            {
                gameRenderer.ZoomOut();
            }
            
            
            if (quit)
                break;
            
            gameLogic.ProcessFrame(); 
            
            gameRenderer.SetSelectedHotbarIndex(inputLogic.SelectedHotbarIndex);
            
            gameLogic.RenderFrame();

            
            
            
            System.Threading.Thread.Sleep(13);
        }

        
        sdl.Quit();
    }
}