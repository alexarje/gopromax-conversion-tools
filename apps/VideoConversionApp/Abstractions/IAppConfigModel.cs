namespace VideoConversionApp.Abstractions;

/// <summary>
/// Application configuration, including the conversion settings that are
/// saved across sessions.
/// </summary>
public interface IAppConfigModel
{
    /// <summary>
    /// Path related configuration.
    /// </summary>
    public IConfigPaths Paths { get; }
    
    /// <summary>
    /// Conversion related configs.
    /// </summary>
    public IConfigConversion Conversion { get; }
    
    /// <summary>
    /// Video previewing related configs.
    /// </summary>
    public IConfigPreviews Previews { get; }
}


public interface IConfigPaths
{
    /// <summary>
    /// Path to exiftool binary.
    /// </summary>
    public string Exiftool { get; set; }
    
    /// <summary>
    /// Path to ffmpeg binary.
    /// </summary>
    public string Ffmpeg { get; set; }
    
    /// <summary>
    /// Path to ffprobe binary.
    /// </summary>
    public string Ffprobe { get; set; }
}

public interface IConfigPreviews
{
    /// <summary>
    /// Number of snapshot frames to generate when the video is selected for previewing.
    /// </summary>
    public uint NumberOfSnapshotFrames { get; set; }
    
    /// <summary>
    /// Number of concurrent threads that generate video thumbnails.
    /// </summary>
    public uint NumberOfThumbnailThreads { get; set; }
    
    /// <summary>
    /// Time position (percent) in video for generating thumbnails.
    /// </summary>
    public uint ThumbnailTimePositionPcnt { get; set; }
}

public interface IConfigConversion
{
    /// <summary>
    /// Audio codec used in conversion.
    /// </summary>
    public string CodecAudio { get; set; }
    
    /// <summary>
    /// Video codec used in conversion.
    /// </summary>
    public string CodecVideo { get; set; }
    
    /// <summary>
    /// Output audio or not when converting.
    /// </summary>
    public bool OutputAudio { get; set; }
    
    /// <summary>
    /// Output the converted videos beside their originals when converting.
    /// </summary>
    public bool OutputBesideOriginals { get; set; }
    
    /// <summary>
    /// The selected output directory for converted videos.
    /// </summary>
    public string OutputDirectory { get; set; }
    
    /// <summary>
    /// Filename naming pattern for the converted videos.
    /// </summary>
    public string OutputFilenamePattern { get; set; }

}
