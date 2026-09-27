using System.Numerics;
using Raylib_cs;

namespace FishGame.Graphics;

public static class CameraHandler {

    static Camera3D camera = new();

    public static void Initialize() {
        camera.Position = new Vector3(0, 4, 4);
        camera.Up = new Vector3(0, 1, 0);
        camera.Target = new Vector3(0, 0, 0);
        camera.FovY = 45.0f;
        camera.Projection = CameraProjection.Perspective;
    }


}