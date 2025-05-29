using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using VideoConversionApp.Abstractions;
using VideoConversionApp.Models;

namespace VideoConversionApp.Services;

public class MediaPreviewService : IMediaPreviewService
{
    private class ThreadWorkItem<T, TResult>
    {
        public T WorkItem;
        public TResult Result;
        public CancellationToken CancellationToken;
    }

    private IAppSettingsService _appSettingsService;
    private string _singleFrameAvFilter = string.Empty;
    private List<Thread> _thumbnailQueueProcessingThreads = new List<Thread>();
    private List<Thread> _snapshotQueueProcessingThreads = new List<Thread>();

    private ConcurrentQueue<ThreadWorkItem<MediaInfo, TaskCompletionSource<byte[]?>>> _thumbnailGenerationQueue = new();
    private ConcurrentQueue<ThreadWorkItem<(MediaInfo mediaInfo, long timePosMs), TaskCompletionSource<byte[]?>>> _snapshotFrameGenerationQueue = new();

    
    public MediaPreviewService(IAppSettingsService appSettingsService)
    {
        _appSettingsService = appSettingsService;
        LoadSingleFrameAvFilter();
    }

    private void RunThumbnailProcessingThreads()
    {
        var threadCount = _appSettingsService.GetSettings().NumberOfThumbnailProcessingThreads;
        lock (_thumbnailQueueProcessingThreads)
        {
            for (var i = _thumbnailQueueProcessingThreads.Count - 1; i >= 0; i--)
            {
                if(!_thumbnailQueueProcessingThreads[i].IsAlive)
                    _thumbnailQueueProcessingThreads.RemoveAt(i);
            }

            while (_thumbnailQueueProcessingThreads.Count < threadCount)
            {
                var thread = new Thread(ProcessThumbnailQueue);
                _thumbnailQueueProcessingThreads.Add(thread);
                thread.Start();
            }
        }
    }
    
    private void RunSnapshotFrameProcessingThreads()
    {
        var threadCount = _appSettingsService.GetSettings().NumberOfSnapshotProcessingThreads;
        lock (_snapshotQueueProcessingThreads)
        {
            for (var i = _snapshotQueueProcessingThreads.Count - 1; i >= 0; i--)
            {
                if(!_snapshotQueueProcessingThreads[i].IsAlive)
                    _snapshotQueueProcessingThreads.RemoveAt(i);
            }

            while (_snapshotQueueProcessingThreads.Count < threadCount)
            {
                var thread = new Thread(ProcessSnapshotFrameQueue);
                _snapshotQueueProcessingThreads.Add(thread);
                thread.Start();
            }
        }
        
    }
    
    public Task<byte[]?> QueueThumbnailGenerationAsync(MediaInfo mediaInfo)
    {
        var threadWorkItem = new ThreadWorkItem<MediaInfo, TaskCompletionSource<byte[]?>>
        {
            WorkItem = mediaInfo,
            Result = new TaskCompletionSource<byte[]?>()
        };
        _thumbnailGenerationQueue.Enqueue(threadWorkItem);
        RunThumbnailProcessingThreads();
        return threadWorkItem.Result.Task;
    }

    public void ClearSnapshotFrameQueue()
    {
        _snapshotFrameGenerationQueue.Clear();
    }
    
    public Task<byte[]?> QueueSnapshotFrameAsync(MediaInfo mediaInfo, long positionMilliseconds, 
        CancellationToken cancellationToken = default)
    {
        var threadWorkItem = new ThreadWorkItem<(MediaInfo mediaInfo, long timePosMs), TaskCompletionSource<byte[]?>>
        {
            WorkItem = (mediaInfo, positionMilliseconds),
            Result = new TaskCompletionSource<byte[]?>(),
            CancellationToken = cancellationToken
        };
        _snapshotFrameGenerationQueue.Enqueue(threadWorkItem);
        RunSnapshotFrameProcessingThreads();
        return threadWorkItem.Result.Task;
    }

