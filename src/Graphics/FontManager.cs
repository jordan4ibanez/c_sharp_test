using System.Numerics;
using Raylib_cs;

namespace FishGame.Graphics;

public static class FontManager {

    // Roboto condensed medium looks pretty close to the Bass Rise font, kind of.
    static Font font;
    static readonly float spacing = -1;
    static float currentFontSize = 1;

    public static void Initialize() {
        font = new Font();
        string codePointString = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789!@#$%^&*()_-+={[]}|\\;:'\",<.>©";
        int[] codepoints = [.. codePointString.Select(c => (int)c)];
        font = Raylib.LoadFontEx("font/roboto_condensed.ttf", 64, codepoints, 0);
    }

    public static Vector2 GetTextSize(string text) {
        return Raylib.MeasureTextEx(font, text, currentFontSize, spacing);
    }

    public static void Draw(string text, float x, float y) {
        Raylib.DrawTextEx(font, text, new Vector2(x, y), currentFontSize, spacing, Color.Black);
    }
    public static void Draw(string text, float x, float y, Color color) {
        Raylib.DrawTextEx(font, text, new Vector2(x, y), currentFontSize, spacing, color);
    }

    public static void DrawShadowed(string text, float x, float y) {
        Raylib.DrawTextEx(font, text, new Vector2(x, y), currentFontSize, spacing, Color.Black);
        Raylib.DrawTextEx(font, text, new Vector2(x - 1, y - 1), currentFontSize, spacing, Color.White);
    }

    public static void DrawShadowed(string text, float x, float y, Color foregroundColor) {
        Raylib.DrawTextEx(font, text, new Vector2(x, y), currentFontSize, spacing, Color.Black);
        Raylib.DrawTextEx(font, text, new Vector2(x - 1, y - 1), currentFontSize, spacing, foregroundColor);
    }

    public static void Update() {
        // This allows the font to look slightly off, like it's a texture font.
        currentFontSize = (float)(font.BaseSize * (GUI.GetGUIScale() * 0.75));
    }

    public static void Terminate() {
        Raylib.UnloadFont(font);
    }
}