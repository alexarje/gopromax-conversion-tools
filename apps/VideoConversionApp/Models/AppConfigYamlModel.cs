using VideoConversionApp.Abstractions;
using YamlDotNet.Serialization;

namespace VideoConversionApp.Models;

/// <summary>
/// YAML data model of the configuration. Pure data representation.
/// </summary>
public class AppConfigYamlModel : IAppConfigModel
{
    [YamlMember(Alias = "paths", SerializeAs = typeof(ConfigPathsYamlModel))]
    public IConfigPaths Paths { get; set; } = new ConfigPathsYamlModel();
    
    [YamlMember(Alias = "previews", SerializeAs = typeof(ConfigPreviewsYamlModel))]
    public IConfigPreviews Previews { get; set; } = new ConfigPreviewsYamlModel();
    
    [YamlMember(Alias = "conversion", SerializeAs = typeof(ConfigConversionYamlModel))]
    public IConfigConversion Conversion { get; set; } = new ConfigConversionYamlModel();
}

/// <summary>
/// YAML data model of the configuration. Pure data representation.
/// </summary>
public class ConfigPathsYamlModel : IConfigPaths
{
    [YamlMember(Alias = "exiftool")]
    public string Exiftool { get; set; } = "exiftool";
    
    [YamlMember(Alias = "ffmpeg")]
    public string Ffmpeg { get; set; } = "ffmpeg";
    
    [YamlMember(Alias = "ffprobe")]
    public string Ffprobe { get; set; } = "ffprobe";
}

/// <summary>
/// YAML data model of the configuration. Pure data representation.
/// </summary>
public class ConfigPreviewsYamlModel : IConfigPreviews
{
    [YamlMember(Alias = "number_of_snapshot_frames")]
    public uint NumberOfSnapshotFrames { get; set; } = 10;

    [YamlMember(Alias = "number_of_thumbnail_threads")]
    public uint NumberOfThumbnailThreads { get; set; } = 3;
    
    [YamlMember(Alias = "thumbnail_time_position_pcnt")]
    public uint ThumbnailTimePositionPcnt { get; set; } = 50;
}

/// <summary>
/// YAML data model of the configuration. Pure data representation.
/// </summary>
public class ConfigConversionYamlModel : IConfigConversion
{
    [YamlMember(Alias = "codec_audio")]
    public string CodecAudio { get; set; } = "pcm_s16le";
    
    [YamlMember(Alias = "codec_video")]
    public string CodecVideo { get; set; } = "prores";

    [YamlMember(Alias = "output_audio")]
    public bool OutputAudio { get; set; } = true;
    
    [YamlMember(Alias = "output_beside_originals")]
    public bool OutputBesideOriginals { get; set; } = false;
    
    [YamlMember(Alias = "output_directory")]
    public string OutputDirectory { get; set; } = "/tmp";
    
    [YamlMember(Alias = "output_filename_pattern")]
    public string OutputFilenamePattern { get; set; } = "%o-%c";

}
