using Raylib_cs;

namespace FishGame.Input;

public static class Keyboard {

    public static bool IsDown(KeyboardKey key) {
        return Raylib.IsKeyDown(key);
    }

    public static bool IsPressed(KeyboardKey key) {
        return Raylib.IsKeyPressed(key);
    }

    public static bool IsReleased(KeyboardKey key) {
        return Raylib.IsKeyReleased(key);
    }
}