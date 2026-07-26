namespace EditWave.Abstractions
{
    public interface IFileExporter
    {
        void Export(string filePath);
        void ExportSelected(string filePath, double startSeconds, double endSeconds);
        void Export(string filePath, int mp3Bitrate);
        void ExportSelected(string filePath, double startSeconds, double endSeconds, int mp3Bitrate);
    }
}
