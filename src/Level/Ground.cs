using System.Numerics;
using FishGame.Graphics;
using FishGame.Utility;
using Raylib_cs;

namespace FishGame.Level;

// todo: rename this to GroundManager.
public static class Ground {
    static float[,] mapData = new float[0, 0];
    static int mapWidth = 0;
    static int mapHeight = 0;
    static string currentLevelLocation = "";
    static bool loaded = false;

    static float groundShimmerRoll = 0.0f;

    static int waterHeightUniformLocation = -1;
    static int shimmerRollUniformLocation = -1;
    static int groundScaleUniformLocation = -1;

    readonly static float groundScale = 7.0f;

    public static void Draw() {
        ModelManager.Draw("ground", new Vector3(0, 0, 0));
    }

    public static void Load(string levelLocation) {
        if (loaded) {
            throw new Exception("[Ground]: Clean up the ground.");
        }

        currentLevelLocation = levelLocation;

        LoadMapData(currentLevelLocation + "height_map.png");

        CreateGroundMesh();

        TextureManager.LoadTexture(currentLevelLocation + "texture_map.png");

        ModelManager.SetModelTexture("ground", "texture_map.png");

        ModelManager.SetModelShader("ground", "ground");

        waterHeightUniformLocation = ShaderManager.GetUniformLocation("ground", "waterHeight");

        shimmerRollUniformLocation = ShaderManager.GetUniformLocation("ground", "shimmerRoll");

        groundScaleUniformLocation = ShaderManager.GetUniformLocation("ground", "groundScale");

        ShaderManager.SetFloatUniformFloat("ground", groundScaleUniformLocation, groundScale);

        loaded = true;
    }

    public static void SetWaterLevel(float newWaterLevel) {
        ShaderManager.SetFloatUniformFloat("ground", waterHeightUniformLocation, newWaterLevel);
    }

    public static (int, int) GetSize() {
        return (mapWidth, mapHeight);
    }

    public static Vector2 GetSizeFloating() {
        return new Vector2(mapWidth, mapHeight);
    }

    public static float GetWidth() {
        return mapWidth;
    }

    public static float GetHeight() {
        return mapHeight;
    }

    public static float GetCollisionPoint(float x, float y) {
        return HeightCalculation(new Vector2(x, y));
    }

    public static void Update() {
        float delta = Delta.Get();
        groundShimmerRoll += delta / 2.0f;
        ShaderManager.SetFloatUniformFloat("ground", shimmerRollUniformLocation, groundShimmerRoll);
    }


    //? This starts the internal parts of the api.

    static float GetHeightAtNode(int x, int y) {
        return mapData[x, y];
    }

    static float HeightCalculation(Vector2 point) {

        // todo: clamp this inside the map after the other clamps are added.

        int x = (int)Math.Floor(point.X);
        int y = (int)Math.Floor(point.Y);

        Vector2[] pData = [
            new Vector2(x, y),
            new Vector2(x, y + 1),
            new Vector2(x + 1, y + 1),
            new Vector2(x + 1, y),
        ];

        int GetInPoint() {
            if (CollisionMath.PointInTriangle(new Vector2(point.X, point.Y), pData[0], pData[1], pData[2])) {
                return 1;
            } else if (CollisionMath.PointInTriangle(new Vector2(point.X, point.Y), pData[2], pData[3], pData[0])) {
                return 2;
            }
            throw new Exception("In non-existent position.");
        }
        int inPoint = GetInPoint();

        float[] heightData = [
            GetHeightAtNode(x, y),
            GetHeightAtNode(x, y + 1),
            GetHeightAtNode(x + 1, y + 1),
            GetHeightAtNode(x + 1, y)
        ];

        if (inPoint == 1) {

            Vector3[] positionData = [
                new Vector3(pData[0].X, heightData[0], pData[0].Y),
                new Vector3(pData[1].X, heightData[1], pData[1].Y),
                new Vector3(pData[2].X, heightData[2], pData[2].Y),
            ];

            Raylib.DrawLine3D(positionData[0], positionData[1], Color.Red);
            Raylib.DrawLine3D(positionData[1], positionData[2], Color.Red);
            Raylib.DrawLine3D(positionData[0], positionData[2], Color.Red);

            return CollisionMath.CalculateY(positionData[0], positionData[1], positionData[2], point);

        } else {
            Vector3[] positionData = [
                new Vector3(pData[2].X, heightData[2], pData[2].Y),
                new Vector3(pData[3].X, heightData[3], pData[3].Y),
                new Vector3(pData[0].X, heightData[0], pData[0].Y),
            ];

            Raylib.DrawLine3D(positionData[0], positionData[1], Color.Red);
            Raylib.DrawLine3D(positionData[1], positionData[2], Color.Red);
            Raylib.DrawLine3D(positionData[0], positionData[2], Color.Red);

            return CollisionMath.CalculateY(positionData[0], positionData[1], positionData[2], point);
        }
    }

