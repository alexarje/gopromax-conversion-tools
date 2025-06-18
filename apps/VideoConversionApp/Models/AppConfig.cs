using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using VideoConversionApp.Abstractions;

#pragma warning disable CS0618 // Type or member is obsolete

namespace VideoConversionApp.Models;

/// <summary>
/// Event args for the event "Configuration has changed".
/// </summary>
public class ConfigChangedEventArgs
{
    /// <summary>
    /// Name of the property whose value was changed, e.g. "NumberOfSnapshotFrames"
    /// </summary>
    public string PropertyName { get; set; }
    /// <summary>
    /// Path of the property, elements separated by dots, e.g. "Previews.NumberOfSnapshotFrames"
    /// </summary>
    public string PropertyPath { get; set; }
    /// <summary>
    /// The property's new value.
    /// </summary>
    public object? NewValue { get; set; }
}

/// <summary>
/// Base class for config elements.
/// The config is YAML, containing objects. Each object is a "config element".
/// </summary>
/// <param name="parent">The parent, root config</param>
/// <param name="yamlModel">The linked YAML data model</param>
/// <param name="path">Path of this config element</param>
/// <typeparam name="T">YAML data model type</typeparam>
public abstract class ConfigElementBase<T>(AppConfig parent, T yamlModel, string path)
{
    protected T YamlModel = yamlModel;
    protected AppConfig Parent = parent;
    protected string Path { get; set; } = path;

    protected void SetAndRaise<TProperty>(ref TProperty field, TProperty value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<TProperty>.Default.Equals(field, value))
        {
            return;
        }
        field = value;
        RaiseEvent(propertyName, value);
    }
    
    protected void RaiseEvent(string propertyName, object newValue)
    {
        Parent.RaisePropertyChanged(new ConfigChangedEventArgs()
        {
            PropertyName = propertyName,
            PropertyPath = $"{Path}{(Path != "" ? "." : "")}{propertyName}",
            NewValue = newValue
        });
    }
}

/// <summary>
/// Application configuration.
///
/// This class provides eventing and any complex logic for the config.
/// Attach event handlers to <see cref="PropertyChanged"/> to get notified when a
/// config value changes.
///
/// This class consumes a YAML data model and applies changes to it.
/// The YAML model is then to be serialized when it needs to be saved. It can be retrieved with
/// <see cref="GetYamlModel"/>.
/// </summary>
public class AppConfig : ConfigElementBase<AppConfigYamlModel>, IAppConfigModel
{
    /// <summary>
    /// Subscribe to this to get events whenever a config value changes.
    /// </summary>
    public event EventHandler<ConfigChangedEventArgs>? PropertyChanged;

    public AppConfig(AppConfigYamlModel yamlModel)
        : base(null!, yamlModel, "")
    {
        Parent = this;
        Paths = new ConfigPaths(this, (ConfigPathsYamlModel)yamlModel.Paths, nameof(Paths));
        Previews = new ConfigPreviews(this, (ConfigPreviewsYamlModel)yamlModel.Previews, nameof(Previews));
        Conversion = new ConfigConversion(this, (ConfigConversionYamlModel)yamlModel.Conversion, nameof(Conversion));
    }
    
    /// <summary>
    /// Returns the YAML data model that can be serialized to text.
    /// </summary>
    /// <returns></returns>
    public AppConfigYamlModel GetYamlModel() => YamlModel;

    [Obsolete("Do not use outside configuration child classes")]
    public void RaisePropertyChanged(ConfigChangedEventArgs eventArgs)
    {
        PropertyChanged?.Invoke(this, eventArgs);
    }
    
    public IConfigPaths Paths
    {
        get => field;
        private set => SetAndRaise(ref field, value);
    }
    
    public IConfigPreviews Previews
    {
        get => field;
        private set => SetAndRaise(ref field, value);
    }
    
    public IConfigConversion Conversion
    {
        get => field;
        private set => SetAndRaise(ref field, value);
    }
}

/// <summary>
/// AppConfig.Paths property.
/// </summary>
public class ConfigPaths : ConfigElementBase<ConfigPathsYamlModel>, IConfigPaths
{
    public ConfigPaths(AppConfig parent, ConfigPathsYamlModel yamlModel, string path)
        : base(parent, yamlModel, path)
    {
    }
    
