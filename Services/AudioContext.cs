using System.IO;
using NAudio.Wave;

namespace EditWave.Services
{
    public class AudioContext
    {
        private WaveStream? _audioStream;
        private string _currentFilePath = string.Empty;

        public WaveStream? AudioStream
        {
            get => _audioStream;
            set => _audioStream = value;
        }

        public string CurrentFilePath
        {
            get => _currentFilePath;
            set => _currentFilePath = value;
        }

        public string CreateTempFilePath() =>
            Path.Combine(Path.GetTempPath(), "EditWave_" + Guid.NewGuid() + ".wav");

        public static void TryDelete(string path)
        {
            try { if (File.Exists(path)) File.Delete(path); } catch { }
        }
    }
}
