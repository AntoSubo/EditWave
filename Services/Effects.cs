using EditWave.Abstractions;

namespace EditWave.Services
{
    public class GainEffect : IAudioEffect
    {
        private readonly float _gainFactor;
        public GainEffect(float gainFactor) => _gainFactor = gainFactor;

        public float[] Process(float[] samples)
        {
            for (int i = 0; i < samples.Length; i++)
            {
                samples[i] *= _gainFactor;
                if (samples[i] > 1.0f) samples[i] = 1.0f;
                if (samples[i] < -1.0f) samples[i] = -1.0f;
            }
            return samples;
        }
    }

    public class ReverseEffect : IAudioEffect
    {
        public float[] Process(float[] samples)
        {
            Array.Reverse(samples);
            return samples;
        }
    }

    public class NormalizeEffect : IAudioEffect
    {
        private float _peak;

        public float[] Process(float[] samples)
        {
            for (int i = 0; i < samples.Length; i++)
            {
                float abs = Math.Abs(samples[i]);
                if (abs > _peak) _peak = abs;
            }
            return samples;
        }

        public float GetPeak() => _peak;
    }
}
