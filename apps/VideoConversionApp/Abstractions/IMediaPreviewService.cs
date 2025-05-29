using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using VideoConversionApp.Models;

namespace VideoConversionApp.Abstractions;

public interface IMediaPreviewService
{
    Task<byte[]?> GenerateThumbnailAsync(MediaInfo mediaInfo);
    void QueueThumbnailGeneration(MediaInfo mediaInfo, Action<Bitmap> callback);
    Task<IList<byte[]>> GenerateSnapshotFramesAsync(MediaInfo mediaInfo, int numberOfFrames);
}