using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using VideoConversionApp.Abstractions;
using VideoConversionApp.Models;

namespace VideoConversionApp.Services;

public class VideoConverterService : IVideoConverterService
{
    private readonly IAppConfigService _appConfigService;
    private List<CodecEntry>? _ffmpegEncodingVideoCodecs;
    private List<CodecEntry>? _ffmpegEncodingAudioCodecs;
    
    public VideoConverterService(IAppConfigService appConfigService)
    {
        _appConfigService = appConfigService;
    }

    private void PopulateValidCodecs()
    {
        var audioCodecs = new List<CodecEntry>();
        var videoCodecs = new List<CodecEntry>();
        
        var appSettings = _appConfigService.GetConfig();
        var codecParseRegex = new Regex(
            @"^\s(?<decode>[D\.])(?<encode>[E\.])(?<type>[VASDT\.])([I\.])(?<lossy>[L\.])(?<lossless>[S\.])\s(?<name>\w*)\s*(?<desc>.*)$");
        
        var processStartInfo = new ProcessStartInfo(appSettings.Paths.Ffmpeg,
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
}