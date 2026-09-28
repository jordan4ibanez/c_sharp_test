using Raylib_cs;

namespace FishGame.Graphics;

static class TextureManager {

    static readonly Dictionary<string, Texture2D> database = [];

    public static void Initialize() {
        // Texture Manager should be the very first thing that loads up when it comes to visual assets.
        Console.WriteLine("-----");
        string[] folders = ["models", "textures"];
        foreach (string dir in folders) {
            // Console.WriteLine(dir);
            foreach (string filePath in Directory.EnumerateFiles(dir, "*.png", SearchOption.AllDirectories)) {
                LoadTexture(filePath);
            }
        }
        Console.WriteLine("-----");
    }

    public static void LoadTexture(string path) {

        if (!File.Exists(path)) {
            throw new Exception($"[TextureManager]: {path} is not a file.");
        }

        if (!path.EndsWith(".png", StringComparison.OrdinalIgnoreCase)) {
            throw new Exception($"[TextureManager]: {path} is not a png.");
        }

        string fileName = Path.GetFileName(path);

        if (fileName.Length == 0) {
            throw new Exception($"[TextureManager]: {path} returned a blank filename.");
        }

        if (database.ContainsKey(fileName)) {
            throw new Exception($"[TextureManager]: Tried to overwrite {path}.");
        }

        Texture2D thisTexture = Raylib.LoadTexture(path);

        if (!Raylib.IsTextureValid(thisTexture)) {
            throw new Exception($"[TextureManager]: {path} is an invalid texture.");
        }

        database[fileName] = thisTexture;
        Console.WriteLine($"[TextureManager]: Loaded [{path}] as [{fileName}]");
    }

    public static bool HasTexture(string textureName) {
        return database.ContainsKey(textureName);
    }

    public static Texture2D GetTexture(string textureName) {
        if (!database.ContainsKey(textureName)) {
            throw new Exception($"[TextureManager]: Texture {textureName} does not exist.");
        }
        return database[textureName];
    }

    public static void DeleteTexture(string textureName) {
        if (database.TryGetValue(textureName, out Texture2D texture)) {
            Texture2D thisTexture = database[textureName];
            Raylib.UnloadTexture(thisTexture);
            database.Remove(textureName);
        } else {
            throw new Exception($"[TextureManager]: Texture {textureName} does not exist. Cannot delete.");
        }
    }

    public static void Terminate() {
        foreach (var (textureName, thisTexture) in database) {
            Raylib.UnloadTexture(thisTexture);
        }
        database.Clear();
    }

}