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
        TextureManager.Initialize();

        CameraManager.Initialize();

        ShaderManager.NewShader("water", "shaders/water.vert", "shaders/water.frag");
        ShaderManager.NewShader("ground", "shaders/ground.vert", "shaders/ground.frag");
        ShaderManager.NewShader("normal", "shaders/normal.vert", "shaders/normal.frag");

        Rlgl.DisableBackfaceCulling();

        // ModelManager.LoadModelFromFile("models/largemouth.glb");
        // ModelManager.setModelTexture("largemouth.glb", "largemouth.png");
        // ModelManager.setModelShader("largemouth.glb", "normal");

        // ModelManager.loadModelFromFile("models/boat.glb");
        // ModelManager.setModelTexture("boat.glb", "boat.png");

        // ModelManager.loadModelFromFile("models/person.glb");
        // ModelManager.setModelTexture("person.glb", "person.png");

        // ModelManager.loadModelFromFile("models/fishing_rod.glb");
        // ModelManager.setModelTexture("fishing_rod.glb", "fishing_rod.png");

        ModelManager.LoadModelFromFile("models/lures/deep_c_110.glb");


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


    float rotation = 0;

    public void MainLoop() {
        DoInternals();

        double delta = Delta.Get();


        timer += delta;

        if (timer >= 1.0) {
            timer -= 1.0;
            Console.WriteLine(Raylib.GetFPS());
            // SoundManager.Play("casting_woosh.ogg");
        }

        rotation += (float)delta;
        if (rotation > Math.PI * 2) {
            rotation -= (float)Math.PI * 2;
        }

        Raylib.SetTargetFPS(0);
        Raylib.ClearWindowState(ConfigFlags.VSyncHint);



        Raylib.BeginDrawing();


        // System.Console.WriteLine(Delta.Get());


        // UpdateCamera(camera, CameraMode.CAMERA_ORBITAL);

        CameraManager.SetPosition(new Vector3(1, 1, 1));

        ModelManager.Destroy("deep_c_110.glb");
        ModelManager.LoadModelFromFile("models/lures/deep_c_110.glb");
        ModelManager.SetModelTexture("deep_c_110.glb", "deep_c_110.png");



        {
            Raylib.ClearBackground(Color.SkyBlue);

            Raylib.BeginMode3D(CameraManager.Get());
            {

                // if (renderPerson) {
                // ModelHandler.draw("person.glb", Vector3(0, 0, 0));
                // }
                ModelManager.Draw("deep_c_110.glb", new Vector3(0, 0, 0), new Vector3(0, rotation, 0));
            }
            Raylib.EndMode3D();
        }


        Raylib.EndDrawing();

    }

    public void Dispose() {
        ModelManager.Terminate();
        TextureManager.Terminate();
        ShaderManager.Terminate();
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

