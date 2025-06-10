using System;
using VideoConversionApp.Models;

namespace VideoConversionApp.Abstractions;

/// <summary>
/// Manager of the conversion process pipeline.
/// Contains the methods to control the conversion queue.
/// </summary>
public interface IConversionManager
{
    public event EventHandler<IConvertibleVideoModel?> PreviewedVideoChanged;
    
    IConvertibleVideoModel AddVideoToPool(IMediaInfo mediaInfo);
    void RemoveVideoFromPool(IConvertibleVideoModel video);
    void SetPreviewedVideo(IConvertibleVideoModel? video);
    IConvertibleVideoModel? GetPreviewedVideo();
}