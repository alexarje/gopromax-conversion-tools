using Microsoft.Extensions.DependencyInjection;
using VideoConversionApp.Abstractions;
using VideoConversionApp.Models;
using VideoConversionApp.Services;
using VideoConversionApp.ViewModels;

namespace VideoConversionApp;

public static class ServiceCollectionExtensions
{
    public static void AddCommonServices(this IServiceCollection collection)
    {
        
        collection.AddSingleton<IStorageDialogProvider, StorageDialogProvider>();
        
        collection.AddSingleton<IVideoInfoService, VideoInfoService>();
        collection.AddSingleton<IVideoPreviewService, VideoPreviewService>();
        collection.AddSingleton<IAppConfigService, AppConfigService>();
        collection.AddSingleton<IVideoPoolManager, VideoPoolManager>();
        collection.AddSingleton<IAvFilterFactory, AvFilterFactory>();
        collection.AddSingleton<IVideoConverterService, VideoConverterService>();
        
        collection.AddSingleton<PreviewVideoPlayerState>();
        
        collection.AddSingleton<MainWindowViewModel>();
        collection.AddSingleton<MediaSelectionViewModel>();
        collection.AddSingleton<ConversionPreviewViewModel>();
        collection.AddSingleton<RenderSettingsViewModel>();
        collection.AddSingleton<RenderQueueViewModel>();
    }
}