using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using VideoConversionApp.Abstractions;
using VideoConversionApp.Models;

namespace VideoConversionApp.Services;

public class VideoPoolManager : IVideoPoolManager
{
    /// <summary>
    /// Represents a video that is entered into VideoPoolManager and also managed by it.
    /// Hence, the class itself is hidden and the model is exposed just by its interface.
    /// Instances are created by VideoPoolManager.
    /// </summary>
    private class ConvertableVideo : IConvertableVideo
    {
        public event EventHandler<AvFilterFrameRotation>? FrameRotationUpdated;
        public event EventHandler<TimelineCrop>? TimelineCropUpdated;
        public event EventHandler<bool>? IsEnabledForConversionUpdated;
        public event EventHandler? SettingsChanged;

        public IInputVideoInfo InputVideoInfo { get; private set; }
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
                var endCropped = TimelineCrop.EndTimeSeconds != InputVideoInfo.DurationInSeconds && TimelineCrop.EndTimeSeconds != null;

                return rotationChanged || startCropped || endCropped;
            }
        }

        public ConvertableVideo(IInputVideoInfo inputVideoInfo)
        {
            InputVideoInfo = inputVideoInfo;
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

    private class PlaceholderInputVideoInfo : IInputVideoInfo
    {
        public string Filename { get; } = IInputVideoInfo.PlaceHolderFilename;
        public bool IsValidVideo { get; } = false;
        public bool IsGoProMaxFormat { get; } = false;
        public decimal DurationInSeconds { get; } = 0;
        public DateTime CreatedDateTime { get; } = DateTime.MinValue;
        public long SizeBytes { get; } = 0;
        public string[]? ValidationIssues { get; } = null;
    }

    private class PreviewDummyInputVideoInfo : IInputVideoInfo
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
    
    private List<ConvertableVideo> _convertibleVideoModels = new ();
    public IReadOnlyList<IConvertableVideo> VideoPool => _convertibleVideoModels;
    
    // Placeholder video, representing "no video" in views and such.
    private readonly ConvertableVideo _placeholderVideo;
    // Dummy video, for filename previews and such.
    private readonly ConvertableVideo _dummyVideo;
    
    public VideoPoolManager(IAppConfigService appConfigService)
    {
        _placeholderVideo = new ConvertableVideo(new PlaceholderInputVideoInfo());
        _dummyVideo = new ConvertableVideo(new PreviewDummyInputVideoInfo())
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

    public IConvertableVideo AddVideoToPool(IInputVideoInfo inputVideoInfo)
    {
        var model = new ConvertableVideo(inputVideoInfo);
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

}