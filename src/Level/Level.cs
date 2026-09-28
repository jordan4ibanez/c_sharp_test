namespace FishGame.Level;

public static class Level {

    // If you're in a level, this logic container will get called.

    bool loaded = false;
    bool paused = false;

    void load(string levelDirectory) {
        if (loaded) {
            throw new Error("[Level]: Unload the level first.");
        }
        // todo: water level parse.
        Ground.load(levelDirectory);
        Water.load();
        Player.setDefaultPosition();

        loaded = true;
    }

    void unload() {
        throw new Error("[Level]: unloading not implemented");
        loaded = false;
    }

    void update() {
        if (paused) {
            return;
        }

        Ground.update();
        Water.update();
        FishTank.update();
        Player.update();
        Lure.update();
        Player.cameraUpdate();
    }

    void draw() {
        Ground.draw();
        FishTank.draw();
        Player.draw();
        Lure.draw();
        Water.draw();
    }

    void togglePause() {
        paused = !paused;
    }
}