namespace FishGame.Utility;

// It's given a human name for 3 reasons.
// 1.) It's dumb.
// 2.) It's obviously dumb and it's obvious it's from this project.
// 3.) It doesn't conflict with Random.
static class Randy {
    public static double NextDouble(double min, double max) {
        return min + (Random.Shared.NextDouble() * (max - min));
    }
    public static float NextFloat(float min, float max) {
        return min + (Random.Shared.NextSingle() * (max - min));
    }

    public static int NextInt(int min, int max) {
        return Random.Shared.Next(min, max);
    }
}