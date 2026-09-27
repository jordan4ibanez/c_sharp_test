using FishGame.Utility;
using Raylib_cs;

namespace FishGame;

class Game : IDisposable {

    public Game() {
        System.Console.WriteLine("created");
    }

    public void Dispose() {
        System.Console.WriteLine("Destroyed");
    }
}



internal static class Program {
    [STAThread]
    public static void Main() {
        using Game game = new();


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

