using System.Numerics;
using FishGame.Graphics;
using FishGame.Utility;

namespace FishGame.Level;

// todo: rename this to GroundManager.
public static class Ground {
    static float[][] mapData;
    static int mapWidth = 0;
    static int mapHeight = 0;
    static string currentMap = "";
    static bool loaded = false;

    static float groundShimmerRoll = 0.0f;

    static int waterHeightUniformLocation = -1;
    static int shimmerRollUniformLocation = -1;
    static int groundScaleUniformLocation = -1;

    readonly static float groundScale = 7.0f;

    static void Draw() {
        ModelManager.Draw("ground", new Vector3(0, 0, 0));
    }

    static void Load(string levelLocation) {
        if (loaded) {
            throw new Exception("[Ground]: Clean up the ground.");
        }
        LoadMapData(levelLocation + "height_map.png");

        CreateGroundMesh();

        TextureManager.LoadTexture(levelLocation + "texture_map.png");

        ModelManager.SetModelTexture("ground", "texture_map.png");

        ModelManager.SetModelShader("ground", "ground");

        waterHeightUniformLocation = ShaderManager.GetUniformLocation("ground", "waterHeight");

        shimmerRollUniformLocation = ShaderManager.GetUniformLocation("ground", "shimmerRoll");

        groundScaleUniformLocation = ShaderManager.GetUniformLocation("ground", "groundScale");

        ShaderManager.SetFloatUniformFloat("ground", groundScaleUniformLocation, groundScale);

        loaded = true;
    }

    static void SetWaterLevel(float newWaterLevel) {
        ShaderManager.SetFloatUniformFloat("ground", waterHeightUniformLocation, newWaterLevel);
    }

    static (int, int) GetSize() {
        return (mapWidth, mapHeight);
    }

    static Vector2 GetSizeFloating() {
        return new Vector2(mapWidth, mapHeight);
    }

    static float GetWidth() {
        return mapWidth;
    }

    static float GetHeight() {
        return mapHeight;
    }

    static float GetCollisionPoint(float x, float y) {
        return HeightCalculation(new Vector2(x, y));
    }

    static void Update() {
        float delta = Delta.Get();
        groundShimmerRoll += delta / 2.0f;
        ShaderManager.SetFloatUniformFloat("ground", shimmerRollUniformLocation, groundShimmerRoll);
    }


    //? This starts the internal parts of the api.

    static float GetHeightAtNode(int x, int y) {
        return mapData[x][y];
    }

    static float HeightCalculation(Vector2 point) {

        // todo: clamp this inside the map after the other clamps are added.

        int x = (int)Math.Floor(point.X);
        int y = (int)Math.Floor(point.Y);

        Vector2[4] pData = [
            Vector2(x, y),
            Vector2(x, y + 1),
            Vector2(x + 1, y + 1),
            Vector2(x + 1, y),
        ];

        const int inPoint = () {
            if (pointInTriangle(Vector2(point.X, point.Y), pData[0], pData[1], pData[2])) {
                return 1;
            } else if (pointInTriangle(Vector2(point.X, point.Y), pData[2], pData[3], pData[0])) {
                return 2;
            }
            throw new Error("In non-existent position.");
        }
        ();

        float[4] heightData = [
            GetHeightAtNode(x, y),
            GetHeightAtNode(x, y + 1),
            GetHeightAtNode(x + 1, y + 1),
            GetHeightAtNode(x + 1, y)
        ];

        if (inPoint == 1) {

            Vector3[3] positionData = [
                Vector3(pData[0].X, heightData[0], pData[0].Y),
                Vector3(pData[1].X, heightData[1], pData[1].Y),
                Vector3(pData[2].X, heightData[2], pData[2].Y),
            ];

            DrawLine3D(positionData[0], positionData[1], Colors.RED);
            DrawLine3D(positionData[1], positionData[2], Colors.RED);
            DrawLine3D(positionData[0], positionData[2], Colors.RED);

            return calculateY(positionData[0], positionData[1], positionData[2], point);

        } else {
            Vector3[3] positionData = [
                Vector3(pData[2].X, heightData[2], pData[2].Y),
                Vector3(pData[3].X, heightData[3], pData[3].Y),
                Vector3(pData[0].X, heightData[0], pData[0].Y),
            ];

            DrawLine3D(positionData[0], positionData[1], Colors.RED);
            DrawLine3D(positionData[1], positionData[2], Colors.RED);
            DrawLine3D(positionData[0], positionData[2], Colors.RED);

            return calculateY(positionData[0], positionData[1], positionData[2], point);
        }
    }