    public string Exiftool 
    { 
        get => YamlModel.Exiftool;
        set
        {
            if (value == YamlModel.Exiftool) return;
            YamlModel.Exiftool = value;
            RaiseEvent(nameof(Exiftool), value);
        }
    }
    
    public string Ffmpeg
    { 
        get => YamlModel.Ffmpeg;
        set
        {
            if (value == YamlModel.Ffmpeg) return;
            YamlModel.Ffmpeg = value;
            RaiseEvent(nameof(Ffmpeg), value);
        }
    }
    
    public string Ffprobe
    { 
        get => YamlModel.Ffprobe;
        set
        {
            if (value == YamlModel.Ffprobe) return;
            YamlModel.Ffprobe = value;
            RaiseEvent(nameof(Ffprobe), value);
        }
    }
}

/// <summary>
/// AppConfig.Previews property.
/// </summary>
public class ConfigPreviews : ConfigElementBase<ConfigPreviewsYamlModel>, IConfigPreviews
{
    public ConfigPreviews(AppConfig parent, ConfigPreviewsYamlModel yamlModel, string path)
        : base(parent, yamlModel, path)
    {
    }

    public uint NumberOfSnapshotFrames
    { 
        get => YamlModel.NumberOfSnapshotFrames;
        set
        {
            if (value == YamlModel.NumberOfSnapshotFrames) return;
            YamlModel.NumberOfSnapshotFrames = value;
            RaiseEvent(nameof(NumberOfSnapshotFrames), value);
        }
    }

    public uint NumberOfThumbnailThreads
    { 
        get => YamlModel.NumberOfThumbnailThreads;
        set
        {
            if (value == YamlModel.NumberOfThumbnailThreads) return;
            YamlModel.NumberOfThumbnailThreads = value;
            RaiseEvent(nameof(NumberOfThumbnailThreads), value);
        }
    }
    
    public uint ThumbnailTimePositionPcnt
    { 
        get => YamlModel.ThumbnailTimePositionPcnt;
        set
        {
            if (value == YamlModel.ThumbnailTimePositionPcnt) return;
            YamlModel.ThumbnailTimePositionPcnt = value;
            RaiseEvent(nameof(ThumbnailTimePositionPcnt), value);
        }
    }
}

/// <summary>
/// AppConfig.Conversion property.
/// </summary>
public class ConfigConversion : ConfigElementBase<ConfigConversionYamlModel>, IConfigConversion
{
    public ConfigConversion(AppConfig parent, ConfigConversionYamlModel yamlModel, string path)
        : base(parent, yamlModel, path)
    {
    }
    
    public string CodecAudio
    { 
        get => YamlModel.CodecAudio;
        set
        {
            if (value == YamlModel.CodecAudio) return;
            YamlModel.CodecAudio = value;
            RaiseEvent(nameof(CodecAudio), value);
        }
    }
    
    public string CodecVideo
    { 
        get => YamlModel.CodecVideo;
        set
        {
            if (value == YamlModel.CodecVideo) return;
            YamlModel.CodecVideo = value;
            RaiseEvent(nameof(CodecVideo), value);
        }
    }

    public bool OutputAudio
    { 
        get => YamlModel.OutputAudio;
        set
        {
            if (value == YamlModel.OutputAudio) return;
            YamlModel.OutputAudio = value;
            RaiseEvent(nameof(OutputAudio), value);
        }
    }
    
    public bool OutputBesideOriginals
    { 
        get => YamlModel.OutputBesideOriginals;
        set
        {
            if (value == YamlModel.OutputBesideOriginals) return;
            YamlModel.OutputBesideOriginals = value;
            RaiseEvent(nameof(OutputBesideOriginals), value);
        }
    }
    
    public string OutputDirectory
    { 
        get => YamlModel.OutputDirectory;
        set
        {
            if (value == YamlModel.OutputDirectory) return;
            YamlModel.OutputDirectory = value;
            RaiseEvent(nameof(OutputDirectory), value);
        }
    }
    
    public string OutputFilenamePattern
    { 
        get => YamlModel.OutputFilenamePattern;
        set
        {
            if (value == YamlModel.OutputFilenamePattern) return;
            YamlModel.OutputFilenamePattern = value;
            RaiseEvent(nameof(OutputFilenamePattern), value);
        }
    }


}

