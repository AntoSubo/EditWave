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
        private AudioFileReader _audioFileReader;
        private WaveOutEvent _waveOut;
        private System.Timers.Timer _positionTimer;
        private bool _isPlaying;
        public bool IsPlaying => _isPlaying;
        public double Duration { get; private set; }
        public double CurrentPosition
        {
            get
            {
                if (_audioFileReader == null) return 0;
                return _audioFileReader.CurrentTime.TotalSeconds;
            }
        }
        public event Action PositionChanged;
        public bool LoadFile(string filePath)
        {
            try
            {
                Stop();
                _audioFileReader = new AudioFileReader(filePath);
                _waveOut = new WaveOutEvent();
                _waveOut.Init(_audioFileReader);
                Duration = _audioFileReader.TotalTime.TotalSeconds;
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
            if (_audioFileReader != null)
            {
                _audioFileReader.Position = 0;
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
            if (_audioFileReader != null)
            {
                _audioFileReader.Volume = Math.Clamp(volume, 0.0f, 1.0f);
            }
        }
        public void SetPosition(double position)
        {
            if (_audioFileReader == null) return;
            if (position < 0.0f) position = 0.0f;
            if (position > Duration) position = Duration;
            _audioFileReader.CurrentTime = TimeSpan.FromSeconds(position);
            PositionChanged?.Invoke();
        }
        private void OnTimerTick(object sender, ElapsedEventArgs args)
        {
            if (_isPlaying && _audioFileReader != null)
            {
                PositionChanged?.Invoke();
            }
        }
        public void ApplyReverse()
        {
            if (_audioFileReader == null) return;
            bool wasPlaying = _isPlaying;
            Stop();

            _audioFileReader.Position = 0;
            byte[] buffer = new byte[_audioFileReader.Length];
            int bytesRead = _audioFileReader.Read(buffer, 0, buffer.Length);
            Array.Reverse(buffer);

            string TempFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".wav");
            using (var writer = new WaveFileWriter(TempFile, _audioFileReader.WaveFormat))
            {
                writer.Write(buffer, 0, bytesRead);
            }
            LoadFile(TempFile);
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

            if (_audioFileReader == null) return;
            bool wasPlaying = _isPlaying;
            Stop();

            long startByte = (long)(startSeconds * _audioFileReader.WaveFormat.AverageBytesPerSecond);
            long endByte = (long)(endSeconds * _audioFileReader.WaveFormat.AverageBytesPerSecond);

            long lengthBytes = endByte - startByte;

            byte[] buffer = new byte[lengthBytes];
            _audioFileReader.Position = startByte;
            _audioFileReader.Read(buffer, 0, (int)lengthBytes);
            string TempFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".wav");
            using (var writer = new WaveFileWriter(TempFile, _audioFileReader.WaveFormat))
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

            if (_audioFileReader == null) return;

            bool wasPlaying = _isPlaying;
            Stop();

            long startByte = (long)(startSeconds * _audioFileReader.WaveFormat.AverageBytesPerSecond);
            long endByte = (long)(endSeconds * _audioFileReader.WaveFormat.AverageBytesPerSecond);

            byte[] firstPart = new byte[startByte];

            _audioFileReader.Position = 0;
            _audioFileReader.Read(firstPart, 0, (int)startByte);

            long remainingBytes = _audioFileReader.Length - endByte;
            byte[] secondPart = new byte[remainingBytes];

            _audioFileReader.Position = endByte;
            _audioFileReader.Read(secondPart, 0, (int)remainingBytes);

            byte[] result = new byte[firstPart.Length + secondPart.Length];
            Buffer.BlockCopy(firstPart, 0, result, 0, firstPart.Length);

            Buffer.BlockCopy(secondPart, 0, result, firstPart.Length, secondPart.Length);

            string TempFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".wav");
            using (var writer = new WaveFileWriter(TempFile, _audioFileReader.WaveFormat))
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
            if (_audioFileReader == null) return;
            bool wasPlaying = _isPlaying;
            Stop();

            File.Copy(_audioFileReader.FileName, filePath, true );
            if (wasPlaying)
            {
                Play();
            }
        }
    }
}
