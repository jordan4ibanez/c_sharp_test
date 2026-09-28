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

    public static void Draw() {
        ModelManager.Draw("water", new Vector3(0, 0, 0), new Vector3(0, 0, 0), 1.0f, new Color(200, 200, 200, 200));
    }

    public static float GetWaterLevel() {
        return waterLevel;
    }

    public static void Load() {

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

        ResetWaterData();

        float[] vertices = LoadVertices();
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

    public static unsafe void Update() {

        float delta = Delta.Get();

        waterUpdateTimer += delta;

        waterRoll += delta * waveSpeed;

        if (waterUpdateTimer <= targetTime) {
            return;
        }
        waterUpdateTimer -= targetTime;

        for (int x = 0; x < waterWidth + 1; x++) {
            for (int y = 0; y < waterHeight + 1; y++) {
                waterData[x, y] = waterLevel + (noise.GetNoise((x * tileWidth * waveScale) + waterRoll, (y * tileWidth * waveScale) + waterRoll) * waveMagnitude);
            }
        }

        // This also automatically uploads the new water data into the gpu.
        Model thisModel = ModelManager.GetModel("water");
        Mesh thisMesh = thisModel.Meshes[0];

        // float[] blah = thisMesh.Vertices[0..thisMesh.VertexCount * 3];

        int totalFloats = thisMesh.VertexCount * 3;
        ReadOnlySpan<float> verticesSpan = new(thisMesh.Vertices, totalFloats);
        Span<float> blah = new(thisMesh.Vertices, totalFloats);

        // writeln("blah: ", blah.length);

        int i = 0;
        for (int x = 0; x < waterWidth; x++) {
            for (int y = 0; y < waterHeight; y++) {

                float[] vData = [
                   waterData[x,y], // 0
                    waterData[x,y + 1], // 1
                    waterData[x + 1,y + 1], // 2
                    waterData[x + 1,y], // 3
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

        ModelManager.UpdateModelPositionsInGPU("water");
    }

    public static float GetCollisionPoint(float x, float y) {
        return HeightCalculation(new Vector2(x, y));
    }

    //? Begins the private section of the class.

    static float GetHeightAtNode(int x, int y) {
        return waterData[x, y];
    }

    static float HeightCalculation(Vector2 point) {

        // todo: clamp this inside the map after the other clamps are added.

        int adjustedX = (int)Math.Floor(point.X / tileWidth);
        int adjustedY = (int)Math.Floor(point.Y / tileWidth);

        float scaledx = adjustedX * tileWidth;
        float scaledY = adjustedY * tileWidth;

        Vector2[] pData = [
            new Vector2(scaledx, scaledY),
            new Vector2(scaledx, scaledY + tileWidth),
            new Vector2(scaledx + tileWidth, scaledY + tileWidth),
            new Vector2(scaledx + tileWidth, scaledY),
        ];

        int inPointCheck() {
            if (CollisionMath.PointInTriangle(point, pData[0], pData[1], pData[2])) {
                return 1;
            } else if (CollisionMath.PointInTriangle(point, pData[2], pData[3], pData[0])) {
                return 2;
            }
            throw new Exception("In non-existent position.");
        }
        int inPoint = inPointCheck();

        float[] heightData = [
            GetHeightAtNode(adjustedX, adjustedY),
            GetHeightAtNode(adjustedX, adjustedY + 1),
            GetHeightAtNode(adjustedX + 1, adjustedY + 1),
            GetHeightAtNode(adjustedX + 1, adjustedY)
        ];

        if (inPoint == 1) {

            Vector3[] positionData = [
                new Vector3(pData[0].X, heightData[0], pData[0].Y),
                new Vector3(pData[1].X, heightData[1], pData[1].Y),
                new Vector3(pData[2].X, heightData[2], pData[2].Y),
            ];

            Raylib.DrawLine3D(positionData[0], positionData[1], Color.Green);
            Raylib.DrawLine3D(positionData[1], positionData[2], Color.Green);
            Raylib.DrawLine3D(positionData[0], positionData[2], Color.Green);

            return CollisionMath.CalculateY(positionData[0], positionData[1], positionData[2], point);

        } else {
            Vector3[] positionData = [
                new Vector3(pData[2].X, heightData[2], pData[2].Y),
                new Vector3(pData[3].X, heightData[3], pData[3].Y),
                new Vector3(pData[0].X, heightData[0], pData[0].Y),
            ];

            Raylib.DrawLine3D(positionData[0], positionData[1], Color.Green);
            Raylib.DrawLine3D(positionData[1], positionData[2], Color.Green);
            Raylib.DrawLine3D(positionData[0], positionData[2], Color.Green);

            return CollisionMath.CalculateY(positionData[0], positionData[1], positionData[2], point);
        }
    }

    static void ResetWaterData() {

        for (int x = 0; x < waterWidth + 1; x++) {
            for (int y = 0; y < waterHeight + 1; y++) {
                waterData[x, y] = waterLevel;
            }
        }
    }

    static float[] LoadVertices() {
        List<float> vertices = [];

        // todo: updateVertices will reuse the pointer in place
        // todo: from the height data and reupload in place.

        for (int x = 0; x < waterWidth; x++) {
            for (int y = 0; y < waterHeight; y++) {

                float sx = x * tileWidth;
                float sy = y * tileWidth;

                Vector3[] vData = [
                   new Vector3(sx, waterData[x,y], sy), // 0
                    new Vector3(sx, waterData[x,y + 1], sy + tileWidth), // 1
                    new Vector3(sx + tileWidth, waterData[x + 1,y + 1], sy + tileWidth), // 2
                    new Vector3(sx + tileWidth, waterData[x + 1,y], sy) // 3
               ];
                // writeln(waterData[x][y]);

                vertices.AddRange([
                    // Tri 1.
                    vData[0].X, vData[0].Y, vData[0].Z,
                    vData[1].X, vData[1].Y, vData[1].Z,
                    vData[2].X, vData[2].Y, vData[2].Z,
                    // Tri 2.
                    vData[2].X, vData[2].Y, vData[2].Z,
                    vData[3].X, vData[3].Y, vData[3].Z,
                    vData[0].X, vData[0].Y, vData[0].Z,
                ]);
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
                    tData[0].X, tData[0].Y,
                    tData[1].X, tData[1].Y,
                    tData[2].X, tData[2].Y,
                    // Tri 2.
                    tData[2].X, tData[2].Y,
                    tData[3].X, tData[3].Y,
                    tData[0].X, tData[0].Y,
                ];
            }
        }

        return textureCoordinates;
    }
}