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

    // Texture2D* getTexturePointer(string textureName) {
    //     if (textureName!in database) {
    //         throw new Error("[TextureManager]: Texture does not exist. " ~textureName);
    //     }

    //     return database[textureName];
    // }

    // void deleteTexture(string textureName) {
    //     if (textureName!in database) {
    //         throw new Error(
    //             "[TextureManager]: Texture does not exist. Cannot delete. " ~textureName);
    //     }

    //     Texture* thisTexture = database[textureName];
    //     UnloadTexture(*thisTexture);
    //     database.remove(textureName);
    // }

    // void terminate() {
    //     foreach (textureName, thisTexture; database) {
    //         UnloadTexture(*thisTexture);
    //     }

    //     database.clear();
    // }

}