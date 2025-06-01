namespace VideoConversionApp.Models;

public class KeyFrameVideo
{
    public string VideoPath { get; set; }
    public ConvertibleVideoModel SourceVideo { get; set; } 
    public int FramesGenerated { get; set; }
}