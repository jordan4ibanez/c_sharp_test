using System.Numerics;
using FishGame.Utility;

namespace FishGame.Graphics;

static class GUI {
    // We standardize the GUI with 1080p.
    static Vector2 standardSize = new(1920, 1080);
    static float currentGUIScale = 1.0f;

    public static float GetGUIScale() {
        return currentGUIScale;
    }

    public static void Update() {
        Vector2 windowSize = Window.getSize();
        // Find out which GUI scale is smaller so things can be scaled around it.
        Vector2 scales = new(windowSize.X / standardSize.X, windowSize.Y / standardSize.Y);
        if (scales.X >= scales.Y) {
            currentGUIScale = scales.Y;
        } else {
            currentGUIScale = scales.X;
        }
    }
}