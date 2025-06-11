using System;
using VideoConversionApp.Models;

namespace VideoConversionApp.Abstractions;

/// <summary>
/// Manager of the conversion process pipeline.
/// Contains the methods to control the conversion queue.
/// </summary>
public interface IConversionManager
{
    IConvertableVideo GetPlaceholderVideo();
    IConvertableVideo AddVideoToPool(IMediaInfo mediaInfo);
    void RemoveVideoFromPool(IConvertableVideo video);
}