using System.Text.Json.Serialization;
using YamlDotNet.Serialization;

namespace VideoConversionApp.Models;

/// <summary>
/// Settings construct.
/// </summary>
public struct AppSettings
{
    [YamlMember(Alias = "ffmpegPath")]
    public string FfmpegPath = "ffmpeg";

    [YamlMember(Alias = "ffprobePath")]
    public string FfprobePath = "ffprobe";
    
    [YamlMember(Alias = "exifToolPath")]
    public string ExifToolPath = "exiftool";

    [YamlMember(Alias = "thumbnailAtPosition")]
    public int ThumbnailAtPosition = 50;

    [YamlMember(Alias = "numberOfSnapshotFrames")]
    public int NumberOfSnapshotFrames = 10;
    
    [YamlMember(Alias = "numberOfThumbnailProcessingThreads")]
    public int NumberOfThumbnailProcessingThreads = 3;

    public AppSettings()
    {
    }
}


// public string FfmpegPath { init; set; } = "ffmpeg";
    // public string FfprobePath { init; set; } = "ffprobe";
