namespace FishGame.Level;

public static class Ground {
    static float[][] mapData;
    static int mapWidth = 0;
    static int mapHeight = 0;
    static string currentMap = null;
    static bool loaded = false;

    static float groundShimmerRoll = 0.0;

    static int waterHeightUniformLocation = -1;
    static int shimmerRollUniformLocation = -1;
    static int groundScaleUniformLocation = -1;

    immutable static float groundScale = 7.0;

    static void draw() {
        ModelHandler.draw("ground", Vector3(0, 0, 0));
    }

    static void load(string levelLocation) {
        if (loaded) {
            throw new Error("Clean up the ground.");
        }
        loadMapData(levelLocation ~"height_map.png");
        createGroundMesh();
        TextureHandler.loadTexture(levelLocation ~"texture_map.png");
        ModelHandler.setModelTexture("ground", "texture_map.png");

        ModelHandler.setModelShader("ground", "ground");

        waterHeightUniformLocation = ShaderHandler.getUniformLocation("ground", "waterHeight");

        shimmerRollUniformLocation = ShaderHandler.getUniformLocation("ground", "shimmerRoll");

        groundScaleUniformLocation = ShaderHandler.getUniformLocation("ground", "groundScale");

        ShaderHandler.setFloatUniformFloat("ground", groundScaleUniformLocation, groundScale);

        loaded = true;
    }

    static void setWaterLevel(float newWaterLevel) {
        ShaderHandler.setFloatUniformFloat("ground", waterHeightUniformLocation, newWaterLevel);
    }

    Tuple!static (int, int) getSize() {
        return tuple(mapWidth, mapHeight);
    }

    static Vector2 getSizeFloating() {
        return Vector2(mapWidth, mapHeight);
    }

    static float getWidth() {
        return mapWidth;
    }

    static float getHeight() {
        return mapHeight;
    }

    static float getCollisionPoint(float x, float y) {
        return heightCalculation(Vector2(x, y));
    }

    static void update() {
        immutable float delta = Delta.getDelta();
        groundShimmerRoll += delta / 2.0;
        ShaderHandler.setFloatUniformFloat("ground", shimmerRollUniformLocation, groundShimmerRoll);
    }

    private:

    static float getHeightAtNode(int x, int y) {
        return mapData[x][y];
    }

    static float heightCalculation(Vector2 point) {
        import raylib;

        // todo: clamp this inside the map after the other clamps are added.

        int x = cast(int) floor(point.x);
        int y = cast(int) floor(point.y);

        Vector2[4] pData = [
            Vector2(x, y),
            Vector2(x, y + 1),
            Vector2(x + 1, y + 1),
            Vector2(x + 1, y),
        ];

        const int inPoint = () {
            if (pointInTriangle(Vector2(point.x, point.y), pData[0], pData[1], pData[2])) {
                return 1;
            } else if (pointInTriangle(Vector2(point.x, point.y), pData[2], pData[3], pData[0])) {
                return 2;
            }
            throw new Error("In non-existent position.");
        }
        ();

        float[4] heightData = [
            getHeightAtNode(x, y),
            getHeightAtNode(x, y + 1),
            getHeightAtNode(x + 1, y + 1),
            getHeightAtNode(x + 1, y)
        ];

        if (inPoint == 1) {

            Vector3[3] positionData = [
                Vector3(pData[0].x, heightData[0], pData[0].y),
                Vector3(pData[1].x, heightData[1], pData[1].y),
                Vector3(pData[2].x, heightData[2], pData[2].y),
            ];

            DrawLine3D(positionData[0], positionData[1], Colors.RED);
            DrawLine3D(positionData[1], positionData[2], Colors.RED);
            DrawLine3D(positionData[0], positionData[2], Colors.RED);

            return calculateY(positionData[0], positionData[1], positionData[2], point);

        } else {
            Vector3[3] positionData = [
                Vector3(pData[2].x, heightData[2], pData[2].y),
                Vector3(pData[3].x, heightData[3], pData[3].y),
                Vector3(pData[0].x, heightData[0], pData[0].y),
            ];

            DrawLine3D(positionData[0], positionData[1], Colors.RED);
            DrawLine3D(positionData[1], positionData[2], Colors.RED);
            DrawLine3D(positionData[0], positionData[2], Colors.RED);

            return calculateY(positionData[0], positionData[1], positionData[2], point);
        }
    }

    static void createGroundMesh() {
        import raylib;

        float[] vertices = new float[](0);
        float[] textureCoordinates = new float[](0);

        foreach (x; 0..mapWidth) {
            foreach (y; 0..mapHeight) {

                // Raylib is still absolutely ancient with ushort as the indices so I have to convert this mess into raw vertex tris.

                const float[4] heightData = [
                    getHeightAtNode(x, y), // 0 - Top Left.
                    getHeightAtNode(x, y + 1), // 1 - Bottom Left.
                    getHeightAtNode(x + 1, y + 1), // 2 - Bottom Right.
                    getHeightAtNode(x + 1, y), // 3 - Top Right.
                ];

                const Vector3[4] vData = [
                    Vector3(x, heightData[0], y), // 0
                    Vector3(x, heightData[1], y + 1), // 1
                    Vector3(x + 1, heightData[2], y + 1), // 2
                    Vector3(x + 1, heightData[3], y) // 3
                ];

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

        ModelHandler.newModelFromMesh("ground", vertices, textureCoordinates);

        //todo: set the ground texture from a pallete thing.
    }

    static void loadMapData(string location) {
        Image image;

        loadImage(location, &image);

        checkImage(location, &image);

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

    static void loadImage(string location, Image* image) {

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

    static void checkImage(string location, Image* image) {
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