using System.Numerics;
using Raylib_cs;

namespace FishGame.Graphics;

public static class CameraManager {

    static Camera3D camera = new();

    public static void Initialize() {
        camera.Position = new Vector3(0, 4, 4);
        camera.Up = new Vector3(0, 1, 0);
        camera.Target = new Vector3(0, 0, 0);
        camera.FovY = 45.0f;
        camera.Projection = CameraProjection.Perspective;
    }

    public static void SetPosition(Vector3 newPosition) {
        camera.Position = newPosition;
    }

    public static void SetTarget(Vector3 newTarget) {
        camera.Target = newTarget;
    }

    public static float GetFOV() {
        return camera.FovY;
    }

    public static void SetFOV(float newFOV) {
        camera.FovY = newFOV;
    }

    public static Camera3D Get() {
        return camera;
    }

    // static unsafe void DoFreeCam() {
    //     Raylib.UpdateCamera(&camera, CameraMode.Free);
    // }

}