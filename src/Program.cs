using System.Numerics;
using FishGame.Audio;
using FishGame.Graphics;
using FishGame.Utility;
using Raylib_cs;

namespace FishGame;

class Game : IDisposable {

    Vector2 windowSize = new(800, 450);
    readonly String windowTitle = "Fish Game";

    double timer = 0;


    public Game() {
        System.Console.WriteLine("created");
        Setup();

    }

    void Setup() {

        Raylib.SetTraceLogLevel(TraceLogLevel.Warning);
        Raylib.SetConfigFlags(ConfigFlags.ResizableWindow);

        Raylib.InitWindow(1, 1, windowTitle);
        CenterWindow();

        Raylib.InitAudioDevice();
        Raylib.SetTargetFPS(60);

        SoundManager.Initialize();
        FontManager.Initialize();
    }

    static void CenterWindow() {
        int currentMonitor = Raylib.GetCurrentMonitor();
        int monitorWidth = Raylib.GetMonitorWidth(currentMonitor);
        int monitorHeight = Raylib.GetMonitorHeight(currentMonitor);
        int halfMonitorWidth = monitorWidth / 2;
        int halfMonitorHeight = monitorHeight / 2;
        // Console.WriteLine($"Monitor Resolution: {monitorWidth}x{monitorHeight}");
        Raylib.SetWindowSize(halfMonitorWidth, halfMonitorHeight);
        Vector2 monitorPos = Raylib.GetMonitorPosition(currentMonitor);
        int startX = ((monitorWidth - halfMonitorWidth) / 2) + (int)monitorPos.X;
        int startY = (monitorHeight - halfMonitorHeight) / 2 + (int)monitorPos.Y;
        Raylib.SetWindowPosition(startX, startY);
    }

    public void MainLoop() {

        timer += Delta.Get();

        if (timer >= 1.0) {
            timer -= 1.0;
            Console.WriteLine("woosh");
            SoundManager.Play("casting_woosh.ogg");
        }

        Raylib.BeginDrawing();

        // System.Console.WriteLine(Delta.Get());




        Raylib.EndDrawing();

    }

    public void Dispose() {
        FontManager.Terminate();
        SoundManager.Terminate();

        Raylib.CloseAudioDevice();
        Raylib.CloseWindow();
    }
}



internal static class Program {
    [STAThread]
    public static void Main() {
        using Game game = new();

        while (!Raylib.WindowShouldClose()) {
            Delta.CalculateDelta();
            GUI.Update();
            FontManager.Update();
            game.MainLoop();
        }


        // const int screenWidth = 800;
        // const int screenHeight = 450;

        // Raylib.InitWindow(screenWidth, screenHeight, "Simple Raylib Project");
        // Raylib.SetTargetFPS(60);

        // while (!Raylib.WindowShouldClose()) {
        //     Delta.CalculateDelta();

        //     System.Console.WriteLine("test");

        //     System.Console.WriteLine(Delta.GetDelta());


        //     Raylib.EndDrawing();
        // }

        // Raylib.CloseWindow();
    }
}

