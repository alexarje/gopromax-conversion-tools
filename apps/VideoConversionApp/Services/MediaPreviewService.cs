using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using VideoConversionApp.Abstractions;
using VideoConversionApp.Models;
using File = System.IO.File;

namespace VideoConversionApp.Services;

public class MediaPreviewService : IMediaPreviewService
{
    /// <summary>
    /// A job class for our queues.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="TResult"></typeparam>
    private class ThreadWorkItem<T, TResult>
    {
        public T WorkItem;
        public TResult Result;
        public CancellationToken CancellationToken;
    }

    private IAppSettingsService _appSettingsService;
    private readonly IAvFilterFactory _avFilterFactory;
    private List<Thread> _thumbnailQueueProcessingThreads = new();

    private ConcurrentQueue<ThreadWorkItem<(MediaInfo mediaInfo, long timePosMs), TaskCompletionSource<byte[]?>>> 
        _thumbnailGenerationQueue = new();

    
    public MediaPreviewService(IAppSettingsService appSettingsService,
        IAvFilterFactory avFilterFactory)
    {
        _appSettingsService = appSettingsService;
        _avFilterFactory = avFilterFactory;
    }

    
    public Task<byte[]?> QueueThumbnailGenerationAsync(MediaInfo mediaInfo, long timePositionMilliseconds)
    {
        var threadWorkItem = new ThreadWorkItem<(MediaInfo, long), TaskCompletionSource<byte[]?>>
        {
            WorkItem = (mediaInfo, timePositionMilliseconds),
            Result = new TaskCompletionSource<byte[]?>()
        };
        _thumbnailGenerationQueue.Enqueue(threadWorkItem);
        // Run immediately.
        RunThumbnailProcessingThreads();
        return threadWorkItem.Result.Task;
    }

    /// <summary>
    /// Creates/starts a new processing thread, as many as are allowed concurrently, and cleans up completed ones.
    /// </summary>
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

