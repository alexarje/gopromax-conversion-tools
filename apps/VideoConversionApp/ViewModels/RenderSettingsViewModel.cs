using System.ComponentModel;
using Avalonia.Controls.Primitives;
using CommunityToolkit.Mvvm.ComponentModel;

namespace VideoConversionApp.ViewModels;

public partial class RenderSettingsViewModel : ViewModelBase
{
    public TabStripItem SelectedOutputDirectoryMethod
    {
        get => field;
        set
        {
            SetProperty(ref field, value);
            IsOutputToSelectedDirSelected = value.Name == "OutputToSelectedDir";
        }
    }
    
    public TabStripItem SelectedVideoCodecTab
    {
        get => field;
        set
        {
            SetProperty(ref field, value);
            IsOtherVideoCodecSelected = value.Name == "VcOther";
        }
    }
    
    public TabStripItem SelectedAudioCodecTab
    {
        get => field;
        set
        {
            SetProperty(ref field, value);
            IsOtherAudioCodecSelected = value.Name == "AcOther";
        }
    }
    
    [ObservableProperty]
    public partial bool IsOutputToSelectedDirSelected { get; set; }

    public bool IsOtherVideoCodecSelected
    {
        get => field;
        set
        {
            SetProperty(ref field, value);
        }
    }
    
    public bool IsOtherAudioCodecSelected
    {
        get => field;
        set
        {
            SetProperty(ref field, value);
        }
    }

    public RenderSettingsViewModel()
    {
        
    }

}