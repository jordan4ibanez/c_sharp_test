using Raylib_cs;
using System.Numerics;

namespace RaylibGame;

// See https://aka.ms/new-console-template for more information
// Console.WriteLine("Hello, World!");



internal static class Program {
    [STAThread]
    public static void Main() {
        const int screenWidth = 800;
        const int screenHeight = 450;

        Raylib.InitWindow(screenWidth, screenHeight, "Simple Raylib Project");
        Raylib.SetTargetFPS(60);

        var ballSpeed = new Vector2(300f, 200f);
        Ball ball = new(new(200, 200), new(300, 200), 20, Color.Red, screenWidth, screenHeight);



        while (!Raylib.WindowShouldClose()) {
            float dt = Raylib.GetFrameTime();

            ball.Update(dt);

            // Render frame
            Raylib.BeginDrawing();
            Raylib.ClearBackground(Color.RayWhite);

            Raylib.DrawCircleV(ball.Position, ball.Radius, Color.Maroon);
            Raylib.DrawText("Press ESC or close window to exit", 10, 10, 20, Color.DarkGray);

            Raylib.EndDrawing();
        }

        Raylib.CloseWindow();
    }
}