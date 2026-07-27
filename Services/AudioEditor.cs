using EditWave.Abstractions;
using NAudio.Wave;
using System.IO;

namespace EditWave.Services
{
    public class AudioEditor : IAudioEditor, IWaveformProvider
    {
        private readonly AudioContext _context;
        private const int BufferSize = 65536;
        private const int FloatBufferSize = 4096;
        private float[]? _clipboard;
        private WaveFormat? _clipboardFormat;

        public AudioEditor(AudioContext context)
        {
            _context = context;
        }

        public string? Trim(double startSeconds, double endSeconds)
        {
            return CopyRegion(startSeconds, endSeconds,
                (reader, writer) =>
                {
                    reader.CurrentTime = TimeSpan.FromSeconds(startSeconds);
                    long bytesToCopy = (long)((endSeconds - startSeconds) * reader.WaveFormat.AverageBytesPerSecond);
                    CopyBytes(reader, writer, bytesToCopy);
                });
        }

        public string? DeleteSelection(double startSeconds, double endSeconds)
        {
            return CopyRegion(startSeconds, endSeconds,
                (reader, writer) =>
                {
                    long bytesToCopyStart = (long)(startSeconds * reader.WaveFormat.AverageBytesPerSecond);
                    CopyBytes(reader, writer, bytesToCopyStart);
                    CopyFromPosition(reader, writer, endSeconds);
                });
        }

        public string? ApplyGain(float gainFactor)
        {
            if (_context.AudioStream == null) return null;
            return ApplyEffect(new GainEffect(gainFactor));
        }

        public string? ApplyReverse()
        {
            if (_context.AudioStream == null) return null;
            return ApplyEffect(new ReverseEffect());
        }

        public string? ApplyGainToSelection(float gainFactor, double startSeconds, double endSeconds)
        {
            return ApplyEffectToSelection(new GainEffect(gainFactor), startSeconds, endSeconds);
        }

        public string? ApplyReverseToSelection(double startSeconds, double endSeconds)
        {
            return ApplyEffectToSelection(new ReverseEffect(), startSeconds, endSeconds);
        }

        public string? ApplyFadeIn(double startSeconds, double endSeconds)
        {
            if (_context.AudioStream == null) return null;
            return ApplyFade(true, startSeconds, endSeconds);
        }

        public string? ApplyFadeOut(double startSeconds, double endSeconds)
        {
            if (_context.AudioStream == null) return null;
            return ApplyFade(false, startSeconds, endSeconds);
        }

        public string? ApplyNormalize()
        {
            if (_context.AudioStream == null) return null;
            return ApplyNormalizeToSelection(0, _context.AudioStream.TotalTime.TotalSeconds);
        }

        public string? ApplyNormalizeToSelection(double startSeconds, double endSeconds)
        {
            if (_context.AudioStream == null) return null;
            if (startSeconds >= endSeconds)
                throw new ArgumentException("Некорректное выделение");

            string tempFile = _context.CreateTempFilePath();
            string tempSelection = _context.CreateTempFilePath();
            string tempProcessed = _context.CreateTempFilePath();

            try
            {
                using (var reader = new AudioFileReader(_context.CurrentFilePath))
                using (var writer = new WaveFileWriter(tempSelection, reader.WaveFormat))
                {
                    reader.CurrentTime = TimeSpan.FromSeconds(startSeconds);
                    long bytesToCopy = (long)((endSeconds - startSeconds) * reader.WaveFormat.AverageBytesPerSecond);
                    CopyBytes(reader, writer, bytesToCopy);
                }

                var normalize = new NormalizeEffect();
                using (var input = new AudioFileReader(tempSelection))
                {
                    var floatFormat = WaveFormat.CreateIeeeFloatWaveFormat(input.WaveFormat.SampleRate, input.WaveFormat.Channels);
                    using (var output = new WaveFileWriter(tempProcessed, floatFormat))
                    {
                        float[] buffer = new float[FloatBufferSize];
                        int read;
                        while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
                            normalize.Process(buffer.Take(read).ToArray());
                        input.Position = 0;
                        float peak = normalize.GetPeak();
                        if (peak > 0)
                        {
                            float scale = 1.0f / peak;
                            while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
                            {
                                for (int i = 0; i < read; i++)
                                    buffer[i] *= scale;
                                output.WriteSamples(buffer, 0, read);
                            }
                        }
                    }
                }

                using (var reader = new AudioFileReader(_context.CurrentFilePath))
                using (var writer = new WaveFileWriter(tempFile, reader.WaveFormat))
                {
                    long bytesBefore = (long)(startSeconds * reader.WaveFormat.AverageBytesPerSecond);
                    CopyBytes(reader, writer, bytesBefore);

                    using var processedReader = new AudioFileReader(tempProcessed);
                    processedReader.Position = 0;
                    byte[] buffer = new byte[BufferSize];
                    int read;
                    while ((read = processedReader.Read(buffer, 0, buffer.Length)) > 0)
                        writer.Write(buffer, 0, read);

                    CopyFromPosition(reader, writer, endSeconds);
                }

                return tempFile;
            }
            catch (Exception ex)
            {
                AudioContext.TryDelete(tempFile);
                throw new InvalidOperationException($"Ошибка: {ex.Message}", ex);
            }
            finally
            {
                AudioContext.TryDelete(tempSelection);
                AudioContext.TryDelete(tempProcessed);
            }
        }

