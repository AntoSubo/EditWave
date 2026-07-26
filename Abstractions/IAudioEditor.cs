namespace EditWave.Abstractions
{
    public interface IAudioEditor
    {
        string? Trim(double startSeconds, double endSeconds);
        string? DeleteSelection(double startSeconds, double endSeconds);
        string? ApplyGain(float gainFactor);
        string? ApplyReverse();
        string? ApplyGainToSelection(float gainFactor, double startSeconds, double endSeconds);
        string? ApplyReverseToSelection(double startSeconds, double endSeconds);
        string? ApplyFadeIn(double startSeconds, double endSeconds);
        string? ApplyFadeOut(double startSeconds, double endSeconds);
        string? ApplyNormalize();
        string? ApplyNormalizeToSelection(double startSeconds, double endSeconds);
        string? ApplySpeed(float speedFactor);
        string? ApplyPitch(float pitchFactor);
        string? InsertSilence(double positionSeconds, double silenceDurationSeconds);
        void CopySelection(double startSeconds, double endSeconds);
        string? PasteAt(double positionSeconds);
        bool HasClipboard { get; }
        double ClipboardDuration { get; }
        void ReadMetadata(out string? title, out string? artist, out string? album, out string? year);
        void WriteMetadata(string? title, string? artist, string? album, string? year);
        string? TrimSilence(float threshold);
        string? ApplyEQ(float bassGain, float trebleGain);
        string? ToMono();
        string? ToStereo();
    }
}
