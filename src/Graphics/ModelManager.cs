using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using FishGame.Utility;
using Raylib_cs;

namespace FishGame.Graphics;

class AnimationContainer {
    public int animationCount = 0;
    public bool hasAnimation = false;
    public ModelAnimation[] animationData = [];
}


static class ModelManager {

    static Dictionary<string, Model> database = [];
    static Dictionary<string, bool> isCustomDatabase = [];
    static Dictionary<string, AnimationContainer> animationDatabase = [];

    public static void Initialize() {
        string[] folders = ["models"];
        foreach (string dir in folders) {
            // Console.WriteLine(dir);
            foreach (string filePath in Directory.EnumerateFiles(dir, "*.glb", SearchOption.AllDirectories)) {
                LoadModelFromFile(filePath);
            }
        }

        if (Game.IsDebugMode()) {
            Console.WriteLine("-----");
        }
        AutoAssignTextures();
    }

    static void AutoAssignTextures() {
        if (Game.IsDebugMode()) {
            Console.WriteLine("[ModelManager]: Begin texture auto application.");
        }
        foreach (var (modelName, _) in database) {
            string textureName = Path.GetFileNameWithoutExtension(modelName) + ".png";
            if (TextureManager.HasTexture(textureName)) {
                SetModelTexture(modelName, textureName);
                if (Game.IsDebugMode()) {
                    Console.WriteLine($"[ModelManager]: Applied texture [{textureName}] to [{modelName}].");
                }
            }
        }
        if (Game.IsDebugMode()) {
            Console.WriteLine("-----");
        }
    }

    public static void Draw(string modelName, Vector3 position) {
        Draw(modelName, position, new Vector3(0, 0, 0), 1.0f, Color.White);
    }
    public static void Draw(string modelName, Vector3 position, Vector3 rotation) {
        Draw(modelName, position, rotation, 1.0f, Color.White);
    }
    public static void Draw(string modelName, Vector3 position, Vector3 rotation, float scale) {
        Draw(modelName, position, rotation, scale, Color.White);
    }
    public static void Draw(string modelName, Vector3 position, Vector3 rotation, float scale, Color color) {

        if (!database.ContainsKey(modelName)) {
            throw new Exception("[ModelManager]: Cannot draw model that does not exist. " + modelName);
        }

        Model thisModel = database[modelName];

        // Have to jump through some hoops to rotate the model correctly.
        Quaternion quat = Raymath.QuaternionFromEuler(rotation.X, rotation.Y, rotation.Z);
        quat.ToAxisAngle(out Vector3 axisRotation, out float angle);
        Raylib.DrawModelEx(thisModel, position, axisRotation, Raylib.RAD2DEG * angle, new Vector3(scale, scale, scale), color);
    }

    public static unsafe void NewModelFromMesh(string modelName, float[] vertices, float[] textureCoordinates, bool dynamic = false) {

        if (database.ContainsKey(modelName)) {
            throw new Exception(
                "[ModelManager]: Tried to overwrite mesh [" + modelName + "]. Delete it first.");
        }

        Mesh thisMesh = new();

        thisMesh.VertexCount = vertices.Length / 3;
        thisMesh.TriangleCount = thisMesh.VertexCount / 3;

        fixed (float* ptr = vertices) {
            thisMesh.Vertices = ptr;
        }
        fixed (float* ptr = textureCoordinates) {
            thisMesh.TexCoords = ptr;
        }

        Raylib.UploadMesh(&thisMesh, dynamic);

        Model thisModel = new();
        thisModel = Raylib.LoadModelFromMesh(thisMesh);

        if (!Raylib.IsModelValid(thisModel)) {
            throw new Exception("[ModelHandler]: Invalid model loaded from mesh. " + modelName);
        }

        database[modelName] = thisModel;
        isCustomDatabase[modelName] = true;
    }

    public static unsafe void LoadModelFromFile(string path) {

        if (!File.Exists(path)) {
            throw new Exception($"[ModelManager]: {path} is not a file.");
        }

        if (!path.EndsWith(".glb", StringComparison.OrdinalIgnoreCase)) {
            throw new Exception($"[ModelManager]: {path} is not a gltf.");
        }

        string fileName = Path.GetFileName(path);

        if (fileName.Length == 0) {
            throw new Exception($"[ModelManager]: {path} returned a blank filename.");
        }

        if (database.ContainsKey(fileName)) {
            throw new Exception($"[ModelManager]: Tried to overwrite {path}.");
        }


        Model thisModel = Raylib.LoadModel(path);

        if (!Raylib.IsModelValid(thisModel)) {
            throw new Exception("[ModelHandler]: Invalid model loaded from file. " + path);
        }

        int animationCount = 0;
        ModelAnimation[] animationArray;
        fixed (byte* ptr = Encoding.UTF8.GetBytes(path + '\0')) {
            ModelAnimation* animsPtr = Raylib.LoadModelAnimations((sbyte*)ptr, &animationCount);
            animationArray = new Span<ModelAnimation>(animsPtr, animationCount).ToArray();
        }

        AnimationContainer thisModelAnimation = new() {
            animationCount = animationCount,
            animationData = animationArray,
            hasAnimation = animationArray.Length > 0
        };

        database[fileName] = thisModel;
        isCustomDatabase[fileName] = false;
        animationDatabase[fileName] = thisModelAnimation;

        if (Game.IsDebugMode()) {
            Console.WriteLine($"[ModelManager]: Loaded model {fileName}");
        }
    }

