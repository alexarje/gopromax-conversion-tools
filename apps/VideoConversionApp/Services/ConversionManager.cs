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
    private class ConvertibleVideoModel : IConvertibleVideoModel
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

        public ConvertibleVideoModel(IMediaInfo mediaInfo)
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
    
    public event EventHandler<IConvertibleVideoModel?>? PreviewedVideoChanged;
    
    private List<ConvertibleVideoModel> _convertibleVideoModels = new ();
    public IReadOnlyList<IConvertibleVideoModel> ConversionCandidates => _convertibleVideoModels;
    private IConvertibleVideoModel? _previewedVideo;

    public ConversionManager()
    {
    }


    public IConvertibleVideoModel AddVideoToPool(IMediaInfo mediaInfo)
    {
        var model = new ConvertibleVideoModel(mediaInfo);
        _convertibleVideoModels.Add(model);
        return model;
    }
    
    public void RemoveVideoFromPool(IConvertibleVideoModel video)
    {
        if (video is not ConvertibleVideoModel v)
            throw new ArgumentException("Type mismatch");
        
        _convertibleVideoModels.Remove(v);
        v.RemoveListeners();
    }

    public IConvertibleVideoModel? GetPreviewedVideo() => _previewedVideo;
    
    public void SetPreviewedVideo(IConvertibleVideoModel? video)
    {
        if (_previewedVideo != video)
        {
            _previewedVideo = video;
            PreviewedVideoChanged?.Invoke(this, video);
        }
    }

    
}