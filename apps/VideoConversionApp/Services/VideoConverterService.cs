using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using VideoConversionApp.Abstractions;
using VideoConversionApp.Config;
using VideoConversionApp.Models;

namespace VideoConversionApp.Services;

public class VideoConverterService : IVideoConverterService
{
    private readonly IConfigManager _configManager;
    private readonly IAvFilterFactory _avFilterFactory;
    private List<CodecEntry>? _ffmpegEncodingVideoCodecs;
    private List<CodecEntry>? _ffmpegEncodingAudioCodecs;
    
    public event EventHandler? RenderingQueueProcessingStarted;
    public event EventHandler? RenderingQueueProcessingFinished;
    public event EventHandler<VideoRenderQueueEntry>? RenderingFailed;
    public event EventHandler<VideoRenderQueueEntry>? RenderingSucceeded;
    public event EventHandler<VideoRenderQueueEntry>? RenderingCanceled;

    private IList<VideoRenderQueueEntry>? _activeRenderingQueue;
    
    public VideoConverterService(IConfigManager configManager,
        IAvFilterFactory avFilterFactory)
    {
        _configManager = configManager;
        _avFilterFactory = avFilterFactory;
    }

    private void PopulateValidCodecs()
    {
        var audioCodecs = new List<CodecEntry>();
        var videoCodecs = new List<CodecEntry>();
        
        var pathsConfig = _configManager.GetConfig<PathsConfig>()!;
        var codecParseRegex = new Regex(
            @"^\s(?<decode>[D\.])(?<encode>[E\.])(?<type>[VASDT\.])([I\.])(?<lossy>[L\.])(?<lossless>[S\.])\s(?<name>\w*)\s*(?<desc>.*)$");
        
        var processStartInfo = new ProcessStartInfo(pathsConfig.Ffmpeg,
        [
            "-codecs"
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
            process.BeginOutputReadLine();
            process!.OutputDataReceived += (sender, args) =>
            {
                Console.WriteLine(args.Data);
                if (!string.IsNullOrEmpty(args.Data))
                {
                    var match = codecParseRegex.Match(args.Data);
                    if (match.Success)
                    {
                        var c = new CodecEntry()
                        {
                            Name = match.Groups["name"].Value,
                            Description = match.Groups["desc"].Value,
                            DecodingSupported = match.Groups["decode"].Value == "D",
                            EncodingSupported = match.Groups["encode"].Value == "E",
                            IsAudio = match.Groups["type"].Value == "A",
                            IsVideo = match.Groups["type"].Value == "V",
                            IsLossy = match.Groups["lossy"].Value == "L",
                            IsLossless = match.Groups["lossless"].Value == "S"
                        };
                        if (match.Groups["type"].Value == "A")
                            audioCodecs.Add(c);
                        if (match.Groups["type"].Value == "V")
                            videoCodecs.Add(c);
                    }
                }
            };
            process!.WaitForExit();
            if (process.ExitCode != 0)
            {
                throw new Exception("Process exited with exit code " + process.ExitCode);
            }
            _ffmpegEncodingAudioCodecs = audioCodecs;
            _ffmpegEncodingVideoCodecs = videoCodecs;
        }
        catch (Exception e)
        {
            throw new Exception("Failed to iterate ffmpeg video codecs", e);
        }
    }

    public IReadOnlyList<CodecEntry> GetAvailableVideoCodecs()
    {
        if (_ffmpegEncodingVideoCodecs == null)
            PopulateValidCodecs();
        return _ffmpegEncodingVideoCodecs!;
    }

    public IReadOnlyList<CodecEntry> GetAvailableAudioCodecs()
    {
        if (_ffmpegEncodingVideoCodecs == null)
            PopulateValidCodecs();
        return _ffmpegEncodingAudioCodecs!;
    }
    
    //public string GetFilenameFromPattern(IInputVideoInfo inputVideoInfo, TimelineCrop crop, string pattern)
    public string GetFilenameFromPattern(IConvertableVideo video, string pattern)
    {
        if (string.IsNullOrEmpty(pattern))
            throw new Exception("Filename pattern is empty");
            
        var crop = video.TimelineCrop;
        var inputVideoInfo = video.InputVideoInfo;
        
        var cropElems = new List<string>();
        if (crop.StartTimeSeconds != null && crop.StartTimeSeconds > 0)
        {
            var startTime = crop.StartTimeSeconds;
            var endTime = crop.EndTimeSeconds ?? inputVideoInfo.DurationInSeconds;
            cropElems.Add(TimeSpan.FromSeconds((double)startTime).ToString("hh\\-mm\\-ss"));
            cropElems.Add(TimeSpan.FromSeconds((double)endTime).ToString("hh\\-mm\\-ss"));
        }
        else if (crop.EndTimeSeconds != null && crop.EndTimeSeconds > 0)
        {
            var startTime = crop.StartTimeSeconds ?? 0;
            var endTime = crop.EndTimeSeconds;
            cropElems.Add(TimeSpan.FromSeconds((double)startTime).ToString("hh\\-mm\\-ss"));
            cropElems.Add(TimeSpan.FromSeconds((double)endTime).ToString("hh\\-mm\\-ss"));
        }
        // e.g. 00-01-22.332__00-02-44.692
        var cropString = string.Join("__", cropElems);

        var fn = Path.GetFileNameWithoutExtension(inputVideoInfo.Filename);
        var output = pattern.Replace("%o", fn)
            .Replace("%c", cropString)
            .Replace("%d", inputVideoInfo.CreatedDateTime.ToString("yyyy-MM-ddTHH-mm-ss"));
            
        return output;
        
    }

    public async Task<bool> ConvertVideosAsync(IList<VideoRenderQueueEntry> renderingQueue, bool renderAll)
    {
        if (_activeRenderingQueue != null)
            throw new Exception("Video rendering is already running");
        
        RenderingQueueProcessingStarted?.Invoke(this, EventArgs.Empty);
        var allSuccessful = true;
        var renderableStates = new []
        {
            VideoRenderingState.Queued,
            VideoRenderingState.CompletedWithErrors,
            VideoRenderingState.Canceled
        };
        
        if (renderAll)
        {
            foreach (var entry in renderingQueue)
                entry.ResetStatus();
        }
        
        _activeRenderingQueue = renderingQueue;
        foreach (var queueEntry in renderingQueue)
        {
            try
            {
                if (!renderableStates.Contains(queueEntry.RenderingState))
                    continue;
                
                if (queueEntry.CancellationTokenSource.IsCancellationRequested)
                {
                    queueEntry.RenderingState = VideoRenderingState.Canceled;
                    RenderingCanceled?.Invoke(this, queueEntry);
                    allSuccessful = false;
                    continue;
                }
                
                queueEntry.Progress = 0;
                queueEntry.RenderingState = VideoRenderingState.Rendering;
                await RenderVideo(queueEntry, queueEntry.CancellationTokenSource.Token);
                queueEntry.RenderingState = VideoRenderingState.CompletedSuccessfully;
                RenderingSucceeded?.Invoke(this, queueEntry);
            }
            catch (TaskCanceledException e)
            {
                queueEntry.RenderingState = VideoRenderingState.Canceled;
                queueEntry.Errors = new []{ "Rendering process was terminated while rendering was in progress" };
                RenderingCanceled?.Invoke(this, queueEntry);
                allSuccessful = false;
            }
            catch (Exception e)
            {
                queueEntry.RenderingState = VideoRenderingState.CompletedWithErrors;
                queueEntry.Errors = new []{ e.Message };
                RenderingFailed?.Invoke(this, queueEntry);
                allSuccessful = false;
            }
        }

        _activeRenderingQueue = null;
        RenderingQueueProcessingFinished?.Invoke(this, EventArgs.Empty);
        return allSuccessful;
    }

    public void SignalCancellation()
    {
        if (_activeRenderingQueue == null)
            return;
        
        _activeRenderingQueue.ToList().ForEach(entry => entry.CancellationTokenSource.Cancel());
    }

    public void SignalCancellation(VideoRenderQueueEntry renderQueueEntry)
    {
        renderQueueEntry.CancellationTokenSource.Cancel();
    }

    private async Task RenderVideo(VideoRenderQueueEntry entry, CancellationToken cancellationToken)
    {
        var video = entry.Video;
        var inputVideoInfo = video.InputVideoInfo;
        var rotation = video.FrameRotation;
        var pathsConfig = _configManager.GetConfig<PathsConfig>()!;
        var conversionConfig = _configManager.GetConfig<ConversionConfig>()!;

        var avFilterString = _avFilterFactory.BuildAvFilter(new AvFilterFrameSelectCondition(), rotation);

        var outputVideoFullFilename = GetFilenameFromPattern(video, conversionConfig.OutputFilenamePattern) + ".mp4";
        if (conversionConfig.OutputBesideOriginals)
            outputVideoFullFilename = Path.Combine(Path.GetDirectoryName(video.InputVideoInfo.Filename)!, outputVideoFullFilename);
        else
            outputVideoFullFilename = Path.Combine(conversionConfig.OutputDirectory, outputVideoFullFilename);

        if (outputVideoFullFilename == video.InputVideoInfo.Filename)
            throw new Exception("Output video filename is the same as the input video filename");
        
        if (File.Exists(outputVideoFullFilename))
            throw new Exception("Output video file already exists");
        
        if (string.IsNullOrEmpty(conversionConfig.CodecVideo))
            throw new Exception("Video codec is not set");
        
        if (string.IsNullOrEmpty(conversionConfig.CodecAudio) && conversionConfig.OutputAudio)
            throw new Exception("Audio codec is not set");

        var startTime = video.TimelineCrop.StartTimeSeconds != null
            ? TimeSpan.FromSeconds(Math.Round((double)video.TimelineCrop.StartTimeSeconds, 2))
            : TimeSpan.Zero;
        var endTime = video.TimelineCrop.EndTimeSeconds != null
            ? TimeSpan.FromSeconds(Math.Round((double)video.TimelineCrop.EndTimeSeconds, 2))
            : TimeSpan.Zero;
        var duration = endTime - startTime;
        
        var argsList = new List<string>
        {
            "-loglevel", "8",
            "-y",
            "-progress", "pipe:1",
            "-stats_period", "0.25",
            "-filter_complex", avFilterString,
            "-ss", startTime.ToString("c"),
            "-to", endTime.ToString("c"), 
            "-i", inputVideoInfo.Filename,
            "-c:v", conversionConfig.CodecVideo,
            "-c:a", conversionConfig.CodecAudio,
            "-map", "[OUTPUT_FRAME]"
        };
        if (conversionConfig.OutputAudio)
            argsList.AddRange("-map", "0:a:0");
        
        argsList.Add(outputVideoFullFilename);
        
        var processStartInfo = new ProcessStartInfo(pathsConfig.Ffmpeg, argsList)
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
            process!.OutputDataReceived += (sender, args) =>
            {
                Console.WriteLine(args.Data);
                if (!string.IsNullOrEmpty(args.Data) && Regex.IsMatch(args.Data, @"^out_time=\d"))
                {
                    var frameTime = TimeSpan.Parse(args.Data.Split("=")[1], CultureInfo.InvariantCulture);
                    var progress = frameTime.TotalMilliseconds / duration.TotalMilliseconds;
                    entry.Progress = Math.Min(Math.Round(progress * 100.0, 2), 99.0);
                }
                if (!string.IsNullOrEmpty(args.Data) && Regex.IsMatch(args.Data, @"^progress=end"))
                {
                    entry.Progress = 99.0;
                }
            };
            await process!.WaitForExitAsync(cancellationToken);
            if (process.ExitCode == 0 && File.Exists(outputVideoFullFilename))
            {
                var taggingProcessStartInfo = new ProcessStartInfo(pathsConfig.Exiftool,
                    [
                        "-api", "LargeFileSupport=1",
                        "-overwrite_original",
                        "-XMP-GSpherical:Spherical=true",
                        "-XMP-GSpherical:Stitched=true",
                        "-XMP-GSpherical:StitchingSoftware=MAXVideoConvert",
                        "-XMP-GSpherical:ProjectionType=equirectangular",
                        outputVideoFullFilename
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
                entry.Progress = 100.0;
                if (taggingProcess.ExitCode == 0)
                {
                    return;
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