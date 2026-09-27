using System.Numerics;
using Raylib_cs;

namespace FishGame.Utility;

static class Window {

    static bool maximized = false;
    static bool mouseLocked = false;

    public static int getWidth() {
        return Raylib.GetRenderWidth();
    }

    public static int getHeight() {
        return Raylib.GetRenderHeight();
    }

    public static Vector2 getSize() {
        return new Vector2(getWidth(), getHeight());
    }

    public static bool shouldStayOpen() {
        return !Raylib.WindowShouldClose();
    }

    public static void maximize() {
        maximized = true;
        Raylib.MaximizeWindow();
    }

    public static void unmaximize() {
        maximized = false;
        Raylib.RestoreWindow();
    }

    public static void toggleMaximize() {
        if (maximized) {
            unmaximize();
        } else {
            maximize();
        }
    }

    public static void lockMouse() {
        mouseLocked = true;
        Raylib.DisableCursor();
    }

    public static void unlockMouse() {
        mouseLocked = false;
        Raylib.EnableCursor();
    }

    public static void toggleMouseLock() {
        if (mouseLocked) {
            unlockMouse();
        } else {
            lockMouse();
        }
    }

    public static bool isMouseLocked() {
        return mouseLocked;
    }

}