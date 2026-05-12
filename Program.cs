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
      //  FileReader fileReader = new FileReader();
        
        using var gameWindow = new GameWindow(sdl);
        {
            
            var input = new Input(sdl);
            var gameRenderer = new GameRenderer(sdl, gameWindow);
            var engine = new Engine(gameRenderer, input);


            engine.SetupWorld();




            bool quit = false;

            while (!quit)
            {
                quit = input.ProcessInput();

                if (input.ZoomInRequested)
                {
                    gameRenderer.ZoomIn();
                }

                if (input.ZoomOutRequested)
                {
                    gameRenderer.ZoomOut();
                }


                if (quit)
                {
                    engine.SaveGame();
                    break;
                }
                    
                    

                engine.ProcessFrame();

                gameRenderer.SetSelectedHotbarIndex(input.SelectedHotbarIndex);

                engine.RenderFrame();




                System.Threading.Thread.Sleep(13);
            }
            
        }
        

        sdl.Quit();
    }
}