namespace EditWave.Abstractions
{
    public interface IAudioEffect
    {
        float[] Process(float[] samples);
    }
}