        private string? ApplyFade(bool fadeIn, double startSeconds, double endSeconds)
        {
            if (_context.AudioStream == null) return null;

            string tempFile = _context.CreateTempFilePath();
            try
            {
                using var reader = new AudioFileReader(_context.CurrentFilePath);
                var floatFormat = WaveFormat.CreateIeeeFloatWaveFormat(reader.WaveFormat.SampleRate, reader.WaveFormat.Channels);
                using var writer = new WaveFileWriter(tempFile, floatFormat);
                double duration = _context.AudioStream.TotalTime.TotalSeconds;
                float[] buffer = new float[FloatBufferSize];
                int read;
                while ((read = reader.Read(buffer, 0, buffer.Length)) > 0)
                {
                    for (int i = 0; i < read; i++)
                    {
                        double time = reader.CurrentTime.TotalSeconds - (double)i / reader.WaveFormat.SampleRate;
                        if (time < startSeconds || time > endSeconds) continue;
                        double progress = (time - startSeconds) / (endSeconds - startSeconds);
                        float factor = fadeIn ? (float)progress : (float)(1.0 - progress);
                        buffer[i] *= factor;
                    }
                    writer.WriteSamples(buffer, 0, read);
                }
                return tempFile;
            }
            catch
            {
                AudioContext.TryDelete(tempFile);
                throw;
            }
        }

        public float[] GetWaveformSamples()
        {
            if (_context.AudioStream == null) return Array.Empty<float>();

            using var reader = new AudioFileReader(_context.CurrentFilePath);
            var samples = new List<float>();
            var buffer = new float[1024];
            int read;
            while ((read = reader.Read(buffer, 0, buffer.Length)) > 0)
            {
                for (int i = 0; i < read; i++)
                    samples.Add(buffer[i]);
            }
            return samples.ToArray();
        }

        private string? CopyRegion(double startSeconds, double endSeconds, Action<AudioFileReader, WaveFileWriter> copyAction)
        {
            if (_context.AudioStream == null) return null;
            if (startSeconds >= endSeconds)
                throw new ArgumentException("Некорректное выделение");

            string tempFile = _context.CreateTempFilePath();
            try
            {
                using var reader = new AudioFileReader(_context.CurrentFilePath);
                using var writer = new WaveFileWriter(tempFile, reader.WaveFormat);
                copyAction(reader, writer);
                return tempFile;
            }
            catch
            {
                AudioContext.TryDelete(tempFile);
                throw;
            }
        }

        private string? ApplyEffect(IAudioEffect effect)
        {
            if (_context.AudioStream == null) return null;

            string tempFile = _context.CreateTempFilePath();
            try
            {
                using var reader = new AudioFileReader(_context.CurrentFilePath);
                var floatFormat = WaveFormat.CreateIeeeFloatWaveFormat(reader.WaveFormat.SampleRate, reader.WaveFormat.Channels);
                using var writer = new WaveFileWriter(tempFile, floatFormat);
                float[] buffer = new float[FloatBufferSize];
                int read;
                while ((read = reader.Read(buffer, 0, buffer.Length)) > 0)
                {
                    float[] processed = effect.Process(buffer.Take(read).ToArray());
                    writer.WriteSamples(processed, 0, processed.Length);
                }
                return tempFile;
            }
            catch
            {
                AudioContext.TryDelete(tempFile);
                throw;
            }
        }

