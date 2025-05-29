using System;
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
    private AppSettings _appSettings;
    private string _singleFrameAvFilter = string.Empty;

    public MediaPreviewService(IAppSettingsService appSettingsService)
    {
        _appSettings = appSettingsService.GetSettings();
    }

    private async Task UseSingleFrameAvFilter()
    {
        if (_singleFrameAvFilter == string.Empty)
        {
            await using var resourceStream = AssetLoader.Open(
                new Uri("avares://VideoConversionApp/Resources/360-to-equirect.avfilter"));
            using var reader = new StreamReader(resourceStream);
            _singleFrameAvFilter = await reader.ReadToEndAsync();
        }
    }
    
    public async Task<byte[]?> GenerateThumbnailAsync(MediaInfo mediaInfo)
    {
        await UseSingleFrameAvFilter();
        var tmpThumbFilePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".jpg");
        
        var thumbTimePosition = _appSettings.ThumbnailAtPosition / 100.0 * mediaInfo.DurationSeconds;
        var processStartInfo = new ProcessStartInfo(_appSettings.FfmpegPath,
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

    public void QueueThumbnailGeneration(MediaInfo mediaInfo, Action<Bitmap> callback)
    {
    }

    public async Task<IList<byte[]>> GenerateSnapshotFramesAsync(MediaInfo mediaInfo, int numberOfFrames)
    {
        if (numberOfFrames < 2)
            throw new ArgumentException("Number of frames must be at least 2");
     
        await UseSingleFrameAvFilter();
        
        var skipLength = mediaInfo.DurationSeconds / (numberOfFrames - 1);
        var timePositions = new string[numberOfFrames]; 
        for (var i = 0; i < numberOfFrames; i++)
            timePositions[i] = TimeSpan.FromSeconds(Math.Min(i * skipLength, mediaInfo.DurationSeconds)).ToString(@"hh\:mm\:ss");
        
        // TODO do in parallel

        var frames = new List<byte[]>();
        for (var i = 0; i < numberOfFrames; i++)
        {
            var tmpFrameFilePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".jpg");
        
            var frameTimePosition = timePositions[i];
            var processStartInfo = new ProcessStartInfo(_appSettings.FfmpegPath,
            [
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

            try
            {
                var process = Process.Start(processStartInfo);
                await process!.WaitForExitAsync();
                if (process.ExitCode == 0 && File.Exists(tmpFrameFilePath))
                {
                    var imgBytes = await File.ReadAllBytesAsync(tmpFrameFilePath);
                    File.Delete(tmpFrameFilePath);
                    frames.Add(imgBytes);
                }
            }
            catch (Exception e)
            {
                // TODO handle this
                Console.WriteLine("Error: " + e.Message);
            }

        }

        return frames;

    }
}