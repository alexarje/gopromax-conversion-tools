using System.Text.Json.Serialization;

namespace VideoConversionApp.Models;

public struct AppSettings
{
    [JsonPropertyName("ffmpegPath")]
    public string FfmpegPath = "ffmpeg";

    [JsonPropertyName("ffprobePath")]
    public string FfprobePath = "ffprobe";

    [JsonPropertyName("thumbnailAtPosition")]
    public int ThumbnailAtPosition = 50;

    [JsonPropertyName("numberOfSnapshotFrames")]
    public int NumberOfSnapshotFrames = 8;
    
    [JsonPropertyName("numberOfSnapshotProcessingThreads")]
    public int NumberOfSnapshotProcessingThreads = 3;
    
    [JsonPropertyName("numberOfThumbnailProcessingThreads")]
    public int NumberOfThumbnailProcessingThreads = 2;

    public AppSettings()
    {
    }
}


// public string FfmpegPath { init; set; } = "ffmpeg";
    // public string FfprobePath { init; set; } = "ffprobe";
