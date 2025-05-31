using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using VideoConversionApp.Models;

namespace VideoConversionApp.Abstractions;

/// <summary>
/// Service for generating media previews (thumbnails, snapshot frames, keyframe videos).
/// </summary>
public interface IMediaPreviewService
{
    /// <summary>
    /// Generate a number of snapshot frames from a GoPro Max 360 video.
    /// </summary>
    /// <param name="mediaInfo">Source media info</param>
    /// <param name="numberOfFrames">Number of frames to generate</param>
    /// <param name="progressCallback">Optional callback for receiving progress info</param>
    /// <param name="cancellationToken">Token for cancelling the process</param>
    /// <returns>A list of generated frames (images as byte arrays) when generation is complete</returns>
    Task<IList<byte[]>> GenerateSnapshotFramesAsync(MediaInfo mediaInfo, SnapshotFrameTransformationSettings settings, 
        int numberOfFrames, Action<double>? progressCallback = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Queues the generation of a video thumbnail of a GoPro Max 360 video.
    /// </summary>
    /// <param name="mediaInfo">Source media info</param>
    /// <param name="timePositionMilliseconds">The time position of the thumbnail</param>
    /// <returns>The task which when completed will provide the thumbnail image as bytes in the result.
    /// Note that since the task is queued, awaiting for it may take a long time.
    /// </returns>
    Task<byte[]?> QueueThumbnailGenerationAsync(MediaInfo mediaInfo, long timePositionMilliseconds);
}