using Raylib_cs;

namespace FishGame.Graphics;

static class TextureManager {

    static Dictionary<string, Texture2D> database;

    static void LoadTexture(string path) {

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
    }

    static Texture2D GetTexture(string textureName) {
        if (!database.ContainsKey(textureName)) {
            throw new Exception($"[TextureManager]: Texture {textureName} does not exist.");
        }
        return database[textureName];
    }

    static void DeleteTexture(string textureName) {
        if (database.TryGetValue(textureName, out Texture2D texture)) {
            Texture2D thisTexture = database[textureName];
            Raylib.UnloadTexture(thisTexture);
            database.Remove(textureName);
        } else {
            throw new Exception($"[TextureManager]: Texture {textureName} does not exist. Cannot delete.");
        }
    }

    static void Terminate() {
        foreach (var (textureName, thisTexture) in database) {
            Raylib.UnloadTexture(thisTexture);
        }
        database.Clear();
    }

}