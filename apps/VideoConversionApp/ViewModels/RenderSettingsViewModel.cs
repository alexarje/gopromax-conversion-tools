using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using VideoConversionApp.Abstractions;
using VideoConversionApp.Models;

namespace VideoConversionApp.ViewModels;

public partial class RenderSettingsViewModel : ViewModelBase
{
    private readonly IConversionManager _conversionManager;
    private readonly IMediaConverterService _converterService;

    [ObservableProperty]
    public partial string SelectedOutputDirectoryMethod { get; set; }
    [ObservableProperty]
    public partial string SelectedVideoCodecTab { get; set; }
    [ObservableProperty]
    public partial string SelectedAudioCodecTab { get; set; }
    [ObservableProperty]
    public partial bool IsOutputToSelectedDirSelected { get; set; } = true;
    [ObservableProperty]
    public partial bool IsOtherVideoCodecSelected { get; set; }
    [ObservableProperty]
    public partial bool IsOtherAudioCodecSelected { get; set; }
    [ObservableProperty]
    public partial string CustomVideoCodecName { get; set; }
    [ObservableProperty]
    public partial string CustomAudioCodecName { get; set; }
    [ObservableProperty]
    public partial string FilenamePattern { get; set; }
    [ObservableProperty]
    public partial string FilenamePreview { get; set; }
    [ObservableProperty]
    public partial ObservableCollection<CodecEntry>? AudioCodecs { get; set; }
    [ObservableProperty]
    public partial ObservableCollection<CodecEntry>? VideoCodecs { get; set; }

    public RenderSettingsViewModel(IConversionManager conversionManager,
        IMediaConverterService converterService)
    {
        _conversionManager = conversionManager;
        _converterService = converterService;
    }

    partial void OnSelectedOutputDirectoryMethodChanged(string value)
    {
        IsOutputToSelectedDirSelected = value == "OutputToSelectedDir";
        _conversionManager.GetConversionSettings().OutputBesideOriginals = !IsOutputToSelectedDirSelected;
    }

    partial void OnSelectedVideoCodecTabChanged(string value)
    {
        IsOtherVideoCodecSelected = value == "VcOther";
        if (IsOtherVideoCodecSelected && VideoCodecs == null)
            RefreshCodecLists();
        
        _conversionManager.GetConversionSettings().VideoCodecinFfmpeg = value switch
        {
            "VcProres" => "prores",
            "VcDnxhd" => "dnxhd",
            "VcCineform" => "cfhd",
            _ => CustomVideoCodecName
        };
    }

    partial void OnSelectedAudioCodecTabChanged(string value)
    {
        IsOtherAudioCodecSelected = value == "AcOther";
        if (IsOtherAudioCodecSelected && AudioCodecs == null)
            RefreshCodecLists();
        
        _conversionManager.GetConversionSettings().AudioCodecinFfmpeg = value switch
        {
            "AcPcms16le" => "prores",
            "AcPcms32le" => "dnxhd",
            "AcNone" => "",
            _ => CustomVideoCodecName
        };
        if (value == "AcNone")
            _conversionManager.GetConversionSettings().OutputAudio = false;
    }

    partial void OnCustomVideoCodecNameChanged(string value)
    {
        if (IsOtherVideoCodecSelected)
            _conversionManager.GetConversionSettings().VideoCodecinFfmpeg = value;
    }

    partial void OnCustomAudioCodecNameChanged(string value)
    {
        if (IsOtherAudioCodecSelected)
            _conversionManager.GetConversionSettings().AudioCodecinFfmpeg = value;
    }

    partial void OnFilenamePatternChanged(string value)
    {
        var dummyVideo = _conversionManager.GetDummyVideo();
        FilenamePreview = _conversionManager.GetFilenameFromPattern(
            dummyVideo.MediaInfo, dummyVideo.TimelineCrop, value);
        _conversionManager.GetConversionSettings().OutputFilenamePattern = value;
    }

    public void RefreshCodecLists()
    {
        VideoCodecs = new ObservableCollection<CodecEntry>(_converterService.GetAvailableVideoCodecs().Where(x => x.EncodingSupported));
        AudioCodecs = new ObservableCollection<CodecEntry>(_converterService.GetAvailableAudioCodecs().Where(x => x.EncodingSupported));
    }
}