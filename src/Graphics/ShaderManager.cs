using Raylib_cs;

namespace FishGame.Graphics;

public static class ShaderManager {

    static readonly Dictionary<string, Shader> database = [];


    public static void NewShader(string shaderName, string vertCodeLocation, string fragCodeLocation) {

        if (database.ContainsKey(shaderName)) {
            throw new Exception("[ShaderManager]: Tried to overwrite shader " + shaderName);
        }

        Shader thisShader = Raylib.LoadShader(vertCodeLocation, fragCodeLocation);

        if (!Raylib.IsShaderValid(thisShader)) {
            throw new Exception("[ShaderManager]: Invalid shader. " + shaderName);
        }

        database[shaderName] = thisShader;
        Console.WriteLine($"[ShaderManager]: Loaded shader {shaderName}");
    }

    public static int GetUniformLocation(string shaderName, string uniformName) {
        if (database.TryGetValue(shaderName, out Shader shader)) {

            int val = Raylib.GetShaderLocation(shader, uniformName);

            if (val == -1) {
                throw new Exception("[ShaderManager]: Uniform " + uniformName + " does not exist for shader. " + shaderName);
            }

            return val;

        } else {
            throw new Exception("[ShaderManager]: Tried to get non-existent shader. " + shaderName);
        }
    }

    public static Shader GetShader(string shaderName) {
        if (database.TryGetValue(shaderName, out Shader shader)) {
            return shader;
        } else {
            throw new Exception("[ShaderManager]: Tried to get non-existent shader pointer. " + shaderName);
        }
    }

    public static void SetFloatUniformFloat(string shaderName, int location, float value) {
        if (database.TryGetValue(shaderName, out Shader shader)) {
            Raylib.SetShaderValue(shader, location, value, ShaderUniformDataType.Float);
        } else {
            throw new Exception("[ShaderManager]: Tried to set uniform in non-existent shader. " + shaderName);
        }
    }

    public static void Terminate() {
        foreach (var (shaderName, thisShader) in database) {
            Raylib.UnloadShader(thisShader);
        }

        database.Clear();
    }
}