        private string? ApplyEffectToSelection(IAudioEffect effect, double startSeconds, double endSeconds)
        {
            if (_context.AudioStream == null) return null;
            if (startSeconds >= endSeconds)
                throw new ArgumentException("Некорректное выделение");

            string tempFile = _context.CreateTempFilePath();
            string tempSelection = _context.CreateTempFilePath();
            string tempProcessed = _context.CreateTempFilePath();

            try
            {
                using (var reader = new AudioFileReader(_context.CurrentFilePath))
                using (var writer = new WaveFileWriter(tempSelection, reader.WaveFormat))
                {
                    reader.CurrentTime = TimeSpan.FromSeconds(startSeconds);
                    long bytesToCopy = (long)((endSeconds - startSeconds) * reader.WaveFormat.AverageBytesPerSecond);
                    CopyBytes(reader, writer, bytesToCopy);
                }

                using (var input = new AudioFileReader(tempSelection))
                {
                    var floatFormat = WaveFormat.CreateIeeeFloatWaveFormat(input.WaveFormat.SampleRate, input.WaveFormat.Channels);
                    using (var output = new WaveFileWriter(tempProcessed, floatFormat))
                    {
                        float[] buffer = new float[FloatBufferSize];
                        int read;
                        while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            float[] processed = effect.Process(buffer.Take(read).ToArray());
                            output.WriteSamples(processed, 0, processed.Length);
                        }
                    }
                }

                using (var reader = new AudioFileReader(_context.CurrentFilePath))
                using (var writer = new WaveFileWriter(tempFile, reader.WaveFormat))
                {
                    long bytesBefore = (long)(startSeconds * reader.WaveFormat.AverageBytesPerSecond);
                    CopyBytes(reader, writer, bytesBefore);

                    using var processedReader = new AudioFileReader(tempProcessed);
                    processedReader.Position = 0;
                    byte[] buffer = new byte[BufferSize];
                    int read;
                    while ((read = processedReader.Read(buffer, 0, buffer.Length)) > 0)
                        writer.Write(buffer, 0, read);

                    CopyFromPosition(reader, writer, endSeconds);
                }

                return tempFile;
            }
            catch (Exception ex)
            {
                AudioContext.TryDelete(tempFile);
                throw new InvalidOperationException($"Ошибка: {ex.Message}", ex);
            }
            finally
            {
                AudioContext.TryDelete(tempSelection);
                AudioContext.TryDelete(tempProcessed);
            }
        }

