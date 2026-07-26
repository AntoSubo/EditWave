using System.IO;
using EditWave.Abstractions;
using NAudio.Wave;
using System.Windows.Threading;

namespace EditWave.Services
{
    public class AudioEngine : IAudioEngine
    {
        private readonly AudioContext _context;
        private WaveOutEvent? _waveOut;
        private DispatcherTimer? _positionTimer;
        private bool _isPlaying;
        private bool _disposed;
        private string? _tempFilePath;

        public event Action? PositionChanged;
        public event Action? PlaybackStopped;

        public AudioEngine(AudioContext context)
        {
            _context = context;
        }

        public bool HasFile => !string.IsNullOrEmpty(_context.CurrentFilePath);
        public bool IsPlaying => _isPlaying;
        public double Duration { get; private set; }
        public double CurrentPosition => _context.AudioStream?.CurrentTime.TotalSeconds ?? 0;

        public string FormatInfo
        {
            get
            {
                if (_context.AudioStream == null) return "";
                var fmt = _context.AudioStream.WaveFormat;
                string ext = Path.GetExtension(_context.CurrentFilePath).ToUpper();
                return $"{ext} | {fmt.SampleRate} Hz | {fmt.BitsPerSample} bit | {fmt.Channels}ch";
            }
        }

        public bool LoadFile(string filePath, bool isTemporary = false)
        {
            try
            {
                Stop();

                if (!isTemporary && filePath.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase))
                {
                    string wavPath = ConvertMp3ToWav(filePath);
                    filePath = wavPath;
                    isTemporary = true;
                }

                if (isTemporary)
                {
                    if (!string.IsNullOrEmpty(_tempFilePath) && File.Exists(_tempFilePath))
                    {
                        try { File.Delete(_tempFilePath); } catch { }
                    }
                    _tempFilePath = filePath;
                }
                else
                {
                    _tempFilePath = null;
                }

                _context.AudioStream?.Dispose();
                _waveOut?.Dispose();

                _context.CurrentFilePath = filePath;

                if (filePath.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase))
                    _context.AudioStream = new Mp3FileReader(filePath);
                else
                    _context.AudioStream = new AudioFileReader(filePath);

                _waveOut = new WaveOutEvent();
                _waveOut.Init(_context.AudioStream);
                _waveOut.PlaybackStopped += OnPlaybackStopped;
                Duration = _context.AudioStream.TotalTime.TotalSeconds;
                _isPlaying = false;

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки файла: {ex.Message}");
                return false;
            }
        }

        public string GetCurrentFilePath() => _context.CurrentFilePath;
        public bool IsTemporaryFile() => _tempFilePath != null;

        public void Play()
        {
            if (_waveOut == null || _isPlaying) return;
            _waveOut.Play();
            _isPlaying = true;

            if (_positionTimer == null)
            {
                _positionTimer = new DispatcherTimer();
                _positionTimer.Interval = TimeSpan.FromMilliseconds(100);
                _positionTimer.Tick += OnTimerTick;
            }
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

        public void Stop()
        {
            if (_waveOut != null && _isPlaying)
            {
                _waveOut.Stop();
                _isPlaying = false;
                _positionTimer?.Stop();
            }
            if (_context.AudioStream != null)
            {
                _context.AudioStream.Position = 0;
                PositionChanged?.Invoke();
            }
        }

        public void SetPosition(double position)
        {
            if (_context.AudioStream == null) return;
            if (position < 0.0) position = 0.0;
            if (position > Duration) position = Duration;
            _context.AudioStream.CurrentTime = TimeSpan.FromSeconds(position);
            PositionChanged?.Invoke();
        }

        public void SetVolume(float volume)
        {
            if (_context.AudioStream is AudioFileReader reader)
                reader.Volume = Math.Clamp(volume, 0f, 1f);
        }

        public void CleanTempFile()
        {
            if (!string.IsNullOrEmpty(_tempFilePath) && File.Exists(_tempFilePath))
            {
                try
                {
                    Stop();
                    _context.AudioStream?.Dispose();
                    _context.AudioStream = null;
                    _waveOut?.Dispose();
                    _waveOut = null;
                    File.Delete(_tempFilePath);
                }
                catch { }
                _tempFilePath = null;
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            CleanTempFile();
            _positionTimer?.Stop();
            _context.AudioStream?.Dispose();
            _context.AudioStream = null;
            _waveOut?.Dispose();
            _waveOut = null;
        }

        private string ConvertMp3ToWav(string mp3Path)
        {
            string tempWav = _context.CreateTempFilePath();
            using var reader = new Mp3FileReader(mp3Path);
            using var writer = new WaveFileWriter(tempWav, reader.WaveFormat);
            reader.CopyTo(writer);
            return tempWav;
        }

        private void OnTimerTick(object? sender, EventArgs args)
        {
            if (_isPlaying && _context.AudioStream != null)
                PositionChanged?.Invoke();
        }

        private void OnPlaybackStopped(object? sender, StoppedEventArgs e)
        {
            _isPlaying = false;
            _positionTimer?.Stop();
            if (_context.AudioStream != null)
                _context.AudioStream.CurrentTime = TimeSpan.Zero;
            System.Windows.Application.Current?.Dispatcher?.Invoke(() => PlaybackStopped?.Invoke());
        }
    }
}
