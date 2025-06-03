namespace VideoConversionApp.Models;

/// <summary>
/// Represents a single convertible 360 video, along with its conversion parameters.
/// </summary>
public class ConvertibleVideoModel
{
    public MediaInfo MediaInfo { get; private set; }
    public bool IsEnabledForConversion { get; set; } = false;
    
    public AvFilterFrameRotation FrameRotation { get; set; }
    public TimelineCrop TimelineCrop { get; set; }
    
    public ConvertibleVideoModel(MediaInfo mediaInfo)
    {
        MediaInfo = mediaInfo;
        FrameRotation = AvFilterFrameRotation.Zero;
        TimelineCrop = new TimelineCrop();
    }
    
    
}