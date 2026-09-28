using System.Numerics;

namespace FishGame.Utility;

static class MathThings {
    public static void ToAxisAngle(this Quaternion q, out Vector3 axis, out float angle) {
        q = Quaternion.Normalize(q);
        angle = 2.0f * MathF.Acos(Math.Clamp(q.W, -1.0f, 1.0f));
        float s = MathF.Sqrt(1.0f - q.W * q.W);
        if (s < 0.0001f) {
            axis = new Vector3(1.0f, 0.0f, 0.0f);
        } else {
            axis = new Vector3(q.X / s, q.Y / s, q.Z / s);
        }
    }


}