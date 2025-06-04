using System;

namespace VideoConversionApp.Models;

/// <summary>
/// Represents a single convertible 360 video, along with its conversion parameters.
/// </summary>
public class ConvertibleVideoModel
{
    public MediaInfo MediaInfo { get; private set; }
    public bool IsEnabledForConversion { get; set; } = false;
    
    public AvFilterFrameRotation FrameRotation { get; set; }
    public TimelineCrop TimelineCrop { get; set; }

    public event EventHandler<bool> OnConversionSettingsChanged;

    /// <summary>
    /// Returns true if this model's conversion settings have been modified.
    /// </summary>
    public bool HasModifiedSettings
    {
        get
        {
            var rotationChanged = FrameRotation.Pitch != 0 || FrameRotation.Yaw != 0 || FrameRotation.Roll != 0;
            var startCropped = TimelineCrop.StartTimeMilliseconds != 0 && TimelineCrop.StartTimeMilliseconds != null;
            var endCropped = TimelineCrop.EndTimeMilliseconds != MediaInfo.DurationMilliseconds && TimelineCrop.EndTimeMilliseconds != null;

            return rotationChanged || startCropped || endCropped;
        }
    }

    /// <summary>
    /// Call this after making changes to conversion settings in this model.
    /// This lets any interested parties know.
    /// </summary>
    public void NotifyConversionSettingsChanged()
    {
        OnConversionSettingsChanged?.Invoke(this, HasModifiedSettings);
    }
    
    
    public ConvertibleVideoModel(MediaInfo mediaInfo)
    {
        MediaInfo = mediaInfo;
        FrameRotation = AvFilterFrameRotation.Zero;
        TimelineCrop = new TimelineCrop();
    }
    
    
}