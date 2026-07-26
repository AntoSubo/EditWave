namespace EditWave.Abstractions
{
    public interface IAudioEngine : IDisposable
    {
        bool HasFile { get; }
        bool IsPlaying { get; }
        double Duration { get; }
        double CurrentPosition { get; }
        string FormatInfo { get; }
        event Action? PositionChanged;
        event Action? PlaybackStopped;

        bool LoadFile(string filePath, bool isTemporary = false);
        string GetCurrentFilePath();
        bool IsTemporaryFile();
        void Play();
        void Pause();
        void Stop();
        void SetPosition(double position);
        void SetVolume(float volume);
    }
}
