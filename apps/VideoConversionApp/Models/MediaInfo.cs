using System;

namespace VideoConversionApp.Models;

/// <summary>
/// Describes the media (one video), very basic stuff.
/// </summary>
public class MediaInfo
{
    public MediaInfo(string filename, bool isValidVideo, bool isGoProMaxFormat, 
        long durationMilliseconds, DateTime createdDateTime, long sizeBytes, string[]? validationIssues)
    {
        Filename = filename;
        IsValidVideo = isValidVideo;
        IsGoProMaxFormat = isGoProMaxFormat;
        DurationInSeconds = (decimal)durationMilliseconds / 1000;
        CreatedDateTime = createdDateTime;
        SizeBytes = sizeBytes;
        ValidationIssues = validationIssues;
    }

    public MediaInfo(string filename, bool isValidVideo, bool isGoProMaxFormat)
    {
        Filename = filename;
        IsValidVideo = isValidVideo;
        IsGoProMaxFormat = isGoProMaxFormat;
    }
    
    public string Filename { get; }
    public bool IsValidVideo { get; }
    public bool IsGoProMaxFormat { get; }
    public decimal DurationInSeconds { get; }
    public DateTime CreatedDateTime { get; }
    public long SizeBytes { get; }
    public string[]? ValidationIssues { get; }
}