using Microsoft.Extensions.DependencyInjection;
using VideoConversionApp.Abstractions;
using VideoConversionApp.Services;
using VideoConversionApp.ViewModels;

namespace VideoConversionApp;

public static class ServiceCollectionExtensions
{
    public static void AddCommonServices(this IServiceCollection collection)
    {
        collection.AddSingleton<IMediaInfoService, MediaInfoService>();
        collection.AddSingleton<IStorageDialogProvider, StorageDialogProvider>();
        collection.AddSingleton<IMediaPreviewService, MediaPreviewService>();
        collection.AddSingleton<IAppSettingsService, AppSettingsService>();
        collection.AddSingleton<IConversionManager, ConversionManager>();
        
        collection.AddSingleton<MainViewModel>();
        collection.AddSingleton<MediaSelectionViewModel>();
    }
}