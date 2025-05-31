namespace VideoConversionApp.Models;

/// <summary>
/// Frame rotation parameters for our complex AV filter. 
/// </summary>
public class AvFilterFrameRotation
{
    public int Yaw { get; set; }
    public int Pitch { get; set; }
    public int Roll { get; set; }

    public static AvFilterFrameRotation Zero => new AvFilterFrameRotation();
}