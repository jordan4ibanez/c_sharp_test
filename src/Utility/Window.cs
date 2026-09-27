namespace FishGame.Utility;

static class Window {

    static bool maximized = false;
    static bool mouseLocked = false;

    public static int getWidth() {
        return GetRenderWidth();
    }

    public static int getHeight() {
        return GetRenderHeight();
    }

    public static Vector2 getSize() {
        return Vector2(getWidth(), getHeight());
    }

    public static bool shouldStayOpen() {
        // This calls the update system to automatically make common utilities run.
        updateSystem();

        return !WindowShouldClose();
    }

    public static void maximize() {
        maximized = true;
        MaximizeWindow();
    }

    public static void unmaximize() {
        maximized = false;
        RestoreWindow();
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
        DisableCursor();
    }

    public static void unlockMouse() {
        mouseLocked = false;
        EnableCursor();
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