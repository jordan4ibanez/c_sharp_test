namespace FishGame.Level;

public static class Level {

    // If you're in a level, this logic container will get called.

    static bool loaded = false;
    static bool paused = false;

    public static void Load(string levelDirectory) {
        if (loaded) {
            throw new Exception("[Level]: Unload the level first.");
        }
        // todo: water level parse.
        Ground.Load(levelDirectory);
        Water.Load();
        Player.SetDefaultPosition();

        loaded = true;
    }

    public static void Unload() {
        throw new Exception("[Level]: unloading not implemented");
        loaded = false;
    }

    public static void Update() {
        if (paused) {
            return;
        }

        Ground.Update();
        Water.Update();
        FishTank.Update();
        Player.Update();
        Lure.Update();
        Player.CameraUpdate();
    }

    public static void Draw() {
        Ground.Draw();
        FishTank.Draw();
        Player.Draw();
        Lure.Draw();
        Water.Draw();
    }

    public static void TogglePause() {
        paused = !paused;
    }
}