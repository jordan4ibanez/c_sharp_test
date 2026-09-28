using System.Numerics;
using Raylib_cs;

namespace FishGame.Input;

public static class Mouse {
    public static Vector2 GetDelta() {
        return Raylib.GetMouseDelta();
    }

    public static bool IsButtonPressed(MouseButton button) {
        return Raylib.IsMouseButtonPressed(button);
    }

    public static bool IsButtonDown(MouseButton button) {
        return Raylib.IsMouseButtonDown(button);
    }

}