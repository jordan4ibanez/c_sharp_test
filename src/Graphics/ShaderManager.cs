using Raylib_cs;

namespace FishGame.Graphics;

public static class ShaderManager {

    static Dictionary<string, Shader> database = [];


    static void NewShader(string shaderName, string vertCodeLocation, string fragCodeLocation) {

        if (database.ContainsKey(shaderName)) {
            throw new Exception("[ShaderHandler]: Tried to overwrite shader " + shaderName);
        }

        Shader thisShader = new();
        thisShader = Raylib.LoadShader(vertCodeLocation, fragCodeLocation);

        if (!Raylib.IsShaderValid(thisShader)) {
            throw new Exception("[ShaderHandler]: Invalid shader. " + shaderName);
        }

        database[shaderName] = thisShader;
    }

    static int GetUniformLocation(string shaderName, string uniformName) {
        if (!database.ContainsKey(shaderName)) {
            throw new Exception("[ShaderHandler]: Tried to get non-existent shader. " + shaderName);
        }

        int val = Raylib.GetShaderLocation(database[shaderName], uniformName);

        if (val == -1) {
            throw new Exception("[ShaderHandler]: Uniform " + uniformName + " does not exist for shader. " + shaderName);
        }

        return val;
    }

    static Shader GetShaderPointer(string shaderName) {
        if (!database.ContainsKey(shaderName)) {
            throw new Exception("[ShaderHandler]: Tried to get non-existent shader pointer. " + shaderName);
        }
        return database[shaderName];
    }

    static void SetFloatUniformFloat(string shaderName, int location, float value) {
        if (!database.ContainsKey(shaderName)) {
            throw new Exception("[ShaderHandler]: Tried to set uniform in non-existent shader. " + shaderName);
        }

        Raylib.SetShaderValue(database[shaderName], location, value, ShaderUniformDataType.Float);
    }

    static void Terminate() {
        foreach (var (shaderName, thisShader) in database) {
            Raylib.UnloadShader(thisShader);
        }

        database.Clear();
    }
}