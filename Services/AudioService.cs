using NAudio.Lame;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Threading;
namespace EditWave.Services

{
    public class AudioService : IDisposable
    {
        public string GetCurrentFilePath()
        {
            return _currentFilePath;
        }

        public bool IsTemporaryFile()
        {
            return _tempFilePath != null;
        }
        private string _tempFilePath;
        private WaveStream _audioStream;
        private WaveOutEvent _waveOut;
        //  private System.Timers.Timer _positionTimer;
        private DispatcherTimer _positionTimer;
        private bool _isPlaying;
        private string _currentFilePath;
        public bool HasFile => !string.IsNullOrEmpty(_currentFilePath); // чтобы юзер не тыкал на сохранить до вообще появления какого либо файла
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

        public bool LoadFile(string filePath, bool isTemporary = false)
        {
            try
            {
                Stop();

                // Если загружаем MP3 и это не временный файл, конвертируем во временный WAV
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
                        try
                        {
                            File.Delete(_tempFilePath);
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Не удалось удалить старый временный файл: {ex.Message}");
                        }
                    }
                    _tempFilePath = filePath;
                }
                else
                {
                    _tempFilePath = null; // если файл обычный можно забыть про временный
                }
                _audioStream?.Dispose();
                _waveOut?.Dispose();
                _currentFilePath = filePath;
                if (filePath.EndsWith(".mp3"))
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
        public void CleanTempFile()
        {
            if (!string.IsNullOrEmpty(_tempFilePath) && File.Exists(_tempFilePath))
            {
                try
                {
                    Stop();
                    _audioStream?.Dispose();
                    _audioStream = null; 
                    _waveOut?.Dispose();
                    _waveOut = null;

                    File.Delete(_tempFilePath);
                }
                catch( Exception ex ) 
                {
                    System.Diagnostics.Debug.WriteLine($"Не удалось удалить временный файл: {ex.Message}");
                }
                _tempFilePath = null;
            }
        }
        public void Dispose()
        {
            CleanTempFile();
            _positionTimer?.Stop();
    
            _audioStream?.Dispose();
            _waveOut?.Dispose();
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
            _isPlaying = true;

            //_positionTimer = new System.Timers.Timer(100);
            //_positionTimer.Elapsed += OnTimerTick;
            //_positionTimer.Start();
            //_isPlaying = true;
            //создаём UI-таймер вместо обычного
            _positionTimer = new DispatcherTimer();
            _positionTimer.Interval = TimeSpan.FromMilliseconds(100);
            _positionTimer.Tick += OnTimerTick;
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
        private void OnTimerTick(object sender, EventArgs args)
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
            LoadFile(tempFile, isTemporary: true);
   
            if (wasPlaying)
            {
                Play();
            }

        }

        public void Trim(double startSeconds, double endSeconds)
        {
            if (_audioStream == null) return;
            if (startSeconds >= endSeconds)
            {
                MessageBox.Show("Некорректное выделение");
                return;
            }

            bool wasPlaying = _isPlaying;
            Stop();

            string tempFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".wav");
            using (var reader = new AudioFileReader(_currentFilePath))
            using (var writer = new WaveFileWriter(tempFile, reader.WaveFormat))
            {
                reader.CurrentTime = TimeSpan.FromSeconds(startSeconds);
                double durationToCopy = endSeconds - startSeconds;
                long bytesToCopy = (long)(durationToCopy * reader.WaveFormat.AverageBytesPerSecond);
                int blockAlign = reader.WaveFormat.BlockAlign;
                bytesToCopy -= bytesToCopy % blockAlign;
                byte[] buffer = new byte[65536];
                long totalBytesCopied = 0;
                while (totalBytesCopied < bytesToCopy)
                {
                    int bytesToRead = (int)Math.Min(buffer.Length, bytesToCopy - totalBytesCopied);
                    bytesToRead -= bytesToRead % blockAlign;
                    int bytesRead = reader.Read(buffer, 0, bytesToRead);
                    if (bytesRead == 0) break;
                    writer.Write(buffer, 0, bytesRead);
                    totalBytesCopied += bytesRead;
                }
            }

            LoadFile(tempFile, isTemporary: true);
            if (wasPlaying) Play();
        }

