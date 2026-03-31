using NAudio.Wave;
using System;
using System.Collections.Generic;
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
        //todo поле для таймера (яничепоканепонимаюкакегописать)
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
                //todo здесь таймер останавливать
            }
            if (_audioFileReader != null)
            {
                _audioFileReader.Position = 0; 
            }

        }
        public void Play()
        {
            if (_waveOut == null) return;

            if (_isPlaying) return;

            _waveOut.Play();
            _isPlaying = true;
            //todo тут таймер запускать
        }
        public void Pause()
        {
            if (_waveOut != null && _isPlaying)
            {
                _waveOut.Pause();
                _isPlaying = false;
                //todo здесь таймер останавливать
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
            //todo ибо я устав я болею я не спав я мухожук
        }
    }
}
