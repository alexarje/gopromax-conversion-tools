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
        
        collection.AddSingleton<IMediaInfoService, MediaInfoService>();
        collection.AddSingleton<IMediaPreviewService, MediaPreviewService>();
        collection.AddSingleton<IAppSettingsService, AppSettingsService>();
        collection.AddSingleton<IConversionManager, ConversionManager>();
        collection.AddSingleton<IAvFilterFactory, AvFilterFactory>();
        
        collection.AddSingleton<PreviewVideoPlayerState>();
        
        collection.AddSingleton<MainWindowViewModel>();
        collection.AddSingleton<MediaSelectionViewModel>();
        collection.AddSingleton<ConversionPreviewViewModel>();
        collection.AddSingleton<RenderSettingsViewModel>();
        collection.AddSingleton<RenderQueueViewModel>();
    }
}