        private string ConvertMp3ToWav(string mp3Path) // так как с мп3 нормально не работает и надо сразу конвертировать чтоб работала громкость и тп
        {
            string tempWav = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".wav");
            using (var reader = new Mp3FileReader(mp3Path))
            using (var writer = new WaveFileWriter(tempWav, reader.WaveFormat))
            {
                reader.CopyTo(writer);
            }
            return tempWav;
        }
        public void DeleteSelection(double startSeconds, double endSeconds)
        {
            if (_audioStream == null) return;
            if (startSeconds >= endSeconds)
            {
                MessageBox.Show("Некорректное выделение");
                return;
            }

            bool wasPlaying = _isPlaying;
            Stop();

            string tempFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".wav");
            using (var reader = new AudioFileReader(_currentFilePath))
            using (var writer = new WaveFileWriter(tempFile, reader.WaveFormat))
            {
                int blockAlign = reader.WaveFormat.BlockAlign;
                byte[] buffer = new byte[65536];

                reader.CurrentTime = TimeSpan.FromSeconds(0);
                long bytesToCopyStart = (long)(startSeconds * reader.WaveFormat.AverageBytesPerSecond);
                bytesToCopyStart -= bytesToCopyStart % blockAlign;
                long copied = 0;
                while (copied < bytesToCopyStart)
                {
                    int toRead = (int)Math.Min(buffer.Length, bytesToCopyStart - copied);
                    toRead -= toRead % blockAlign;
                    int read = reader.Read(buffer, 0, toRead);
                    if (read == 0) break;
                    writer.Write(buffer, 0, read);
                    copied += read;
                }

                reader.CurrentTime = TimeSpan.FromSeconds(endSeconds);
                long remaining = reader.Length - reader.Position;
                remaining -= remaining % blockAlign;
                copied = 0;
                while (copied < remaining)
                {
                    int toRead = (int)Math.Min(buffer.Length, remaining - copied);
                    toRead -= toRead % blockAlign;
                    int read = reader.Read(buffer, 0, toRead);
                    if (read == 0) break;
                    writer.Write(buffer, 0, read);
                    copied += read;
                }
            }

            LoadFile(tempFile, isTemporary: true);
            if (wasPlaying) Play();
        }
        public void Export(string filePath)
        {
            if (_audioStream == null) return;

            bool wasPlaying = _isPlaying;
            Stop();

            string extension = Path.GetExtension(filePath).ToLower();
            try
            {
                if (extension == ".mp3")
                {
                    string tempWav = Path.GetTempFileName() + ".wav";
                    File.Copy(_currentFilePath, tempWav, true);

                    string lamePath = "lame.exe";
                    string arguments = $"-b 192 \"{tempWav}\" \"{filePath}\"";

                    var process = new Process();
                    process.StartInfo.FileName = lamePath;
                    process.StartInfo.Arguments = arguments;
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.CreateNoWindow = true;
                    process.Start();
                    process.WaitForExit();

                    File.Delete(tempWav);
                }
                else if (extension == ".wav")
                {
                    File.Copy(_currentFilePath, filePath, true);
                }
                else
                {
                    MessageBox.Show("Неподдерживаемый формат. Используйте .wav или .mp3");
                }
                MessageBox.Show("Экспорт завершён: " + Path.GetFileName(filePath));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при экспорте: {ex.Message}");
            }
            finally
            {
                if (wasPlaying) Play();
            }
        }
        //todo волновая фкнкция

        public float[] GetWaveformSamples()
        {
            if (_audioStream == null) return new float[0];

            using (var reader = new AudioFileReader(_currentFilePath))
            {
                var samples = new List<float>();
                var buffer = new float[1024];
                int read;

                float max = 0;
                while ((read = reader.Read(buffer, 0, buffer.Length)) > 0)
                {
                    for (int i = 0; i < read; i++)
                    {
                        float abs = Math.Abs(buffer[i]);
                        if (abs > max) max = abs;
                        samples.Add(buffer[i]);
                    }
                }

                if (max == 0) max = 1;

                for (int i = 0; i < samples.Count; i++)
                {
                    samples[i] = samples[i] / max;
                }

                return samples.ToArray();
            }
        }
        public void ApplyGain(float gainFactor)
        {

            if (_audioStream == null)
            {
                System.Diagnostics.Debug.WriteLine("_audioStream == null");
                return;
            }
            if (_audioStream == null) return;
            bool wasPlaying = _isPlaying;
            Stop();

            string tempFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".wav");

            using (var reader = new AudioFileReader(_currentFilePath))
            using (var writer = new WaveFileWriter(tempFile, reader.WaveFormat))
            {
                var buffer = new float[4096];
                int read;
                while ((read = reader.Read(buffer, 0, buffer.Length)) > 0)
                {
                    for (int i = 0; i < read; i++)
                    {
                        buffer[i] *= gainFactor;
                   
                        if (buffer[i] > 1.0f) buffer[i] = 1.0f;
                        if (buffer[i] < -1.0f) buffer[i] = -1.0f;
                    }
                    writer.WriteSamples(buffer, 0, read);
                }
            }

            LoadFile(tempFile, isTemporary: true);

            if (wasPlaying) Play();
        }
    }
}
// TODO поправить методы