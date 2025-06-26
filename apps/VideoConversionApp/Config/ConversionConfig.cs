using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;
using VideoConversionApp.Abstractions;

namespace VideoConversionApp.Config;

[ObservableObject]
public partial class ConversionConfig : ConfigurationObject<ConversionConfig>
{
    public override string GetConfigurationKey() => "conversion";

    [ObservableProperty]
    public partial string CodecAudio { get; set; }
    [ObservableProperty]
    public partial string CodecVideo { get; set; }
    [ObservableProperty]
    public partial bool OutputAudio { get; set; }
    [ObservableProperty]
    public partial bool OutputBesideOriginals { get; set; }
    [ObservableProperty]
    public partial string OutputDirectory { get; set; }
    [ObservableProperty]
    public partial string OutputFilenamePattern { get; set; }

    public ConversionConfig()
    {
        CodecAudio = "pcm_s16le";
        CodecVideo = "prores";
        OutputAudio = true;
        OutputBesideOriginals = true;
        OutputDirectory = "";
        OutputFilenamePattern = "%o-%c";
    }
    
    protected override void InitializeFrom(ConversionConfig? configuration)
    {
        if (configuration is null)
            return;
        
        CodecAudio = configuration.CodecAudio;
        CodecVideo = configuration.CodecVideo;
        OutputAudio = configuration.OutputAudio;
        OutputBesideOriginals = configuration.OutputBesideOriginals;
        OutputDirectory = configuration.OutputDirectory;
        OutputFilenamePattern = configuration.OutputFilenamePattern;
    }
}