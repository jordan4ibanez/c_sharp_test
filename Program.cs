using Raylib_cs;
using System.Numerics;

namespace RaylibGame;

// See https://aka.ms/new-console-template for more information
// Console.WriteLine("Hello, World!");



internal static class Program
{
    [STAThread]
    public static void Main()
    {
        const int screenWidth = 800;
        const int screenHeight = 450;

        Raylib.InitWindow(screenWidth, screenHeight, "Simple Raylib Project");
        Raylib.SetTargetFPS(60);

        Vector2 ballPosition = new(screenWidth / 2f, screenHeight / 2f);
        Vector2 ballSpeed = new(300f, 200f);
        const float ballRadius = 20f;

        while (!Raylib.WindowShouldClose())
        {
            float dt = Raylib.GetFrameTime();

            // Update ball position
            ballPosition += ballSpeed * dt;

            // Bounce off wall boundaries
            if (ballPosition.X + ballRadius >= screenWidth || ballPosition.X - ballRadius <= 0)
            {
                ballSpeed.X *= -1;
            }
            if (ballPosition.Y + ballRadius >= screenHeight || ballPosition.Y - ballRadius <= 0)
            {
                ballSpeed.Y *= -1;
            }

            // Render frame
            Raylib.BeginDrawing();
            Raylib.ClearBackground(Color.RayWhite);

            Raylib.DrawCircleV(ballPosition, ballRadius, Color.Maroon);
            Raylib.DrawText("Press ESC or close window to exit", 10, 10, 20, Color.DarkGray);

            Raylib.EndDrawing();
        }

        Raylib.CloseWindow();
    }
}