using System.Numerics;

namespace FishGame.Utility;

static class CollisionMath {
    // Begin stackoverflow.
    // https://stackoverflow.com/a/2049593
    public static float Sign(Vector2 p1, Vector2 p2, Vector2 p3) {
        return (p1.X - p3.X) * (p2.Y - p3.Y) - (p2.X - p3.X) * (p1.Y - p3.Y);
    }

    // https://stackoverflow.com/a/2049593
    public static bool PointInTriangle(Vector2 point, Vector2 v1, Vector2 v2, Vector2 v3) {
        float d1 = Sign(point, v1, v2);
        float d2 = Sign(point, v2, v3);
        float d3 = Sign(point, v3, v1);

        bool has_neg = (d1 < 0) || (d2 < 0) || (d3 < 0);
        bool has_pos = (d1 > 0) || (d2 > 0) || (d3 > 0);

        return !(has_neg && has_pos);
    }

    // https://stackoverflow.com/a/5507832
    public static float CalculateY(Vector3 point1, Vector3 point2, Vector3 point3, Vector2 position) {
        float det = (point2.Z - point3.Z) * (point1.X - point3.X) + (
            point3.X - point2.X) * (point1.Z - point3.Z);

        float l1 = ((point2.Z - point3.Z) * (position.X - point3.X) + (
                point3.X - point2.X) * (position.Y - point3.Z)) / det;
        float l2 = ((point3.Z - point1.Z) * (position.X - point3.X) + (
                point1.X - point3.X) * (position.Y - point3.Z)) / det;
        float l3 = 1.0f - l1 - l2;

        return l1 * point1.Y + l2 * point2.Y + l3 * point3.Y;
    }
    // End stackoverflow.
}