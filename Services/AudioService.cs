using NAudio.Wave;
using System;
using System.Timers;
using System.Linq;
using System.Text;
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
                System.Windows.MessageBox.Show(ex.Message);
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
    }
}
