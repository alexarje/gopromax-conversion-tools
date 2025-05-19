using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using VideoConversionApp.Abstractions;
using VideoConversionApp.Views;

namespace VideoConversionApp.Services;

public class StorageDialogProvider : IStorageDialogProvider
{
    private Window? _providerWindow = null;

    public StorageDialogProvider()
    {
    }

    public void UseProviderWindow(Window window)
    {
        _providerWindow = window;
    }

    public IStorageProvider GetStorageProvider()
    {
        return TopLevel.GetTopLevel(_providerWindow)!.StorageProvider;
    }
}