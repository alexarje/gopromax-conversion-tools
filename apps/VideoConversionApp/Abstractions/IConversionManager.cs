using System;
using System.Collections.Generic;
using VideoConversionApp.Models;

namespace VideoConversionApp.Abstractions;

/// <summary>
/// Manager of the conversion process pipeline.
/// Contains the methods to control the conversion queue.
/// </summary>
public interface IConversionManager
{
    IReadOnlyList<IConvertableVideo> ConversionCandidates { get; }
    
    IConvertableVideo GetPlaceholderVideo();
    IConvertableVideo GetDummyVideo();
    IConvertableVideo AddVideoToPool(IMediaInfo mediaInfo);
    void RemoveVideoFromPool(IConvertableVideo video);
    ConversionSettings GetConversionSettings();
    void SetConversionSettings(ConversionSettings settings);
    string GetFilenameFromPattern(IMediaInfo mediaInfo, TimelineCrop crop, string pattern);
}