            if (_thumbnailQueueProcessingThreads.Count < threadCount)
            {
                var thread = new Thread(ProcessThumbnailQueue);
                _thumbnailQueueProcessingThreads.Add(thread);
                thread.Start();
            }
        }
    }
    
    /// <summary>
    /// The thumbnail processing thread function.
    /// Processes the queue.
    /// </summary>
    private void ProcessThumbnailQueue()
    {
        var appSettings = _appSettingsService.GetSettings();
        
        var avFilterString = _avFilterFactory.BuildAvFilter(
            new AvFilterFrameSelectCondition() { KeyFramesOnly = true }, AvFilterFrameRotation.Zero);
        
        while (_thumbnailGenerationQueue.TryDequeue(out var threadWorkItem))
        {
            var (mediaInfo, timePosMs) = threadWorkItem.WorkItem;
            var tmpThumbFilePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".jpg");
            var thumbTimePosition = timePosMs / 1000.0;
            
            var processStartInfo = new ProcessStartInfo(appSettings.FfmpegPath,
                [
                    "-loglevel", "8",
                    "-discard", "nokey",
                    "-y",
                    "-ss", $"{TimeSpan.FromSeconds(thumbTimePosition):hh\\:mm\\:ss}",
                    "-i", mediaInfo.Filename,
                    "-vsync", "0",
                    "-filter_complex", avFilterString,
                    "-map", "[OUTPUT_FRAME]",
                    "-frames:v", "1",
                    "-f", "image2",
                    "-s", "336x199",
                    tmpThumbFilePath
                ])
                {
                    CreateNoWindow = true,
                    UseShellExecute = false
                };

                
            try
            {
                var process = new Process()
                {
                    StartInfo = processStartInfo
                };
            
                process.Start();
                process!.WaitForExit();
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

    public async Task<IList<byte[]>> GenerateSnapshotFramesAsync(MediaInfo mediaInfo, 
        SnapshotFrameTransformationSettings settings, int numberOfFrames, 
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
        }, settings.Rotation);
        
        // TODO caching
        var genId = Guid.NewGuid();
        var tempPath = Path.Combine(Path.GetTempPath(), genId.ToString());
        Directory.CreateDirectory(tempPath);
        
        var tmpFrameFilePath = Path.Combine(tempPath, "snapshot-%06d.jpg");

        var processStartInfo = new ProcessStartInfo(appSettings.FfmpegPath,
            [
                "-loglevel", "8",
                "-discard", "nokey",
                "-y",
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
            cancellationToken.Register(() => process.Kill());

            process.BeginOutputReadLine();
            progressCallback?.Invoke(5);

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
                
                throw new Exception($"Ffmpeg process returned {process.ExitCode}");
            }
        }
        catch (TaskCanceledException e)
        {
            Console.WriteLine("Ffmpeg process cancellation: " + e.Message);
            throw;
        }
        catch (Exception e)
        {
            // TODO handle this
            Console.WriteLine("Exception: " + e.Message);
            throw;
        }
        
        
    }

    public async Task<KeyFrameVideo> GenerateKeyFrameVideoAsync(ConvertibleVideoModel convertibleVideo,
        Action<double>? progressCallback = null,
        CancellationToken cancellationToken = default)
    {
        var mediaInfo = convertibleVideo.MediaInfo;
        var rotation = convertibleVideo.FrameRotation;
        var appSettings = _appSettingsService.GetSettings();

        var avFilterString = _avFilterFactory.BuildAvFilter(new AvFilterFrameSelectCondition()
        {
            KeyFramesOnly = true
        }, rotation);
        
        var genId = Guid.NewGuid();
        var tempPath = Path.Combine(Path.GetTempPath(), "kfv-" + genId.ToString());
        Directory.CreateDirectory(tempPath);
        
        var tmpVideoFilePath = Path.Combine(tempPath, "keyframevideo.mp4");

        var processStartInfo = new ProcessStartInfo(appSettings.FfmpegPath,
            [
                "-loglevel", "8",
                "-discard", "nokey",
                "-y",
                "-progress", "pipe:1",
                "-stats_period", "0.25",
                "-vsync", "0",
                "-filter_complex", avFilterString,
                "-i", mediaInfo.Filename,
                "-map", "[OUTPUT_FRAME]",
                "-c:v", "prores",
                "-f", "mov",
                "-s", "672x398",
                "-movflags", "+faststart",
                tmpVideoFilePath
            ])
            {
                CreateNoWindow = true,
                UseShellExecute = false,
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
            cancellationToken.Register(() => process.Kill());

            process.BeginOutputReadLine();
            progressCallback?.Invoke(2);

            process!.OutputDataReceived += (sender, args) =>
            {
                Console.WriteLine(args.Data);
                if (!string.IsNullOrEmpty(args.Data) && Regex.IsMatch(args.Data, @"^out_time=\d"))
                {
                    var frameTime = TimeSpan.Parse(args.Data.Split("=")[1], CultureInfo.InvariantCulture);
                    var progress = frameTime.TotalMilliseconds / mediaInfo.DurationMilliseconds;
                    progressCallback?.Invoke(Math.Round(progress * 100.0, 2));
                }
                if (!string.IsNullOrEmpty(args.Data) && Regex.IsMatch(args.Data, @"^progress=end"))
                {
                    progressCallback?.Invoke(98.0);
                }
            };
            await process!.WaitForExitAsync(cancellationToken);
            if (process.ExitCode == 0 && File.Exists(tmpVideoFilePath))
            {
                var taggingProcessStartInfo = new ProcessStartInfo(appSettings.ExifToolPath,
                    [
                        "-api", "LargeFileSupport=1",
                        "-overwrite_original",
                        "-XMP-GSpherical:Spherical=true",
                        "-XMP-GSpherical:Stitched=true",
                        "-XMP-GSpherical:StitchingSoftware=MAXVideoConvert",
                        "-XMP-GSpherical:ProjectionType=equirectangular",
                        tmpVideoFilePath
                    ])
                {
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true
                };
                var taggingProcess = new Process()
                {
                    StartInfo = taggingProcessStartInfo,
                    EnableRaisingEvents = true
                };
                taggingProcess.Start();
                cancellationToken.Register(() => taggingProcess.Kill());
                
                await taggingProcess!.WaitForExitAsync(cancellationToken);
                progressCallback?.Invoke(100.0);
                if (taggingProcess.ExitCode == 0)
                {
                    return new KeyFrameVideo()
                    {
                        VideoPath = tmpVideoFilePath,
                        SourceVideo = convertibleVideo
                    };
                }
                throw new Exception($"ExifTool process returned {taggingProcess.ExitCode}");
            }
            else
            {
                Console.WriteLine(process.StandardError.ReadToEnd());
                Console.WriteLine(process.ExitCode);
                
                throw new Exception($"Ffmpeg process returned {process.ExitCode}");
            }
        }
        catch (TaskCanceledException e)
        {
            Console.WriteLine("Ffmpeg process cancellation: " + e.Message);
            throw;
        }
        catch (Exception e)
        {
            // TODO handle this
            Console.WriteLine("Exception: " + e.Message);
            throw;
        }
        
    }
}