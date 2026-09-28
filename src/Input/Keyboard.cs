using Raylib_cs;

namespace FishGame.Input;

public static class Keyboard {

    static bool IsDown(KeyboardKey key) {
        return Raylib.IsKeyDown(key);
    }

    static bool IsPressed(KeyboardKey key) {
        return Raylib.IsKeyPressed(key);
    }

    static bool IsReleased(KeyboardKey key) {
        return Raylib.IsKeyReleased(key);
    }
}