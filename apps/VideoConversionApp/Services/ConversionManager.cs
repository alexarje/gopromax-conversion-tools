using System;
using System.Collections.Generic;
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
        public string Filename { get; } = ":placeholder:";
        public bool IsValidVideo { get; } = false;
        public bool IsGoProMaxFormat { get; } = false;
        public decimal DurationInSeconds { get; } = 0;
        public DateTime CreatedDateTime { get; } = DateTime.MinValue;
        public long SizeBytes { get; } = 0;
        public string[]? ValidationIssues { get; } = null;
    }
    
    public event EventHandler<IConvertableVideo?>? PreviewedVideoChanged;
    
    private List<ConvertableVideo> _convertibleVideoModels = new ();
    public IReadOnlyList<IConvertableVideo> ConversionCandidates => _convertibleVideoModels;
    private IConvertableVideo? _previewedVideo;
    private readonly ConvertableVideo _placeholderVideo;

    public ConversionManager()
    {
        _placeholderVideo = new ConvertableVideo(new PlaceholderMediaInfo());
    }


    public IConvertableVideo GetPlaceholderVideo()
    {
        return _placeholderVideo;
    }

    public IConvertableVideo AddVideoToPool(IMediaInfo mediaInfo)
    {
        var model = new ConvertableVideo(mediaInfo);
        _convertibleVideoModels.Add(model);
        return model;
    }
    
    public void RemoveVideoFromPool(IConvertableVideo video)
    {
        if (video is not ConvertableVideo v)
            throw new ArgumentException("Type mismatch");
        
        _convertibleVideoModels.Remove(v);
        v.RemoveListeners();
    }

    public IConvertableVideo? GetPreviewedVideo() => _previewedVideo;
    
    public void SetPreviewedVideo(IConvertableVideo? video)
    {
        if (_previewedVideo != video)
        {
            _previewedVideo = video;
            PreviewedVideoChanged?.Invoke(this, video);
        }
    }

    
}