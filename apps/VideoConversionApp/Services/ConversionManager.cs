using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using VideoConversionApp.Abstractions;
using VideoConversionApp.Models;

namespace VideoConversionApp.Services;

public class ConversionManager : IConversionManager
{
    /// <summary>
    /// Represents a video that is entered into ConversionManager and also managed by it.
    /// Hence, the class itself is hidden and the model is exposed just by its interface.
    /// Instances are created by ConversionManager.
    /// </summary>
    private class ConvertableVideo : IConvertableVideo
    {
        public event EventHandler<AvFilterFrameRotation>? FrameRotationUpdated;
        public event EventHandler<TimelineCrop>? TimelineCropUpdated;
        public event EventHandler<bool>? IsEnabledForConversionUpdated;
        public event EventHandler? SettingsChanged;

        public IMediaInfo MediaInfo { get; private set; }
        public AvFilterFrameRotation FrameRotation
        {
            get => field;
            set
            {
                if (value == field)
                    return;
                field = value;
                FrameRotationUpdated?.Invoke(this, value);
                SettingsChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public TimelineCrop TimelineCrop
        {
            get => field;
            set
            {
                if (value == field)
                    return;
                field = value;
                TimelineCropUpdated?.Invoke(this, value);
                SettingsChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public bool IsEnabledForConversion
        {
            get => field;
            set
            {
                if (value == field)
                    return;
                field = value;
                IsEnabledForConversionUpdated?.Invoke(this, value);
            }
        } = false;

        
        /// <summary>
        /// Returns true if this model's conversion settings have been modified.
        /// </summary>
        public bool HasNonDefaultSettings
        {
            get
            {
                var rotationChanged = FrameRotation.Pitch != 0 || FrameRotation.Yaw != 0 || FrameRotation.Roll != 0;
                var startCropped = TimelineCrop.StartTimeSeconds != 0 && TimelineCrop.StartTimeSeconds != null;
                var endCropped = TimelineCrop.EndTimeSeconds != MediaInfo.DurationInSeconds && TimelineCrop.EndTimeSeconds != null;

                return rotationChanged || startCropped || endCropped;
            }
        }

        public ConvertableVideo(IMediaInfo mediaInfo)
        {
            MediaInfo = mediaInfo;
            FrameRotation = AvFilterFrameRotation.Zero;
            TimelineCrop = new TimelineCrop();
        }

        public void RemoveListeners()
        {
            SettingsChanged = null;
            FrameRotationUpdated = null;
            TimelineCropUpdated = null;
            IsEnabledForConversionUpdated = null;
        }
    }

    private class PlaceholderMediaInfo : IMediaInfo
    {
        public string Filename { get; } = IMediaInfo.PlaceHolderFilename;
        public bool IsValidVideo { get; } = false;
        public bool IsGoProMaxFormat { get; } = false;
        public decimal DurationInSeconds { get; } = 0;
        public DateTime CreatedDateTime { get; } = DateTime.MinValue;
        public long SizeBytes { get; } = 0;
        public string[]? ValidationIssues { get; } = null;
    }

    private class PreviewDummyMediaInfo : IMediaInfo
    {
        public string Filename { get; } = "GS204012.360";
        public bool IsValidVideo { get; } = true;
        public bool IsGoProMaxFormat { get; } = true;
        public decimal DurationInSeconds { get; } = (decimal)143.528;
        public DateTime CreatedDateTime { get; } = DateTime.Now;
        public long SizeBytes { get; } = 200_000_000;
        public string[]? ValidationIssues { get; } = null;
    }


    public event EventHandler<IConvertableVideo>? VideoAddedToPool;
    public event EventHandler<IConvertableVideo>? VideoRemovedFromPool;
    
    private ConversionSettings? _conversionSettings;
    private List<ConvertableVideo> _convertibleVideoModels = new ();
    public IReadOnlyList<IConvertableVideo> ConversionCandidates => _convertibleVideoModels;
    
    // Placeholder video, representing "no video" in views and such.
    private readonly ConvertableVideo _placeholderVideo;
    // Dummy video, for filename previews and such.
    private readonly ConvertableVideo _dummyVideo;
    
    public ConversionManager(IAppSettingsService appSettingsService)
    {
        _placeholderVideo = new ConvertableVideo(new PlaceholderMediaInfo());
        _dummyVideo = new ConvertableVideo(new PreviewDummyMediaInfo())
        {
            FrameRotation = new AvFilterFrameRotation(),
            TimelineCrop = new TimelineCrop()
            {
                StartTimeSeconds = (decimal)12.5,
                EndTimeSeconds = 140
            }
        };
    }


    public IConvertableVideo GetPlaceholderVideo()
    {
        return _placeholderVideo;
    }

    public IConvertableVideo GetDummyVideo()
    {
        return _dummyVideo;
    }

    public IConvertableVideo AddVideoToPool(IMediaInfo mediaInfo)
    {
        var model = new ConvertableVideo(mediaInfo);
        _convertibleVideoModels.Add(model);
        VideoAddedToPool?.Invoke(this, model);
        return model;
    }
    
    public void RemoveVideoFromPool(IConvertableVideo video)
    {
        if (video is not ConvertableVideo v)
            throw new ArgumentException("Type mismatch");
        
        _convertibleVideoModels.Remove(v);
        v.RemoveListeners();
        VideoRemovedFromPool?.Invoke(this, v);
    }

    public ConversionSettings GetConversionSettings()
    {
        if (_conversionSettings == null)
        {
            // TODO load default conversion settings from the service...
            
            _conversionSettings = new ConversionSettings
            {
                OutputBesideOriginals = false,
                OutputDirectory = "/tmp",
                AudioCodecinFfmpeg = "pcm_s16le",
                OutputAudio = true,
                OutputFilenamePattern = "%o-%c-%d",
                VideoCodecinFfmpeg = "prores"
            };
        }
        return _conversionSettings;
    }

    public void SetConversionSettings(ConversionSettings settings)
    {
        _conversionSettings = settings;
        // TODO validate settings
    }

    public string GetFilenameFromPattern(IMediaInfo mediaInfo, TimelineCrop crop, string pattern)
    {
        var cropElems = new List<string>();
        if (crop.StartTimeSeconds != null && crop.StartTimeSeconds > 0)
        {
            var startTime = crop.StartTimeSeconds;
            var endTime = crop.EndTimeSeconds ?? mediaInfo.DurationInSeconds;
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

        var fn = Path.GetFileNameWithoutExtension(mediaInfo.Filename);
        var output = pattern.Replace("%o", fn)
            .Replace("%c", cropString)
            .Replace("%d", mediaInfo.CreatedDateTime.ToString("yyyy-MM-ddTHH-mm-ss"));
            
        return output;
        
    }
}