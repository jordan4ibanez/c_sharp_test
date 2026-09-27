
using Raylib_cs;

namespace FishGame.Audio;

class SoundPool : IDisposable {
    private Sound _master;
    private readonly Sound[] _voices = new Sound[4];
    private int _nextVoice = 0;
    private readonly Random _random = new();

    public SoundPool(string filePath) {
        _master = Raylib.LoadSound(filePath);

        for (int i = 0; i < _voices.Length; i++) {
            _voices[i] = Raylib.LoadSoundAlias(_master);
        }
    }

    public void Play(float volume, float pitch) {
        Sound voice = _voices[_nextVoice];
        Raylib.SetSoundVolume(voice, volume);
        Raylib.SetSoundPitch(voice, pitch);
        Raylib.PlaySound(voice);

        _nextVoice = (_nextVoice + 1) % _voices.Length;
    }

    // Play with a subtle random pitch shift.
    public void PlayPitched(float volume, float pitchVariance) {
        float min = 1.0f - pitchVariance;
        float max = 1.0f + pitchVariance;
        float randomPitch = min + (float)_random.NextDouble() * (max - min);

        Play(volume, randomPitch);
    }

    public void Dispose() {
        foreach (Sound voice in _voices) {
            Raylib.UnloadSoundAlias(voice);
        }

        Raylib.UnloadSound(_master);
    }
}

public static class SoundManager {


}