        private static void CopyBytes(AudioFileReader reader, WaveFileWriter writer, long bytesToCopy)
        {
            int blockAlign = reader.WaveFormat.BlockAlign;
            bytesToCopy -= bytesToCopy % blockAlign;
            byte[] buffer = new byte[BufferSize];
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

        private static void CopyFromPosition(AudioFileReader reader, WaveFileWriter writer, double startSeconds)
        {
            reader.CurrentTime = TimeSpan.FromSeconds(startSeconds);
            long remaining = reader.Length - reader.Position;
            CopyBytes(reader, writer, remaining);
        }

        public string? ApplySpeed(float speedFactor)
        {
            if (_context.AudioStream == null) return null;
            if (speedFactor <= 0) throw new ArgumentException("Коэффициент скорости должен быть > 0");

            string tempFile = _context.CreateTempFilePath();
            try
            {
                using var reader = new AudioFileReader(_context.CurrentFilePath);
                var format = WaveFormat.CreateIeeeFloatWaveFormat(
                    (int)(reader.WaveFormat.SampleRate * speedFactor),
                    reader.WaveFormat.Channels);

                using var writer = new WaveFileWriter(tempFile, format);
                float[] buffer = new float[FloatBufferSize];
                int read;
                while ((read = reader.Read(buffer, 0, buffer.Length)) > 0)
                    writer.WriteSamples(buffer, 0, read);

                return tempFile;
            }
            catch
            {
                AudioContext.TryDelete(tempFile);
                throw;
            }
        }

        public string? ApplyPitch(float pitchFactor)
        {
            if (_context.AudioStream == null) return null;
            if (pitchFactor <= 0) throw new ArgumentException("Коэффициент тона должен быть > 0");
            if (pitchFactor == 1.0f) return null;

            string tempFile = _context.CreateTempFilePath();
            try
            {
                using var reader = new AudioFileReader(_context.CurrentFilePath);
                int channels = reader.WaveFormat.Channels;
                int sampleRate = reader.WaveFormat.SampleRate;
                long sourceSamples = reader.Length / (channels * sizeof(float));
                var format = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, channels);

                float[] source = new float[sourceSamples * channels];
                float[] readBuf = new float[FloatBufferSize * channels];
                int totalRead = 0;
                int read;
                while ((read = reader.Read(readBuf, 0, readBuf.Length)) > 0)
                {
                    Array.Copy(readBuf, 0, source, totalRead, read);
                    totalRead += read;
                }

                long outputSamples = sourceSamples;
                float[] output = new float[outputSamples * channels];
                double srcPos = 0;

                for (long outIdx = 0; outIdx < outputSamples; outIdx++)
                {
                    long srcIdx = (long)srcPos;
                    double frac = srcPos - srcIdx;

                    for (int ch = 0; ch < channels; ch++)
                    {
                        long s0 = srcIdx * channels + ch;
                        long s1 = (srcIdx + 1) * channels + ch;

                        float sample;
                        if (s0 >= source.Length)
                        {
                            sample = 0;
                        }
                        else if (s1 >= source.Length)
                        {
                            sample = source[s0];
                        }
                        else
                        {
                            sample = (float)(source[s0] + (source[s1] - source[s0]) * frac);
                        }

                        output[outIdx * channels + ch] = sample;
                    }

                    srcPos += pitchFactor;
                }

                using var writer = new WaveFileWriter(tempFile, format);
                int writeBufSize = FloatBufferSize * channels;
                for (int i = 0; i < output.Length; i += writeBufSize)
                {
                    int count = Math.Min(writeBufSize, output.Length - i);
                    writer.WriteSamples(output, i, count);
                }

                return tempFile;
            }
            catch
            {
                AudioContext.TryDelete(tempFile);
                throw;
            }
        }