    private void ProcessThumbnailQueue()
    {
        var appSettings = _appSettingsService.GetSettings();
        
        while (_thumbnailGenerationQueue.TryDequeue(out var threadWorkItem))
        {
            var mediaInfo = threadWorkItem.WorkItem;
            var tmpThumbFilePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".jpg");
            var thumbTimePosition = appSettings.ThumbnailAtPosition / 100.0 * mediaInfo.DurationSeconds;
            var processStartInfo = new ProcessStartInfo(appSettings.FfmpegPath,
            [
                "-skip_frame", "nokey",
                "-ss", $"{TimeSpan.FromSeconds(thumbTimePosition):hh\\:mm\\:ss}",
                "-i", mediaInfo.Filename,
                "-y",
                "-vsync", "0",
                "-filter_complex", _singleFrameAvFilter,
                "-map", "[OUTPUT_FRAME]",
                "-frames:v", "1",
                "-f", "image2",
                "-s", "336x199",
                tmpThumbFilePath
            ]);
            processStartInfo.CreateNoWindow = true;

            try
            {
                var process = Process.Start(processStartInfo);
                process.WaitForExit();
                if (process.ExitCode == 0 && File.Exists(tmpThumbFilePath))
                {
                    var imgBytes = File.ReadAllBytes(tmpThumbFilePath);
                    File.Delete(tmpThumbFilePath);
                    threadWorkItem.Result.TrySetResult(imgBytes);
                }
                else
                {
                    threadWorkItem.Result.TrySetResult(null);
                }
            }
            catch (Exception e)
            {
                threadWorkItem.Result.TrySetResult(null);
            }
        }
            
    }

    private void ProcessSnapshotFrameQueue()
    {
        var appSettings = _appSettingsService.GetSettings();

        while (_snapshotFrameGenerationQueue.TryDequeue(out var threadWorkItem))
        {
            if (threadWorkItem.CancellationToken.IsCancellationRequested)
            {
                threadWorkItem.Result.TrySetResult(null);
                continue;
            }

            var (mediaInfo, timePosMs) = threadWorkItem.WorkItem;
            var frameTimePosition = TimeSpan.FromSeconds(timePosMs / 1000.0).ToString(@"hh\:mm\:ss\.fff");
            var tmpFrameFilePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".jpg");
            
            var processStartInfo = new ProcessStartInfo(appSettings.FfmpegPath,
            [
                "-accurate_seek",
                "-skip_frame", "nokey",
                "-ss", frameTimePosition,
                "-i", mediaInfo.Filename,
                "-y",
                "-vsync", "0",
                "-filter_complex", _singleFrameAvFilter,
                "-map", "[OUTPUT_FRAME]",
                "-frames:v", "1",
                "-f", "image2",
                "-s", "672x398",
                tmpFrameFilePath
            ]);
            processStartInfo.CreateNoWindow = true;
            processStartInfo.RedirectStandardError = true;

            try
            {
                var process = Process.Start(processStartInfo);
                var t = process!.WaitForExitAsync(threadWorkItem.CancellationToken);
                t.Wait();
                if (process.ExitCode == 0 && File.Exists(tmpFrameFilePath))
                {
                    var imgBytes = File.ReadAllBytes(tmpFrameFilePath);
                    File.Delete(tmpFrameFilePath);
                    threadWorkItem.Result.TrySetResult(imgBytes);
                }
                else
                {
                    Console.WriteLine(process.StandardError.ReadToEnd());
                    Console.WriteLine(process.ExitCode);
                    threadWorkItem.Result.TrySetResult(null);
                }
            }
            catch (TaskCanceledException e)
            {
                Console.WriteLine("Ffmpeg process cancellation: " + e.Message);
            }
            catch (Exception e)
            {
                // TODO handle this
                threadWorkItem.Result.TrySetResult(null);
                Console.WriteLine("Error: " + e.Message);
            }

        }
    }

    private void LoadSingleFrameAvFilter()
    {
        if (_singleFrameAvFilter == string.Empty)
        {
            using var resourceStream = AssetLoader.Open(
                new Uri("avares://VideoConversionApp/Resources/360-to-equirect.avfilter"));
            using var reader = new StreamReader(resourceStream);
            _singleFrameAvFilter = reader.ReadToEnd();
        }
    }

    public async Task<byte[]?> GenerateThumbnailAsync(MediaInfo mediaInfo)
    {
        var tmpThumbFilePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".jpg");
        
        var appSettings = _appSettingsService.GetSettings();
        var thumbTimePosition = appSettings.ThumbnailAtPosition / 100.0 * mediaInfo.DurationSeconds;
        var processStartInfo = new ProcessStartInfo(appSettings.FfmpegPath,
        [
            "-skip_frame", "nokey",
            "-ss", $"{TimeSpan.FromSeconds(thumbTimePosition):hh\\:mm\\:ss}",
            "-i", mediaInfo.Filename,
            "-y",
            "-vsync", "0",
            "-filter_complex", _singleFrameAvFilter,
            "-map", "[OUTPUT_FRAME]",
            "-frames:v", "1",
            "-f", "image2",
            "-s", "336x199",
            tmpThumbFilePath
        ]);
        processStartInfo.CreateNoWindow = true;

        try
        {
            var process = Process.Start(processStartInfo);
            await process!.WaitForExitAsync();
            if (process.ExitCode == 0 && File.Exists(tmpThumbFilePath))
            {
                var imgBytes = await File.ReadAllBytesAsync(tmpThumbFilePath);
                File.Delete(tmpThumbFilePath);
                return imgBytes;
            }

            return null;
        }
        catch (Exception e)
        {
            return null;
        }

    }

    public async Task<IList<byte[]>> GenerateSnapshotFramesAsync(MediaInfo mediaInfo, int numberOfFrames, 
        CancellationToken cancellationToken = default)
    {
        if (numberOfFrames < 2)
            throw new ArgumentException("Number of frames must be at least 2");
     
        var appSettings = _appSettingsService.GetSettings();
        var skipLength = mediaInfo.DurationSeconds / (numberOfFrames - 1);
        var timePositions = new string[numberOfFrames];
        for (var i = 0; i < numberOfFrames; i++)
        {
            // Never go over max length of the video, and always 1000ms under.
            // There seems to be issues generating a frame at the absolute end time.
            var timePosSeconds = Math.Min((mediaInfo.DurationMilliseconds - 1000) / 1000.0, i * skipLength);
            timePositions[i] = TimeSpan.FromSeconds(timePosSeconds).ToString(@"hh\:mm\:ss\.fff");
        }
        
        var frames = new byte[numberOfFrames][];
        
        var parallelOpts = new ParallelOptions()
        {
            MaxDegreeOfParallelism = appSettings.NumberOfSnapshotFrames,
            CancellationToken = cancellationToken
        };
        
        
        // OK so problem here is:
        // We click an item, and this starts. A number of threads start processing with ffmpeg.
        // We click another item, cancellation is requested. These are already running.
        // The new threads already start and do not wait for anything.
        // Then we click yet another item, and we start more threads.
        // We should be queueing, not immediately starting. Then canceling should cancel all currently
        // queued items, so that their ffmpeg processes never start.
        await Parallel.ForAsync(0, numberOfFrames, parallelOpts, async (i, token) =>
        {
            if (token.IsCancellationRequested)
            {
                Console.WriteLine("Parallel snapshot cancellation requested, did not start.");
                return;
            }

            var appSettings = _appSettingsService.GetSettings();
            var tmpFrameFilePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".jpg");
        
            var frameTimePosition = timePositions[i];
            var processStartInfo = new ProcessStartInfo(appSettings.FfmpegPath,
            [
                "-accurate_seek",
                "-skip_frame", "nokey",
                "-ss", frameTimePosition,
                "-i", mediaInfo.Filename,
                "-y",
                "-vsync", "0",
                "-filter_complex", _singleFrameAvFilter,
                "-map", "[OUTPUT_FRAME]",
                "-frames:v", "1",
                "-f", "image2",
                "-s", "672x398",
                tmpFrameFilePath
            ]);
            processStartInfo.CreateNoWindow = true;
            processStartInfo.RedirectStandardError = true;

            try
            {
                var process = Process.Start(processStartInfo);
                await process!.WaitForExitAsync(token);
                if (process.ExitCode == 0 && File.Exists(tmpFrameFilePath))
                {
                    var imgBytes = await File.ReadAllBytesAsync(tmpFrameFilePath);
                    File.Delete(tmpFrameFilePath);
                    frames[i] = imgBytes;
                }
                else
                {
                    Console.WriteLine(process.StandardError.ReadToEnd());
                    Console.WriteLine(process.ExitCode);
                }
            }
            catch (TaskCanceledException e)
            {
                Console.WriteLine("Ffmpeg process cancellation: " + e.Message);
            }
            catch (Exception e)
            {
                // TODO handle this
                Console.WriteLine("Error: " + e.Message);
            }
        });

        return frames;

    }
}