    public static void SetModelTexture(string modelName, string textureName) {

        if (!database.ContainsKey(modelName)) {
            throw new Exception("[ModelManager]: Tried to set texture on non-existent model [" + modelName + "]");
        }

        Model thisModel = database[modelName];
        Texture2D thisTexture = TextureManager.GetTexture(textureName);

        for (int i = 0; i < thisModel.MaterialCount; i++) {
            unsafe {
                thisModel.Materials[i].Maps[(int)MaterialMapIndex.Diffuse].Texture = thisTexture;
            }
        }
    }

    public static void SetModelShader(string modelName, string shaderName) {

        if (!database.ContainsKey(modelName)) {
            throw new Exception("[ModelManager]: Tried to set shader on non-existent model [" + modelName + "]");
        }

        Model thisModel = database[modelName];
        Shader thisShader = ShaderManager.GetShader(shaderName);
        for (int i = 0; i < thisModel.MaterialCount; i++)
            unsafe {
                thisModel.Materials[i].Shader = thisShader;
            }
    }


    public static Model GetModel(string modelName) {
        if (!database.ContainsKey(modelName)) {
            throw new Exception("[ModelManager]: Tried to set get non-existent model pointer [" + modelName + "]");
        }
        return database[modelName];
    }

    public static void UpdateModelPositionsInGPU(string modelName) {
        if (!database.ContainsKey(modelName)) {
            throw new Exception("[ModelManager]: Tried to update non-existent model [" + modelName + "]");
        }
        Model thisModel = database[modelName];

        /*
#define RL_DEFAULT_SHADER_ATTRIB_LOCATION_POSITION    0
#define RL_DEFAULT_SHADER_ATTRIB_LOCATION_TEXCOORD    1
#define RL_DEFAULT_SHADER_ATTRIB_LOCATION_NORMAL      2
#define RL_DEFAULT_SHADER_ATTRIB_LOCATION_COLOR       3
#define RL_DEFAULT_SHADER_ATTRIB_LOCATION_TANGENT     4
#define RL_DEFAULT_SHADER_ATTRIB_LOCATION_TEXCOORD2   5
#define RL_DEFAULT_SHADER_ATTRIB_LOCATION_INDICES     6
        */
        unsafe {
            for (int i = 0; i < thisModel.MeshCount; i++) {
                Mesh* meshPtr = &thisModel.Meshes[i];
                int dataSize = meshPtr->VertexCount * 3 * sizeof(float);
                Raylib.UpdateMeshBuffer(*meshPtr, 0, meshPtr->Vertices, dataSize, 0);
            }
        }
    }

    public static void Destroy(string modelName) {
        if (!database.ContainsKey(modelName)) {
            throw new Exception("[ModelManager]: Tried to destroy non-existent model. " + modelName);
        }

        Model thisModel = database[modelName];

        DestroyModel(modelName, thisModel);

        database.Remove(modelName);
        isCustomDatabase.Remove(modelName);
        animationDatabase.Remove(modelName);
    }

    public static void Terminate() {
        foreach (var (modelName, thisModel) in database) {
            DestroyModel(modelName, thisModel);
        }
        database.Clear();
        isCustomDatabase.Clear();
        animationDatabase.Clear();
    }

    public static void PlayAnimation(string modelName, int index, int frame) {
        if (!database.ContainsKey(modelName)) {
            throw new Exception("[ModelManager]: Tried to play animation on non-existent model. " + modelName);
        }
        Model thisModel = database[modelName];
        AnimationContainer thisAnimation = animationDatabase[modelName];
        if (thisAnimation is null) {
            throw new Exception("[ModelManager]: Tried to play animation on model with no animation. " + modelName);
        }
        Raylib.UpdateModelAnimation(thisModel, thisAnimation.animationData[index], frame);
    }

    public static AnimationContainer GetAnimationContainer(string modelName) {
        if (!animationDatabase.ContainsKey(modelName)) {
            throw new Exception("[ModelManager]: Tried to get non-existent animation container. " + modelName);
        }
        return animationDatabase[modelName];
    }


    static unsafe void DestroyModel(string modelName, Model thisModel) {
        // If we were using the D runtime to make this model, we'll customize
        // the way we free the items. This makes the GC auto clear.
        if (isCustomDatabase[modelName]) {
            Mesh thisMeshInModel = thisModel.Meshes[0];
            thisMeshInModel.VertexCount = 0;
            thisMeshInModel.Vertices = null;
            thisMeshInModel.TexCoords = null;
            Raylib.UnloadMesh(thisMeshInModel);
            thisModel.Meshes = null;
            thisModel.MeshCount = 0;
            Raylib.UnloadModel(thisModel);
        } else {
            Raylib.UnloadModel(thisModel);
            AnimationContainer thisAnimations = animationDatabase[modelName];
            if (thisAnimations != null && thisAnimations.hasAnimation) {
                fixed (ModelAnimation* ptr = thisAnimations.animationData) {
                    Raylib.UnloadModelAnimations(ptr, animationDatabase[modelName].animationCount);
                }
            }
        }
    }

}