    static void CreateGroundMesh() {
        import raylib;

        float[] vertices = new float[](0);
        float[] textureCoordinates = new float[](0);

        foreach (x; 0..mapWidth) {
            foreach (y; 0..mapHeight) {

                // Raylib is still absolutely ancient with ushort as the indices so I have to convert this mess into raw vertex tris.

                const float[4] heightData = [
                    GetHeightAtNode(x, y), // 0 - Top Left.
                    GetHeightAtNode(x, y + 1), // 1 - Bottom Left.
                    GetHeightAtNode(x + 1, y + 1), // 2 - Bottom Right.
                    GetHeightAtNode(x + 1, y), // 3 - Top Right.
                ];

                const Vector3[4] vData = [
                    Vector3(x, heightData[0], y), // 0
                    Vector3(x, heightData[1], y + 1), // 1
                    Vector3(x + 1, heightData[2], y + 1), // 2
                    Vector3(x + 1, heightData[3], y) // 3
                ];

                vertices ~= [
                    // Tri 1.
                    vData[0].X, vData[0].Y, vData[0].z,
                    vData[1].X, vData[1].Y, vData[1].z,
                    vData[2].X, vData[2].Y, vData[2].z,
                    // Tri 2.
                    vData[2].X, vData[2].Y, vData[2].z,
                    vData[3].X, vData[3].Y, vData[3].z,
                    vData[0].X, vData[0].Y, vData[0].z,
                ];

                // Same with the texture coordinate data.

                // todo: make this read from a texture map.
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

        ModelManager.newModelFromMesh("ground", vertices, textureCoordinates);

        //todo: set the ground texture from a pallete thing.
    }

    static void LoadMapData(string location) {
        Image image;

        LoadImage(location, &image);

        CheckImage(location, &image);

        // -1 because these pixels make quads.
        mapWidth = image.width - 1;
        mapHeight = image.height - 1;

        mapData = new float[][](image.width, image.height);

        for (int y = 0; y < image.height; y++) {

            ushort* scan = cast(ushort *) image.scanptr(y);

            for (int x = 0; x < image.width(); x++) {

                ushort rawPixelValue = scan[x];

                float floatingPixelValue = cast(float) rawPixelValue;

                float finalValue = floatingPixelValue / (cast(float) ushort.max);

                mapData[x][y] = (finalValue - 0.5) * groundScale;
            }
        }

        //? I just left this here in case I need more testing.
        // foreach (x; 0 .. image.width) {
        //     foreach (y; 0 .. image.height) {
        //         writeln(x, " ", y, " ", mapData[x][y]);
        //     }
        // }
    }

    static void LoadImage(string location, Image* image) {

        if (!endsWith(location, ".png")) {
            throw new Exception("[Heightmap]: Not .png");
        }

        string[] data = split(location, "/");
        if (data.length <= 1) {
            throw new Exception("[Heightmap]: Do not put heightmaps in the root.");
        }
        const string output = data[cast(long) data.length - 1];
        if (output.length <= 0) {
            throw new Exception("[Heightmap]: String became 0 length.");
        }

        image.loadFromFile(location);
    }

    static void CheckImage(string location, Image* image) {
        if (image.isError()) {
            throw new Exception(cast(string) image.errorMessage() ~". " ~location);
        }

        if (!image.isValid) {
            throw new Exception("[Heightmap]: Invalid image. " ~location);
        }

        if (!image.is16Bit()) {
            throw new Exception("[Heightmap]: Not 16 bit. " ~location);
        }

        if (image.type() != PixelType.l16) {
            throw new Exception("[Heightmap]: Wrong endianness. " ~location);
        }
    }
}