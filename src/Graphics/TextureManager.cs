using Raylib_cs;

namespace FishGame.Graphics;

static class TextureManager {

    static Dictionary<string, Texture2D> database;

    static void LoadTexture(string location) {

        if (!File.Exists(location)) {
            throw new Exception($"[TextureManager]: {location} is not a file.");
        }



        // // Extract the file name from the location.
        // string fileName = () {
        //     string[] items = location.split("/");
        //     int len = cast(int) items.length;
        //     if (len <= 1) {
        //         throw new Error("[TextureManager]: Texture must not be in root directory.");
        //     }
        //     string outputFileName = items[len - 1];
        //     if (!outputFileName.endsWith(".png")) {
        //         throw new Error("[TextureManager]: Not a .png");
        //     }
        //     return outputFileName;
        // }
        // ();

        // if (fileName in database) {
        //     throw new Error("[TextureManager]: Tried to overwrite [" ~fileName ~"]");
        // }

        // Texture2D* thisTexture = new Texture2D();
        // *thisTexture = LoadTexture(toStringz(location));

        // if (!IsTextureValid(*thisTexture)) {
        //     throw new Error("[TextureManager]: Texture [" ~location ~"] is invalid.");
        // }

        // database[fileName] = thisTexture;
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