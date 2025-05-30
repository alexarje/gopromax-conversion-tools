using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
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
    private readonly IAvFilterFactory _avFilterFactory;
    private string _singleFrameAvFilter = string.Empty; // DEPRECATED
    private string _avFilterTemplate = string.Empty;
    private List<Thread> _thumbnailQueueProcessingThreads = new List<Thread>();
    private List<Thread> _snapshotQueueProcessingThreads = new List<Thread>();

    private ConcurrentQueue<ThreadWorkItem<MediaInfo, TaskCompletionSource<byte[]?>>> _thumbnailGenerationQueue = new();
    private ConcurrentQueue<ThreadWorkItem<(MediaInfo mediaInfo, long timePosMs), TaskCompletionSource<byte[]?>>> _snapshotFrameGenerationQueue = new();

    
    public MediaPreviewService(IAppSettingsService appSettingsService,
        IAvFilterFactory avFilterFactory)
    {
        _appSettingsService = appSettingsService;
        _avFilterFactory = avFilterFactory;
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
        Action<double>? progressCallback = null,
        CancellationToken cancellationToken = default)
    {
        if (numberOfFrames < 2)
            throw new ArgumentException("Number of frames must be at least 2");
     
        var appSettings = _appSettingsService.GetSettings();
        var skipLength = mediaInfo.DurationMilliseconds / (numberOfFrames - 1);

        var avFilterString = _avFilterFactory.BuildAvFilter(new AvFilterFrameSelectCondition()
        {
            FrameDistance = skipLength / 1000,
            KeyFramesOnly = true
        }, new AvFilterFrameRotation()
        {
            Pitch = 0,
            Roll = 0,
            Yaw = 0
        });
        
        // TODO caching
        var genId = Guid.NewGuid();
        var tempPath = Path.Combine(Path.GetTempPath(), genId.ToString());
        Directory.CreateDirectory(tempPath);
        
        var tmpFrameFilePath = Path.Combine(tempPath, "snapshot-%06d.jpg");

        // TODO apply these settings everywhere!
        var processStartInfo = new ProcessStartInfo(appSettings.FfmpegPath,
            [
                //"-skip_frame", "nokey",
                "-discard", "nokey",
                "-y",
                //"-loglevel", "8",
                "-progress", "pipe:1",
                "-stats_period", "0.25",
                "-vsync", "0",
                "-filter_complex", avFilterString,
                "-i", mediaInfo.Filename,
                "-map", "[OUTPUT_FRAME]",
                "-f", "image2",
                "-s", "672x398",
                tmpFrameFilePath
            ])
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardInput = true,
                //RedirectStandardError = true,
                RedirectStandardOutput = true
            };
            

        
        
        try
        {
            var process = new Process()
            {
                StartInfo = processStartInfo,
                EnableRaisingEvents = true
            };
            process.Start();
            //process.BeginErrorReadLine();
            process.BeginOutputReadLine();

            process!.OutputDataReceived += (sender, args) =>
            {
                Console.WriteLine(args.Data);
                if (!string.IsNullOrEmpty(args.Data) && Regex.IsMatch(args.Data, @"^frame=\d"))
                {
                    var renderedFrameNumber = int.Parse(args.Data.Split("=")[1], CultureInfo.InvariantCulture);
                    progressCallback?.Invoke(Math.Round((double)renderedFrameNumber / numberOfFrames * 100.0, 2));
                }
                if (!string.IsNullOrEmpty(args.Data) && Regex.IsMatch(args.Data, @"^progress=end"))
                {
                    progressCallback?.Invoke(100.0);
                }
            };
            await process!.WaitForExitAsync(cancellationToken);
            if (process.ExitCode == 0 && Directory.Exists(tempPath))
            {
                var frames = Directory.GetFiles(tempPath, "*.jpg");
                var frameBytes = new byte[frames.Length][];

                for (var i = 0; i < frames.Length; i++)
                {
                    frameBytes[i] = await File.ReadAllBytesAsync(frames[i]);
                    File.Delete(frames[i]);
                }
                
                return frameBytes;
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
        
        return Array.Empty<byte[]>();
        
    }
}