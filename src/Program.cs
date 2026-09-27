using System.Numerics;
using System.Runtime.CompilerServices;
using FishGame.Utility;
using Microsoft.VisualBasic;
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
        Raylib.InitWindow((int)windowSize.X, (int)windowSize.Y, windowTitle);
        Raylib.InitAudioDevice();
        Raylib.SetTargetFPS(60);
    }

    public void MainLoop() {
        Raylib.BeginDrawing();

        // System.Console.WriteLine(Delta.Get());

        timer += Delta.Get();

        if (timer >= 1.0) {
            timer -= 1.0;
            Console.WriteLine("noise");
        }


        Raylib.EndDrawing();

    }

    public void Dispose() {
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

