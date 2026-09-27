using System.Numerics;
using Raylib_cs;

namespace FishGame.Utility;

static class Window {

    static bool maximized = false;
    static bool mouseLocked = false;

    public static int GetWidth() {
        return Raylib.GetRenderWidth();
    }

    public static int GetHeight() {
        return Raylib.GetRenderHeight();
    }

    public static Vector2 GetSize() {
        return new Vector2(GetWidth(), GetHeight());
    }

    public static bool ShouldStayOpen() {
        return !Raylib.WindowShouldClose();
    }

    public static void Maximize() {
        maximized = true;
        Raylib.MaximizeWindow();
    }

    public static void Unmaximize() {
        maximized = false;
        Raylib.RestoreWindow();
    }

    public static void ToggleMaximize() {
        if (maximized) {
            Unmaximize();
        } else {
            Maximize();
        }
    }

    public static void LockMouse() {
        mouseLocked = true;
        Raylib.DisableCursor();
    }

    public static void UnlockMouse() {
        mouseLocked = false;
        Raylib.EnableCursor();
    }

    public static void ToggleMouseLock() {
        if (mouseLocked) {
            UnlockMouse();
        } else {
            LockMouse();
        }
    }

    public static bool IsMouseLocked() {
        return mouseLocked;
    }

}