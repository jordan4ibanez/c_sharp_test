using Raylib_cs;

namespace FishGame;

// See https://aka.ms/new-console-template for more information
// Console.WriteLine("Hello, World!");



internal static class Program {
    [STAThread]
    public static void Main() {
        const int screenWidth = 800;
        const int screenHeight = 450;

        Raylib.InitWindow(screenWidth, screenHeight, "Simple Raylib Project");
        Raylib.SetTargetFPS(60);

        while (!Raylib.WindowShouldClose()) {


            Raylib.EndDrawing();
        }

        Raylib.CloseWindow();
    }
}