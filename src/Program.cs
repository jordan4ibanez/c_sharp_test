using System.Numerics;
using System.Reflection;
using FishGame.Audio;
using FishGame.Graphics;
using FishGame.Input;
using FishGame.Level;
using FishGame.Utility;
using Raylib_cs;

namespace FishGame;

class Game : IDisposable {


    readonly string windowTitle = "Fish Game";

    static readonly bool DEBUG_MODE = false;


    public Game() {
        Setup();
    }

    public static bool IsDebugMode() {
        return DEBUG_MODE;
    }

    void Setup() {

        // Reflection to get the package version of raylib-cs.
        // Console.WriteLine($"Raylib-cs: {typeof(Raylib_cs.Raylib).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion}");

        Raylib.SetTraceLogLevel(TraceLogLevel.Warning);
        Raylib.SetConfigFlags(ConfigFlags.ResizableWindow | ConfigFlags.VSyncHint);

        Raylib.InitWindow(1, 1, windowTitle);
        CenterWindow();

        Raylib.InitAudioDevice();

        Raylib.SetTargetFPS(0);

        SoundManager.Initialize();
        FontManager.Initialize();
        TextureManager.Initialize();
        ModelManager.Initialize();
        ShaderManager.Initialize();

        CameraManager.Initialize();

        // todo: this should probably be done automatically somehow with fish definitions.
        ModelManager.SetModelShader("largemouth.glb", "normal");

        // todo: this should probably be done automatically somehow with lure definitions.
        ModelManager.SetModelShader("deep_c_110.glb", "normal");

        Rlgl.DisableBackfaceCulling();

        LevelManager.Load("levels/map_lake/");
    }

    void CenterWindow() {
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

    void DoInternals() {
        Delta.CalculateDelta();
        GUI.Update();
        FontManager.Update();
    }

    public void MainLoop() {
        DoInternals();

        float delta = Delta.Get();


        if (Keyboard.IsPressed(KeyboardKey.F1)) {
            Window.ToggleMaximize();
        }

        if (Keyboard.IsPressed(KeyboardKey.F2)) {
            Window.ToggleMouseLock();
        }

        if (Keyboard.IsPressed(KeyboardKey.F3)) {
            LevelManager.TogglePause();
        }

        //? This is to ensure the game doesn't explode.
        // LevelManager.Unload();
        // LevelManager.Load("levels/map_lake/");

        LevelManager.Update();

        Raylib.BeginDrawing();

        {
            Raylib.ClearBackground(Color.SkyBlue);
            // CameraManager.SetPosition(new Vector3(128, 128, 128));

            Raylib.BeginMode3D(CameraManager.Get());
            {
                LevelManager.Draw();
            }
            Raylib.EndMode3D();

            FontManager.DrawShadowed("FPS: " + Raylib.GetFPS(), 0, -3 * GUI.GetGUIScale());
            FontManager.DrawShadowed("Bass Count: " + FishTank.GetFishCount(), 0, 32 * GUI.GetGUIScale());


            // 
            // ? This is the fake copyright info for this build. :P
            Vector2 windowSize = Window.GetSize();
            Vector2 textSize = FontManager.GetTextSize("© METABASS GENERAL LURES INC.");

            FontManager.DrawShadowed("© METABASS GENERAL LURES INC.", 1, windowSize.Y - (
                    textSize.Y * 2) + 10);
            FontManager.DrawShadowed("PROTOTYPE BUILD. DO NOT DISTRIBUTE.", 2, windowSize.Y - textSize.Y + 5);
            //


        }


        Raylib.EndDrawing();

    }

    public void Dispose() {
        LevelManager.Unload();
        ShaderManager.Terminate();
        ModelManager.Terminate();
        TextureManager.Terminate();
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

