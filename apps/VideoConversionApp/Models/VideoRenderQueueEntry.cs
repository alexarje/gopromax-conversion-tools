using VideoConversionApp.Abstractions;

namespace VideoConversionApp.Models;

public class VideoRenderQueueEntry
{
    public IConvertableVideo Video { get; set; }
    public float Progress { get; set; } = 0;
    public bool Success { get; set; } = false;
    public bool Canceled { get; set; } = false;

    public VideoRenderQueueEntry(IConvertableVideo video)
    {
        Video = video;
    }
}