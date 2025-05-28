namespace VideoConversionApp.Models;

public class ConvertibleVideoModel
{
    public MediaInfo MediaInfo { get; private set; }
    public bool IsEnabledForConversion { get; set; } = false;
    
    public ConvertibleVideoModel(MediaInfo mediaInfo)
    {
        MediaInfo = mediaInfo;
    }
    
    
}