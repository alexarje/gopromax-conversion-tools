using System;

namespace VideoConversionApp.Models;

/// <summary>
/// Describes the media (one video), very basic stuff.
/// </summary>
public class MediaInfo(string filename)
{
    public string Filename { get; set; } = filename;
    public bool IsValidVideo { get; set; }
    public bool IsGoProMaxFormat { get; set; }
    public long DurationSeconds { get; set; }
    public DateTime CreatedDateTime { get; set; }
    public long SizeBytes { get; set; }
    public string[]? ValidationIssues { get; set; }
}