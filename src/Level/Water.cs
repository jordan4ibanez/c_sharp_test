using System.Numerics;
using FishGame.Graphics;
using FishGame.Utility;
using Raylib_cs;

namespace FishGame.Level;

public static class Water {

    // water is 0.25 unit quads.
    // Level size x * 4 and y * 4
    // Try to update with sin cos etc.

    static float waterRoll = 0;
    static float waveScale = 10;
    static float waveMagnitude = 0.01f;

    // Water has 39 frames.
    readonly static int minWaterTextureFrame = 0;
    readonly static int maxWaterTextureFrame = 39;
    static int currentWaterFrame = 0;

    static bool loaded = false;

    readonly static float tileWidth = 0.25f;

    // This is how high the water is.
    static float waterLevel = 2.0f;

    static int waterWidth = 0;
    static int waterHeight = 0;

    static float[,] waterData = new float[0, 0];

    static FastNoiseLite noise = new();

    static int waterHeightUniformLocation = -1;

    //? Water frequently updates, so this is implemented in a special way.

    static void Draw() {
        ModelManager.Draw("water", new Vector3(0, 0, 0), new Vector3(0, 0, 0), 1.0f, new Color(200, 200, 200, 200));
    }

    static float GetWaterLevel() {
        return waterLevel;
    }

    static void Load() {

        (int, int) groundSize = Ground.GetSize();

        if (loaded) {
            throw new Exception("Clean up the water gpu memory or reuse it.");
        } else {
            // foreach (i; minWaterTextureFrame .. maxWaterTextureFrame + 1) {
            TextureManager.LoadTexture("textures/water.png");
            // }
        }




        noise.SetSeed(Randy.NextInt(0, 1000000));
        noise.SetNoiseType(FastNoiseLite.NoiseType.Value);
        noise.SetFrequency(1);

        waterWidth = groundSize.Item1 * 4;
        waterHeight = groundSize.Item2 * 4;

        waterData = new float[waterWidth + 1, waterHeight + 1];

        resetWaterData();

        float[] vertices = loadVertices();
        float[] textureCoordinates = loadTextureCoordinates();

        ModelManager.NewModelFromMesh("water", vertices, textureCoordinates, true);
        ModelManager.SetModelTexture("water", "water.png");
        ModelManager.SetModelShader("water", "water");

        waterHeightUniformLocation = ShaderManager.GetUniformLocation("water", "waterHeight");
        ShaderManager.SetFloatUniformFloat("water", waterHeightUniformLocation, waterLevel);

        Ground.SetWaterLevel(waterLevel);

        loaded = true;
    }

    static float waterUpdateTimer = 0.0f;
    static float targetTime = 1.0f / 15.0f;
    static float waveSpeed = 0.5f;
    static byte skip = 0;

    static void Update() {

        float delta = Delta.Get();

        waterUpdateTimer += delta;

        waterRoll += (delta * waveSpeed);

        if (waterUpdateTimer <= targetTime) {
            return;
        }
        waterUpdateTimer -= targetTime;

        foreach (x; 0..waterWidth + 1) {
            foreach (y; 0..waterHeight + 1) {

                waterData[x][y] = waterLevel + (noise.fnlGetNoise2D(((x * tileWidth) * waveScale) + waterRoll, (
                        (y * tileWidth) * waveScale) + waterRoll) * waveMagnitude);

            }
        }

        // This also automatically uploads the new water data into the gpu.
        Model* thisModel = ModelManager.getModelPointer("water");
        Mesh* thisMesh = thisModel.meshes;

        float[] blah = thisMesh.vertices[0..thisMesh.vertexCount * 3];

        // writeln("blah: ", blah.length);

        uint i = 0;
        foreach (x; 0..waterWidth) {
            foreach (y; 0..waterHeight) {

                const float[4] vData = [
                    waterData[x][y], // 0
                    waterData[x][y + 1], // 1
                    waterData[x + 1][y + 1], // 2
                    waterData[x + 1][y], // 3
                ];
                // x0, y1,  z2
                // x3, y4,  z5
                // x6, y7,  z8

                // x9, y10,  z11
                // x12, y13,  z14
                // x15, y16,  z17

                blah[i + 1] = vData[0];
                blah[i + 4] = vData[1];
                blah[i + 7] = vData[2];

                blah[i + 10] = vData[2];
                blah[i + 13] = vData[3];
                blah[i + 16] = vData[0];

                i += 18;
            }
        }

        ModelManager.updateModelPositionsInGPU("water");
    }

    static float getCollisionPoint(float x, float y) {
        return heightCalculation(Vector2(x, y));
    }

    private:

    static float getHeightAtNode(int x, int y) {
        return waterData[x][y];
    }

