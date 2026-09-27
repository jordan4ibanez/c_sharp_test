using Raylib_cs;

namespace FishGame.Graphics;

public static class ShaderManager {

    static Dictionary<string, Shader> database = [];


    static void NewShader(string shaderName, string vertCodeLocation, string fragCodeLocation) {

        if (shaderName in database) {
            throw new Error("[ShaderHandler]: Tried to overwrite shader " ~shaderName);
        }

        Shader* thisShader = new Shader();
        *thisShader = LoadShader(toStringz(vertCodeLocation), toStringz(fragCodeLocation));

        if (!IsShaderValid(*thisShader)) {
            throw new Error("[ShaderHandler]: Invalid shader. " ~shaderName);
        }

        database[shaderName] = thisShader;
    }

    static int GetUniformLocation(string shaderName, string uniformName) {
        if (shaderName!in database) {
            throw new Error(
                "[ShaderHandler]: Tried to get non-existent shader. " ~shaderName);
        }

        int val = GetShaderLocation(*database[shaderName], toStringz(uniformName));

        if (val == -1) {
            throw new Error(
                "[ShaderHandler]: Uniform " ~uniformName ~" does not exist for shader. " ~shaderName);
        }

        return val;
    }

    static Shader* GetShaderPointer(string shaderName) {
        if (shaderName!in database) {
            throw new Error(
                "[ShaderHandler]: Tried to get non-existent shader pointer. " ~shaderName);
        }
        return database[shaderName];
    }

    static void SetFloatUniformFloat(string shaderName, int location, float value) {
        if (shaderName!in database) {
            throw new Error(
                "[ShaderHandler]: Tried to set uniform in non-existent shader. " ~shaderName);
        }

        SetShaderValue(*database[shaderName], location, &value,
            ShaderUniformDataType.SHADER_UNIFORM_FLOAT);
    }

    static void Terminate() {
        foreach (shaderName, thisShader; database) {
            UnloadShader(*thisShader);
        }

        database.clear();
    }
}