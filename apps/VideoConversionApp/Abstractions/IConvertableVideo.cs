using System;
using VideoConversionApp.Models;

namespace VideoConversionApp.Abstractions;

public interface IConvertableVideo
{
    public IInputVideoInfo InputVideoInfo { get; }
    public AvFilterFrameRotation FrameRotation { get; set; }
    public TimelineCrop TimelineCrop { get; set; }
    public bool IsEnabledForConversion { get; set; }
    public bool HasNonDefaultSettings { get; }
    
    // TODO Maybe just add the rest of the required stuff to perform the conversion here?
    
    // public RenderQueueInfo RenderQueueInfo { get; set; } // contains progress, success, canceled...
    // also maybe put IsEnabledForConversion there?
    
    
    // public OutputVideoInfo OutputVideoInfo { get; set; } // Put the output filename here at least
    
    // Drop ConversionManager's ConversionSettings and shove everything to AppConfig?

    public event EventHandler<AvFilterFrameRotation> FrameRotationUpdated;
    public event EventHandler<TimelineCrop> TimelineCropUpdated;
    public event EventHandler<bool> IsEnabledForConversionUpdated;
    public event EventHandler SettingsChanged;
}