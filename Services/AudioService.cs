using NAudio.Wave;
using System;
using System.IO;
using System.Timers;
using System.Linq;
using System.Text;
using NAudio.Wave.SampleProviders;
using System.Threading;
using System.Threading.Tasks;

using System.Windows;
namespace EditWave.Services

{
    public class AudioService
    {
        private WaveStream _audioStream;
        private WaveOutEvent _waveOut;
        private System.Timers.Timer _positionTimer;
        private bool _isPlaying;
        private string _currentFilePath;
        public bool IsPlaying => _isPlaying;
        public double Duration { get; private set; }
        public double CurrentPosition
        {
            get
            {
                if (_audioStream == null) return 0;
                return _audioStream.CurrentTime.TotalSeconds;
            }
        }
        public event Action PositionChanged;
        public bool LoadFile(string filePath)
        {
            try
            {
                Stop();

                _audioStream?.Dispose();
                _waveOut?.Dispose();
                _currentFilePath = filePath;
                if (filePath.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase))
                {
                    _audioStream = new Mp3FileReader(filePath);
                }
                else
                {
                    _audioStream = new AudioFileReader(filePath);
                }

                _waveOut = new WaveOutEvent();
                _waveOut.Init(_audioStream);
                Duration = _audioStream.TotalTime.TotalSeconds;
                _isPlaying = false;
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return false;
            }
        }
        public void Stop()
        {
            if (_waveOut != null && _isPlaying)
            {
                _waveOut.Stop();
                _isPlaying = false;
                _positionTimer?.Stop();
            }
            if (_audioStream != null)
            {
                _audioStream.Position = 0;
                PositionChanged?.Invoke();
            }

        }
        public void Play()
        {
            if (_waveOut == null) return;

            if (_isPlaying) return;

            _waveOut.Play();
            _positionTimer?.Stop();
            _isPlaying = true;
            _positionTimer = new System.Timers.Timer(100);
            _positionTimer.Elapsed += OnTimerTick;
            _positionTimer.Start();
        }
        public void Pause()
        {
            if (_waveOut != null && _isPlaying)
            {
                _waveOut.Pause();
                _isPlaying = false;
                _positionTimer?.Stop();
            }
        }
        public void SetVolume(float volume)
        {
            if (_audioStream is AudioFileReader reader)
            {
                reader.Volume = Math.Clamp(volume, 0f, 1f);
            }

        }
        public void SetPosition(double position)
        {
            if (_audioStream == null) return;
            if (position < 0.0f) position = 0.0f;
            if (position > Duration) position = Duration;
            _audioStream.CurrentTime = TimeSpan.FromSeconds(position);
            PositionChanged?.Invoke();
        }
        private void OnTimerTick(object sender, ElapsedEventArgs args)
        {
            if (_isPlaying && _audioStream != null)
            {
                PositionChanged?.Invoke();
            }
        }
        public void ApplyReverse()
        {
            if (_audioStream == null) return;
            bool wasPlaying = _isPlaying;
            Stop();

            string tempFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".wav");

            using (var reader = new AudioFileReader(_currentFilePath))
            {
                var samples = new List<float>();
                var buffer = new float[1024];
                int read;
                while ((read = reader.Read(buffer, 0, buffer.Length)) > 0)
                {
                    for (int i = 0; i < read; i++)
                        samples.Add(buffer[i]);
                }
                samples.Reverse();
                using (var writer = new WaveFileWriter(tempFile, reader.WaveFormat))
                {
                    foreach (var sample in samples)
                    {
                        writer.WriteSample(sample);
                    }
                }
            }
            LoadFile(tempFile);
           // File.Delete(tempFile);
            if (wasPlaying)
            {
                Play();
            }

        }
        public void Trim(double startSeconds, double endSeconds)
        {
            if (startSeconds < 0) startSeconds = 0;
            if (endSeconds > Duration) endSeconds = Duration;
            if (startSeconds >= endSeconds) return;

            if (_audioStream == null) return;
            bool wasPlaying = _isPlaying;
            Stop();

            long startByte = (long)(startSeconds * _audioStream.WaveFormat.AverageBytesPerSecond);
            long endByte = (long)(endSeconds * _audioStream.WaveFormat.AverageBytesPerSecond);

            long lengthBytes = endByte - startByte;

            byte[] buffer = new byte[lengthBytes];
            _audioStream.Position = startByte;
            _audioStream.Read(buffer, 0, (int)lengthBytes);
            string TempFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".wav");
            using (var writer = new WaveFileWriter(TempFile, _audioStream.WaveFormat))
            {
                writer.Write(buffer, 0, buffer.Length);
            }

            LoadFile(TempFile);
            if (wasPlaying)
            {
                Play();
            }
        }

        public void DeleteSelection(double startSeconds, double endSeconds)
        {
            if (startSeconds < 0) startSeconds = 0;
            if (endSeconds > Duration) endSeconds = Duration;
            if (startSeconds >= endSeconds) return;

            if (_audioStream == null) return;

            bool wasPlaying = _isPlaying;
            Stop();

            long startByte = (long)(startSeconds * _audioStream.WaveFormat.AverageBytesPerSecond);
            long endByte = (long)(endSeconds * _audioStream.WaveFormat.AverageBytesPerSecond);

            byte[] firstPart = new byte[startByte];

            _audioStream.Position = 0;
            _audioStream.Read(firstPart, 0, (int)startByte);

            long remainingBytes = _audioStream.Length - endByte;
            byte[] secondPart = new byte[remainingBytes];

            _audioStream.Position = endByte;
            _audioStream.Read(secondPart, 0, (int)remainingBytes);

            byte[] result = new byte[firstPart.Length + secondPart.Length];
            Buffer.BlockCopy(firstPart, 0, result, 0, firstPart.Length);

            Buffer.BlockCopy(secondPart, 0, result, firstPart.Length, secondPart.Length);

            string TempFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".wav");
            using (var writer = new WaveFileWriter(TempFile, _audioStream.WaveFormat))
            {
                writer.Write(result, 0, result.Length);
            }

            LoadFile(TempFile);
            if (wasPlaying)
            {
                Play();
            }
        }
        public void Export(string filePath)
        {
            if (_audioStream == null) return;
            bool wasPlaying = _isPlaying;
            Stop();

            File.Copy(_currentFilePath, filePath, true);
            if (wasPlaying)
            {
                Play();
            }
        }

        //todo волновая фкнкция
    }

}
