using System.Numerics;
using Raylib_cs;

namespace FishGame.Graphics;

public static class FontManager {

    // Roboto condensed medium looks pretty close to the Bass Rise font, kind of.
    static Font font;
    static readonly float spacing = -1;
    static float currentFontSize = 1;

    static void Initialize() {
        font = new Font();
        string codePointString = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789!@#$%^&*()_-+={[]}|\\;:'\",<.>©";
        int[] codepoints = [.. codePointString.Select(c => (int)c)];
        font = Raylib.LoadFontEx("font/roboto_condensed.ttf", 64, codepoints, 0);
    }

    static Vector2 getTextSize(string text) {
        return Raylib.MeasureTextEx(font, text, currentFontSize, spacing);
    }

    static void Draw(string text, float x, float y) {
        Raylib.DrawTextEx(font, text, new Vector2(x, y), currentFontSize, spacing, Color.Black);
    }
    static void Draw(string text, float x, float y, Color color) {
        Raylib.DrawTextEx(font, text, new Vector2(x, y), currentFontSize, spacing, color);
    }

    static void DrawShadowed(string text, float x, float y) {
        Raylib.DrawTextEx(font, text, new Vector2(x, y), currentFontSize, spacing, Color.Black);
        Raylib.DrawTextEx(font, text, new Vector2(x - 1, y - 1), currentFontSize, spacing, Color.White);
    }

    static void DrawShadowed(string text, float x, float y, Color foregroundColor) {
        Raylib.DrawTextEx(font, text, new Vector2(x, y), currentFontSize, spacing, Color.Black);
        Raylib.DrawTextEx(font, text, new Vector2(x - 1, y - 1), currentFontSize, spacing, foregroundColor);
    }

    static void Terminate() {
        Raylib.UnloadFont(font);
    }

    static void __update() {
        // This allows the font to look slightly off, like it's a texture font.
        currentFontSize = font.BaseSize * (GUI.getGUIScale() * 0.75);
    }
}