        public string? InsertSilence(double positionSeconds, double silenceDurationSeconds)
        {
            if (_context.AudioStream == null) return null;
            if (silenceDurationSeconds <= 0) throw new ArgumentException("Длительность тишины должна быть > 0");

            string tempFile = _context.CreateTempFilePath();
            try
            {
                using var reader = new AudioFileReader(_context.CurrentFilePath);
                var fmt = reader.WaveFormat;
                int channels = fmt.Channels;
                int sampleRate = fmt.SampleRate;
                var format = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, channels);
                long silenceSamples = (long)(silenceDurationSeconds * sampleRate) * channels;

                using var writer = new WaveFileWriter(tempFile, format);
                long bytesBefore = (long)(positionSeconds * fmt.AverageBytesPerSecond);
                CopyBytes(reader, writer, bytesBefore);

                float[] silence = new float[Math.Min(silenceSamples, FloatBufferSize * channels)];
                long written = 0;
                while (written < silenceSamples)
                {
                    int toWrite = (int)Math.Min(silence.Length, silenceSamples - written);
                    writer.WriteSamples(silence, 0, toWrite);
                    written += toWrite;
                }

                CopyFromPosition(reader, writer, positionSeconds);
                return tempFile;
            }
            catch
            {
                AudioContext.TryDelete(tempFile);
                throw;
            }
        }

        public bool HasClipboard => _clipboard != null;
        public double ClipboardDuration
        {
            get
            {
                if (_clipboard == null || _clipboardFormat == null) return 0;
                return (double)(_clipboard.Length / _clipboardFormat.Channels) / _clipboardFormat.SampleRate;
            }
        }

        public void CopySelection(double startSeconds, double endSeconds)
        {
            if (_context.AudioStream == null) return;
            if (startSeconds >= endSeconds) throw new ArgumentException("Некорректное выделение");

            using var reader = new AudioFileReader(_context.CurrentFilePath);
            reader.CurrentTime = TimeSpan.FromSeconds(startSeconds);
            var fmt = reader.WaveFormat;
            long bytesToCopy = (long)((endSeconds - startSeconds) * fmt.AverageBytesPerSecond);
            int blockAlign = fmt.BlockAlign;
            bytesToCopy -= bytesToCopy % blockAlign;
            long floatsToCopy = bytesToCopy / sizeof(float);

            _clipboard = new float[floatsToCopy];
            int totalRead = 0;
            while (totalRead < floatsToCopy)
            {
                int toRead = (int)Math.Min(FloatBufferSize * fmt.Channels, floatsToCopy - totalRead);
                int read = reader.Read(_clipboard, totalRead, toRead);
                if (read == 0) break;
                totalRead += read;
            }
            Array.Resize(ref _clipboard, totalRead);
            _clipboardFormat = fmt;
        }

        public string? PasteAt(double positionSeconds)
        {
            if (_context.AudioStream == null || _clipboard == null || _clipboardFormat == null) return null;

            string tempFile = _context.CreateTempFilePath();
            try
            {
                using var reader = new AudioFileReader(_context.CurrentFilePath);
                var fmt = reader.WaveFormat;
                var format = WaveFormat.CreateIeeeFloatWaveFormat(fmt.SampleRate, fmt.Channels);

                using var writer = new WaveFileWriter(tempFile, format);
                long bytesBefore = (long)(positionSeconds * fmt.AverageBytesPerSecond);
                CopyBytes(reader, writer, bytesBefore);

                int writeBufSize = FloatBufferSize * fmt.Channels;
                for (int i = 0; i < _clipboard.Length; i += writeBufSize)
                {
                    int count = Math.Min(writeBufSize, _clipboard.Length - i);
                    writer.WriteSamples(_clipboard, i, count);
                }

                CopyFromPosition(reader, writer, positionSeconds);
                return tempFile;
            }
            catch
            {
                AudioContext.TryDelete(tempFile);
                throw;
            }
        }

        public string? TrimSilence(float threshold)
        {
            if (_context.AudioStream == null) return null;

            string tempFile = _context.CreateTempFilePath();
            try
            {
                using var reader = new AudioFileReader(_context.CurrentFilePath);
                int channels = reader.WaveFormat.Channels;
                var format = reader.WaveFormat;

                float[] allSamples;
                using (var ms = new MemoryStream())
                {
                    float[] buf = new float[FloatBufferSize * channels];
                    int read;
                    while ((read = reader.Read(buf, 0, buf.Length)) > 0)
                    {
                        byte[] bytes = new byte[read * sizeof(float)];
                        Buffer.BlockCopy(buf, 0, bytes, 0, read * sizeof(float));
                        ms.Write(bytes, 0, bytes.Length);
                    }
                    allSamples = new float[ms.Length / sizeof(float)];
                    ms.Position = 0;
                    for (int i = 0; i < allSamples.Length; i++)
                    {
                        byte[] b = new byte[4];
                        ms.Read(b, 0, 4);
                        allSamples[i] = BitConverter.ToSingle(b, 0);
                    }
                }

                int sampleRate = reader.WaveFormat.SampleRate;
                int startIdx = 0;
                while (startIdx < allSamples.Length)
                {
                    bool allSilent = true;
                    for (int ch = 0; ch < channels; ch++)
                    {
                        if (Math.Abs(allSamples[startIdx + ch]) > threshold) { allSilent = false; break; }
                    }
                    if (!allSilent) break;
                    startIdx += channels;
                }

                int endIdx = allSamples.Length;
                while (endIdx > startIdx)
                {
                    bool allSilent = true;
                    for (int ch = 0; ch < channels; ch++)
                    {
                        if (Math.Abs(allSamples[endIdx - channels + ch]) > threshold) { allSilent = false; break; }
                    }
                    if (!allSilent) break;
                    endIdx -= channels;
                }

                startIdx -= startIdx % channels;
                endIdx += (channels - (endIdx % channels)) % channels;
                if (endIdx > allSamples.Length) endIdx = allSamples.Length;

                int trimmedLength = endIdx - startIdx;
                if (trimmedLength <= 0) return null;

                using var writer = new WaveFileWriter(tempFile, format);
                writer.WriteSamples(allSamples, startIdx, trimmedLength);
                return tempFile;
            }
            catch
            {
                AudioContext.TryDelete(tempFile);
                throw;
            }
        }

        public string? ApplyEQ(float bassGain, float trebleGain)
        {
            if (_context.AudioStream == null) return null;
            if (bassGain == 0 && trebleGain == 0) return null;

            string tempFile = _context.CreateTempFilePath();
            try
            {
                using var reader = new AudioFileReader(_context.CurrentFilePath);
                int channels = reader.WaveFormat.Channels;
                int sampleRate = reader.WaveFormat.SampleRate;
                var format = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, channels);

                double bassCutoff = 300.0;
                double trebleCutoff = 3000.0;
                double rc_bass = 1.0 / (2.0 * Math.PI * bassCutoff);
                double dt = 1.0 / sampleRate;
                double alphaBass = dt / (rc_bass + dt);
                double rc_treble = 1.0 / (2.0 * Math.PI * trebleCutoff);
                double alphaTreble = dt / (rc_treble + dt);

                using var writer = new WaveFileWriter(tempFile, format);
                float[] buffer = new float[FloatBufferSize * channels];
                float[] prevBass = new float[channels];
                float[] prevTreble = new float[channels];
                int read;

                while ((read = reader.Read(buffer, 0, buffer.Length)) > 0)
                {
                    for (int i = 0; i < read; i += channels)
                    {
                        for (int ch = 0; ch < channels; ch++)
                        {
                            float sample = buffer[i + ch];

                            prevBass[ch] = (float)(prevBass[ch] + alphaBass * (sample - prevBass[ch]));
                            float bass = prevBass[ch];

                            prevTreble[ch] = (float)(prevTreble[ch] + alphaTreble * (sample - prevTreble[ch]));
                            float treble = sample - prevTreble[ch];

                            buffer[i + ch] = sample
                                + bassGain * bass
                                + trebleGain * treble;
                        }
                    }
                    writer.WriteSamples(buffer, 0, read);
                }
                return tempFile;
            }
            catch
            {
                AudioContext.TryDelete(tempFile);
                throw;
            }
        }

        public string? ToMono()
        {
            if (_context.AudioStream == null) return null;

            string tempFile = _context.CreateTempFilePath();
            try
            {
                using var reader = new AudioFileReader(_context.CurrentFilePath);
                if (reader.WaveFormat.Channels == 1) return null;

                var format = WaveFormat.CreateIeeeFloatWaveFormat(reader.WaveFormat.SampleRate, 1);
                using var writer = new WaveFileWriter(tempFile, format);
                float[] buffer = new float[FloatBufferSize * reader.WaveFormat.Channels];
                float[] monoBuffer = new float[FloatBufferSize];
                int read;

                while ((read = reader.Read(buffer, 0, buffer.Length)) > 0)
                {
                    int frames = read / reader.WaveFormat.Channels;
                    for (int i = 0; i < frames; i++)
                    {
                        float sum = 0;
                        for (int ch = 0; ch < reader.WaveFormat.Channels; ch++)
                            sum += buffer[i * reader.WaveFormat.Channels + ch];
                        monoBuffer[i] = sum / reader.WaveFormat.Channels;
                    }
                    writer.WriteSamples(monoBuffer, 0, frames);
                }
                return tempFile;
            }
            catch
            {
                AudioContext.TryDelete(tempFile);
                throw;
            }
        }

        public string? ToStereo()
        {
            if (_context.AudioStream == null) return null;

            string tempFile = _context.CreateTempFilePath();
            try
            {
                using var reader = new AudioFileReader(_context.CurrentFilePath);
                if (reader.WaveFormat.Channels == 2) return null;

                var format = WaveFormat.CreateIeeeFloatWaveFormat(reader.WaveFormat.SampleRate, 2);
                using var writer = new WaveFileWriter(tempFile, format);
                float[] buffer = new float[FloatBufferSize];
                float[] stereoBuffer = new float[FloatBufferSize * 2];
                int read;

                while ((read = reader.Read(buffer, 0, buffer.Length)) > 0)
                {
                    for (int i = 0; i < read; i++)
                    {
                        stereoBuffer[i * 2] = buffer[i];
                        stereoBuffer[i * 2 + 1] = buffer[i];
                    }
                    writer.WriteSamples(stereoBuffer, 0, read * 2);
                }
                return tempFile;
            }
            catch
            {
                AudioContext.TryDelete(tempFile);
                throw;
            }
        }
    }
}
