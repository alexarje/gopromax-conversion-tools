using System;
using VideoConversionApp.Models;

namespace VideoConversionApp.Abstractions;

public interface IConvertibleVideoModel
{
    public IMediaInfo MediaInfo { get; }
    public AvFilterFrameRotation FrameRotation { get; set; }
    public TimelineCrop TimelineCrop { get; set; }
    public bool IsEnabledForConversion { get; set; }
    public bool HasNonDefaultSettings { get; }

    public event EventHandler<AvFilterFrameRotation> FrameRotationUpdated;
    public event EventHandler<TimelineCrop> TimelineCropUpdated;
    public event EventHandler<bool> IsEnabledForConversionUpdated;
    public event EventHandler SettingsChanged;
}