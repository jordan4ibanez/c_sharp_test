using System.Numerics;
using System.Runtime.InteropServices;
using FishGame.Utility;
using Raylib_cs;

namespace FishGame.Graphics;

class AnimationContainer {
    public int animationCount = 0;
    public bool hasAnimation = false;
    public ModelAnimation animationData;
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

        int animationCount;
        ModelAnimation thisAnimationData;
        unsafe {
            thisAnimationData = *Raylib.LoadModelAnimations((sbyte*)Marshal.StringToHGlobalAnsi(path), &animationCount);
        }
        AnimationContainer thisModelAnimation = new AnimationContainer();
        thisModelAnimation.animationCount = animationCount;
        thisModelAnimation.animationData = thisAnimationData;
        thisModelAnimation.hasAnimation = thisAnimationData.KeyFrameCount > 0;

        database[fileName] = thisModel;
        isCustomDatabase[fileName] = false;
        animationDatabase[fileName] = thisModelAnimation;
    }

    static void setModelTexture(string modelName, string textureName) {

        if (modelName!in database) {
            throw new Error(
                "[ModelManager]: Tried to set texture on non-existent model [" ~modelName ~"]");
        }

        Model* thisModel = database[modelName];
        Texture2D* thisTexture = TextureHandler.getTexturePointer(textureName);

        foreach (index; 0..thisModel.materialCount) {
            thisModel.materials[index].maps[MATERIAL_MAP_DIFFUSE].texture = *thisTexture;
        }
    }

    static void setModelShader(string modelName, string shaderName) {

        if (modelName!in database) {
            throw new Error(
                "[ModelManager]: Tried to set shader on non-existent model [" ~modelName ~"]");
        }

        Model* thisModel = database[modelName];
        Shader* thisShader = ShaderHandler.getShaderPointer(shaderName);
        foreach (index; 0..thisModel.materialCount) {
            thisModel.materials[index].shader = *thisShader;
        }
    }

    static Model* getModelPointer(string modelName) {
        if (modelName!in database) {
            throw new Error(
                "[ModelManager]: Tried to set get non-existent model pointer [" ~modelName ~"]");
        }

        return database[modelName];
    }

    static void updateModelPositionsInGPU(string modelName) {
        if (modelName!in database) {
            throw new Error(
                "[ModelManager]: Tried to update non-existent model [" ~modelName ~"]");
        }

        const Model* thisModel = database[modelName];

        /*
#define RL_DEFAULT_SHADER_ATTRIB_LOCATION_POSITION    0
#define RL_DEFAULT_SHADER_ATTRIB_LOCATION_TEXCOORD    1
#define RL_DEFAULT_SHADER_ATTRIB_LOCATION_NORMAL      2
#define RL_DEFAULT_SHADER_ATTRIB_LOCATION_COLOR       3
#define RL_DEFAULT_SHADER_ATTRIB_LOCATION_TANGENT     4
#define RL_DEFAULT_SHADER_ATTRIB_LOCATION_TEXCOORD2   5
#define RL_DEFAULT_SHADER_ATTRIB_LOCATION_INDICES     6
        */

        foreach (i, thisMesh; thisModel.meshes[0..thisModel.meshCount]) {
            UpdateMeshBuffer(cast(Mesh) thisMesh, 0, &thisMesh.vertices[0], cast(int)(
                    thisMesh.vertexCount * 3 * float.sizeof), 0);
        }
    }

    static void destroy(string modelName) {
        if (modelName!in database) {
            throw new Error("[ModelManager]: Tried to destroy non-existent model. " ~modelName);
        }

        Model* thisModel = database[modelName];

        destroyModel(modelName, thisModel);

        database.remove(modelName);
        isCustomDatabase.remove(modelName);
        animationDatabase.remove(modelName);
    }

    static void terminate() {
        foreach (modelName, thisModel; database) {
            destroyModel(modelName, thisModel);
        }
        database.clear();
        isCustomDatabase.clear();
        animationDatabase.clear();
    }

    static void playAnimation(string modelName, int index, int frame) {
        if (modelName!in database) {
            throw new Error(
                "[ModelManager]: Tried to play animation on non-existent model. " ~modelName);
        }

        Model* thisModel = database[modelName];

        AnimationContainer thisAnimation = animationDatabase[modelName];

        if (thisAnimation is null) {
            throw new Error(
                "[ModelManager]: Tried to play animation on model with no animation. " ~modelName);
        }
        UpdateModelAnimation(*thisModel, thisAnimation.animationData[index], frame);
    }

    static AnimationContainer getAnimationContainer(string modelName) {
        if (modelName!in animationDatabase) {
            throw new Error(
                "[ModelManager]: Tried to get non-existent animation container. " ~modelName);
        }

        return animationDatabase[modelName];
    }

    private:

    static void destroyModel(string modelName, Model* thisModel) {
        // If we were using the D runtime to make this model, we'll customize
        // the way we free the items. This makes the GC auto clear.
        if (isCustomDatabase[modelName]) {
            Mesh thisMeshInModel = thisModel.meshes[0];
            thisMeshInModel.vertexCount = 0;
            thisMeshInModel.vertices = null;
            thisMeshInModel.texcoords = null;
            UnloadMesh(thisMeshInModel);
            thisModel.meshes = null;
            thisModel.meshCount = 0;
            UnloadModel(*thisModel);
        } else {
            UnloadModel(*thisModel);
            AnimationContainer thisAnimations = animationDatabase[modelName];
            if (thisAnimations! is null && thisAnimations.hasAnimation) {
                UnloadModelAnimations(thisAnimations.animationData, animationDatabase[modelName]
                        .animationCount);
            }
        }
    }

}