namespace VideoConversionApp.Models;

/// <summary>
/// Settings for conversion. Combined with MediaInfo this will act
/// as an input for the conversion process.
/// </summary>
public class ConversionSettings
{
    public string OutputDirectory { get; set; }
    public bool OutputBesideOriginals { get; set; }
    public string OutputFilenamePattern { get; set; }
    public string VideoCodecinFfmpeg { get; set; }
    public string AudioCodecinFfmpeg { get; set; }
    public bool OutputAudio { get; set; }
    
}