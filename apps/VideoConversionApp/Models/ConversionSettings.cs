using System;

namespace VideoConversionApp.Models;

/// <summary>
/// Settings for conversion. Combined with MediaInfo this will act
/// as an input for the conversion process.
/// </summary>
public class ConversionSettings
{
    public event EventHandler<string>? OutputFilenamePatternChanged;

    public string OutputFilenamePattern
    {
        get => field;
        set
        {
            if (field == value)
                return;
            
            field = value;
            OutputFilenamePatternChanged?.Invoke(this, value);
        }
    }
    
    public bool OutputBesideOriginals { get; set; }
    public string OutputDirectory { get; set; }
    public string VideoCodecinFfmpeg { get; set; }
    public string AudioCodecinFfmpeg { get; set; }
    public bool OutputAudio { get; set; }
    
    
}