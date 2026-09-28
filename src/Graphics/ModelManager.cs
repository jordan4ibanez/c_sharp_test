using System.Numerics;
using System.Runtime.InteropServices;
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

    static void Draw(string modelName, Vector3 position) {
        Draw(modelName, position, new Vector3(0, 0, 0), 1.0f, Color.White);
    }
    static void Draw(string modelName, Vector3 position, Vector3 rotation) {
        Draw(modelName, position, rotation, 1.0f, Color.White);
    }
    static void Draw(string modelName, Vector3 position, Vector3 rotation, float scale) {
        Draw(modelName, position, rotation, scale, Color.White);
    }
    static void Draw(string modelName, Vector3 position, Vector3 rotation, float scale, Color color) {

        if (!database.ContainsKey(modelName)) {
            throw new Exception("[ModelManager]: Cannot draw model that does not exist. " + modelName);
        }

        Model thisModel = database[modelName];

        // Have to jump through some hoops to rotate the model correctly.
        Quaternion quat = Raymath.QuaternionFromEuler(rotation.X, rotation.Y, rotation.Z);
        quat.ToAxisAngle(out Vector3 axisRotation, out float angle);
        Raylib.DrawModelEx(thisModel, position, axisRotation, Raylib.RAD2DEG * angle, new Vector3(scale, scale, scale), color);
    }

    static unsafe void newModelFromMesh(string modelName, float[] vertices, float[] textureCoordinates, bool dynamic = false) {

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

    static void loadModelFromFile(string path) {

        if (!File.Exists(path)) {
            throw new Exception($"[ModelManager]: {path} is not a file.");
        }

        if (!path.EndsWith(".png", StringComparison.OrdinalIgnoreCase)) {
            throw new Exception($"[ModelManager]: {path} is not a png.");
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



        ModelAnimation[] animationArray = Raylib.LoadModelAnimations(path).ToArray();

        AnimationContainer thisModelAnimation = new() {
            animationCount = animationArray.Length,
            animationData = animationArray,
            hasAnimation = animationArray.Length > 0
        };

        database[fileName] = thisModel;
        isCustomDatabase[fileName] = false;
        animationDatabase[fileName] = thisModelAnimation;
    }

    static void setModelTexture(string modelName, string textureName) {

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

    static void setModelShader(string modelName, string shaderName) {

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


    static Model getModel(string modelName) {
        if (!database.ContainsKey(modelName)) {
            throw new Exception("[ModelManager]: Tried to set get non-existent model pointer [" + modelName + "]");
        }
        return database[modelName];
    }

    static void updateModelPositionsInGPU(string modelName) {
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

    static void destroy(string modelName) {
        if (!database.ContainsKey(modelName)) {
            throw new Exception("[ModelManager]: Tried to destroy non-existent model. " + modelName);
        }

        Model thisModel = database[modelName];

        destroyModel(modelName, thisModel);

        database.Remove(modelName);
        isCustomDatabase.Remove(modelName);
        animationDatabase.Remove(modelName);
    }

    static void terminate() {
        foreach (var (modelName, thisModel) in database) {
            destroyModel(modelName, thisModel);
        }
        database.Clear();
        isCustomDatabase.Clear();
        animationDatabase.Clear();
    }

    static void playAnimation(string modelName, int index, int frame) {
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

    static AnimationContainer getAnimationContainer(string modelName) {
        if (!animationDatabase.ContainsKey(modelName)) {
            throw new Exception("[ModelManager]: Tried to get non-existent animation container. " + modelName);
        }
        return animationDatabase[modelName];
    }



    static unsafe void destroyModel(string modelName, Model thisModel) {
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