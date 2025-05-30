using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using VideoConversionApp.Models;

namespace VideoConversionApp.Abstractions;

public interface IMediaPreviewService
{
    Task<byte[]?> GenerateThumbnailAsync(MediaInfo mediaInfo);

    Task<IList<byte[]>> GenerateSnapshotFramesAsync(MediaInfo mediaInfo, int numberOfFrames,
        Action<double>? progressCallback = null,
        CancellationToken cancellationToken = default);
    
    Task<byte[]?> QueueThumbnailGenerationAsync(MediaInfo mediaInfo);
    Task<byte[]?> QueueSnapshotFrameAsync(MediaInfo mediaInfo, long positionMilliseconds, CancellationToken cancellationToken);
    void ClearSnapshotFrameQueue();
}