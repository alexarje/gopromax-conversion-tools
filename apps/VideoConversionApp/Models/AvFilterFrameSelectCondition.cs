namespace VideoConversionApp.Models;

/// <summary>
/// Frame selection parameters, used in FFMPEG complex filter.
/// </summary>
public class AvFilterFrameSelectCondition
{
    /// <summary>
    /// Output/handle key frames only (speeds up things a lot).
    /// </summary>
    public bool KeyFramesOnly { get; set; }
    
    /// <summary>
    /// Frame minimum "distance" in seconds to the previous frame.
    /// Effectively creates a custom frame rate.
    /// </summary>
    public double? FrameDistance { get; set; }
}