    static float heightCalculation(Vector2 point) {
        import raylib;

        // todo: clamp this inside the map after the other clamps are added.

        int adjustedX = cast(int) floor(point.x / tileWidth);
        int adjustedY = cast(int) floor(point.y / tileWidth);

        float scaledx = adjustedX * tileWidth;
        float scaledY = adjustedY * tileWidth;

        Vector2[4] pData = [
            Vector2(scaledx, scaledY),
            Vector2(scaledx, scaledY + tileWidth),
            Vector2(scaledx + tileWidth, scaledY + tileWidth),
            Vector2(scaledx + tileWidth, scaledY),
        ];

        const int inPoint = () {
            if (pointInTriangle(point, pData[0], pData[1], pData[2])) {
                return 1;
            } else if (pointInTriangle(point, pData[2], pData[3], pData[0])) {
                return 2;
            }
            throw new Exception("In non-existent position.");
        }
        ();

        float[4] heightData = [
            getHeightAtNode(adjustedX, adjustedY),
            getHeightAtNode(adjustedX, adjustedY + 1),
            getHeightAtNode(adjustedX + 1, adjustedY + 1),
            getHeightAtNode(adjustedX + 1, adjustedY)
        ];

        if (inPoint == 1) {

            Vector3[3] positionData = [
                Vector3(pData[0].x, heightData[0], pData[0].y),
                Vector3(pData[1].x, heightData[1], pData[1].y),
                Vector3(pData[2].x, heightData[2], pData[2].y),
            ];

            DrawLine3D(positionData[0], positionData[1], Colors.GREEN);
            DrawLine3D(positionData[1], positionData[2], Colors.GREEN);
            DrawLine3D(positionData[0], positionData[2], Colors.GREEN);

            return calculateY(positionData[0], positionData[1], positionData[2], point);

        } else {
            Vector3[3] positionData = [
                Vector3(pData[2].x, heightData[2], pData[2].y),
                Vector3(pData[3].x, heightData[3], pData[3].y),
                Vector3(pData[0].x, heightData[0], pData[0].y),
            ];

            DrawLine3D(positionData[0], positionData[1], Colors.GREEN);
            DrawLine3D(positionData[1], positionData[2], Colors.GREEN);
            DrawLine3D(positionData[0], positionData[2], Colors.GREEN);

            return calculateY(positionData[0], positionData[1], positionData[2], point);
        }
    }

    static void resetWaterData() {

        foreach (x; 0..waterWidth + 1) {
            foreach (y; 0..waterHeight + 1) {
                waterData[x][y] = waterLevel;
            }
        }
    }

    static float[] loadVertices() {
        float[] vertices = new float[](0);

        // todo: updateVertices will reuse the pointer in place
        // todo: from the height data and reupload in place.

        foreach (x; 0..waterWidth) {
            foreach (y; 0..waterHeight) {

                immutable float sx = x * tileWidth;
                immutable float sy = y * tileWidth;

                const Vector3[4] vData = [
                    Vector3(sx, waterData[x][y], sy), // 0
                    Vector3(sx, waterData[x][y + 1], sy + tileWidth), // 1
                    Vector3(sx + tileWidth, waterData[x + 1][y + 1], sy + tileWidth), // 2
                    Vector3(sx + tileWidth, waterData[x + 1][y], sy) // 3
                ];
                // writeln(waterData[x][y]);

                vertices ~= [
                    // Tri 1.
                    vData[0].x, vData[0].y, vData[0].z,
                    vData[1].x, vData[1].y, vData[1].z,
                    vData[2].x, vData[2].y, vData[2].z,
                    // Tri 2.
                    vData[2].x, vData[2].y, vData[2].z,
                    vData[3].x, vData[3].y, vData[3].z,
                    vData[0].x, vData[0].y, vData[0].z,
                ];
            }
        }
        return vertices;
    }

    static float[] loadTextureCoordinates() {
        float[] textureCoordinates = new float[](0);

        foreach (x; 0..waterWidth) {
            foreach (y; 0..waterHeight) {
                const Vector2[4] tData = [
                    Vector2(0.0, 0.0), // 0 top left.
                    Vector2(0.0, 1.0), // 1 bottom left
                    Vector2(1.0, 1.0), // 2 bottom right.
                    Vector2(1.0, 0.0), // 3 top right.
                ];

                textureCoordinates ~= [
                    // Tri 1.
                    tData[0].x, tData[0].y,
                    tData[1].x, tData[1].y,
                    tData[2].x, tData[2].y,
                    // Tri 2.
                    tData[2].x, tData[2].y,
                    tData[3].x, tData[3].y,
                    tData[0].x, tData[0].y,
                ];
            }
        }

        return textureCoordinates;
    }
}