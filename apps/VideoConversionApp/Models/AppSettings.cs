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

    public AppSettings()
    {
    }
}


// public string FfmpegPath { init; set; } = "ffmpeg";
    // public string FfprobePath { init; set; } = "ffprobe";