    static void CreateGroundMesh() {

        List<float> vertices = [];
        List<float> textureCoordinates = [];

        for (int x = 0; x < mapWidth; x++) {
            for (int y = 0; y < mapWidth; y++) {

                // Raylib is still absolutely ancient with ushort as the indices so I have to convert this mess into raw vertex tris.

                float[] heightData = [
                   GetHeightAtNode(x, y), // 0 - Top Left.
                    GetHeightAtNode(x, y + 1), // 1 - Bottom Left.
                    GetHeightAtNode(x + 1, y + 1), // 2 - Bottom Right.
                    GetHeightAtNode(x + 1, y), // 3 - Top Right.
                ];

                Vector3[] vData = [
                   new Vector3(x, heightData[0], y), // 0
                    new Vector3(x, heightData[1], y + 1), // 1
                    new Vector3(x + 1, heightData[2], y + 1), // 2
                    new Vector3(x + 1, heightData[3], y) // 3
               ];

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

                // Same with the texture coordinate data.

                // todo: make this read from a texture map.
                Vector2[] tData = [
                   new Vector2(0.0f, 0.0f), // 0 top left.
                    new Vector2(0.0f, 1.0f), // 1 bottom left
                    new Vector2(1.0f, 1.0f), // 2 bottom right.
                    new Vector2(1.0f, 0.0f), // 3 top right.
                ];

                textureCoordinates.AddRange([
                    // Tri 1.
                    tData[0].X, tData[0].Y,
                    tData[1].X, tData[1].Y,
                    tData[2].X, tData[2].Y,
                    // Tri 2.
                    tData[2].X, tData[2].Y,
                    tData[3].X, tData[3].Y,
                    tData[0].X, tData[0].Y,
                ]);
            }
        }

        ModelManager.NewModelFromMesh("ground", vertices.ToArray(), textureCoordinates.ToArray());

        //todo: set the ground texture from a pallete thing.
    }

    static unsafe void LoadMapData(string location) {
        // 1. Load the Raylib Image struct
        Image image = Raylib.LoadImage(location);

        // Validation check
        if (image.Data == null) {
            throw new FileNotFoundException($"[Ground]: Failed to load heightmap image at: {location}");
        }

        // -1 because these pixels make quads.
        mapWidth = image.Width - 1;
        mapHeight = image.Height - 1;

        mapData = new float[image.Width, image.Height];

        ushort* pixels = (ushort*)image.Data;

        int width = image.Width;
        int height = image.Height;

        for (int y = 0; y < height; y++) {

            ushort* rowScan = pixels + (y * width);

            for (int x = 0; x < width; x++) {
                ushort rawPixelValue = rowScan[x];

                float floatingPixelValue = rawPixelValue;

                float finalValue = floatingPixelValue / ushort.MaxValue;

                mapData[x, y] = (finalValue - 0.5f) * groundScale;
            }
        }
        Raylib.UnloadImage(image);
    }
}