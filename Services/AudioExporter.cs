using EditWave.Abstractions;
using NAudio.MediaFoundation;
using NAudio.Wave;
using System.IO;

namespace EditWave.Services
{
    public class AudioExporter : IFileExporter
    {
        private readonly AudioContext _context;
        private readonly IAudioEngine _engine;

        public AudioExporter(AudioContext context, IAudioEngine engine)
        {
            _context = context;
            _engine = engine;
        }

        public void Export(string filePath) => Export(filePath, 192);
        public void Export(string filePath, int mp3Bitrate) => ExportInternal(filePath, null, null, mp3Bitrate);

        public void ExportSelected(string filePath, double startSeconds, double endSeconds) =>
            ExportSelected(filePath, startSeconds, endSeconds, 192);

        public void ExportSelected(string filePath, double startSeconds, double endSeconds, int mp3Bitrate) =>
            ExportInternal(filePath, startSeconds, endSeconds, mp3Bitrate);

        private void ExportInternal(string filePath, double? startSeconds, double? endSeconds, int mp3Bitrate)
        {
            if (_context.AudioStream == null) throw new InvalidOperationException("Нет аудио для экспорта");

            bool wasPlaying = _engine.IsPlaying;
            _engine.Stop();

            string extension = Path.GetExtension(filePath).ToLower();
            bool success = false;
            try
            {
                if (extension == ".wav")
                {
                    if (startSeconds.HasValue && endSeconds.HasValue)
                        ExportWavSelection(filePath, startSeconds.Value, endSeconds.Value);
                    else if (_context.CurrentFilePath != filePath)
                        File.Copy(_context.CurrentFilePath, filePath, true);
                    success = true;
                }
                else if (extension == ".mp3")
                {
                    ExportMp3(filePath, startSeconds, endSeconds, mp3Bitrate);
                    success = true;
                }
                else
                {
                    throw new NotSupportedException("Поддерживаются WAV и MP3 файлы");
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Ошибка при экспорте: {ex.Message}", ex);
            }
            finally
            {
                if (wasPlaying && success) _engine.Play();
            }
        }

        private void ExportWavSelection(string filePath, double startSeconds, double endSeconds)
        {
            using var reader = new AudioFileReader(_context.CurrentFilePath);
            using var writer = new WaveFileWriter(filePath, reader.WaveFormat);
            reader.CurrentTime = TimeSpan.FromSeconds(startSeconds);
            long bytesToCopy = (long)((endSeconds - startSeconds) * reader.WaveFormat.AverageBytesPerSecond);
            int blockAlign = reader.WaveFormat.BlockAlign;
            bytesToCopy -= bytesToCopy % blockAlign;
            byte[] buffer = new byte[65536];
            long copied = 0;
            while (copied < bytesToCopy)
            {
                int toRead = (int)Math.Min(buffer.Length, bytesToCopy - copied);
                toRead -= toRead % blockAlign;
                if (toRead == 0) break;
                int read = reader.Read(buffer, 0, toRead);
                if (read == 0) break;
                writer.Write(buffer, 0, read);
                copied += read;
            }
        }

        private void ExportMp3(string filePath, double? startSeconds, double? endSeconds, int bitrate)
        {
            using var reader = new AudioFileReader(_context.CurrentFilePath);
            if (startSeconds.HasValue && endSeconds.HasValue)
            {
                reader.CurrentTime = TimeSpan.FromSeconds(startSeconds.Value);
                long bytesToCopy = (long)((endSeconds.Value - startSeconds.Value) * reader.WaveFormat.AverageBytesPerSecond);
                int blockAlign = reader.WaveFormat.BlockAlign;
                bytesToCopy -= bytesToCopy % blockAlign;

                using var ms = new MemoryStream();
                byte[] buffer = new byte[65536];
                long copied = 0;
                while (copied < bytesToCopy)
                {
                    int toRead = (int)Math.Min(buffer.Length, bytesToCopy - copied);
                    toRead -= toRead % blockAlign;
                    if (toRead == 0) break;
                    int read = reader.Read(buffer, 0, toRead);
                    if (read == 0) break;
                    ms.Write(buffer, 0, read);
                    copied += read;
                }
                ms.Position = 0;
                using var waveStream = new RawSourceWaveStream(ms, reader.WaveFormat);
                MediaFoundationEncoder.EncodeToMp3(waveStream, filePath, bitrate);
            }
            else
            {
                MediaFoundationEncoder.EncodeToMp3(reader, filePath, bitrate);
            }
        }
    }
}
