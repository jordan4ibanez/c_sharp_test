using System.Diagnostics;

namespace FishGame.Utility;

public static class Delta {
    private static long _lastTimestamp = Stopwatch.GetTimestamp();
    private static double _delta = 0;
    private static double _maxDelta = 1.0 / 5.0;

    public static void CalculateDelta() {
        long currentTimestamp = Stopwatch.GetTimestamp();
        TimeSpan elapsed = Stopwatch.GetElapsedTime(_lastTimestamp, currentTimestamp);

        _delta = elapsed.TotalSeconds;

        // Delta limiter.
        if (_delta > _maxDelta) {
            _delta = _maxDelta;
        }

        _lastTimestamp = currentTimestamp;
    }

    public static double GetDelta() {
        return _delta;
    }

    public static void SetMaxDelta(double newDeltaMax) {
        _maxDelta = newDeltaMax;
    }

    public static void SetMaxDeltaFps(double fps) {
        if (fps > 0) {
            _maxDelta = 1.0 / fps;
        }
    }
}