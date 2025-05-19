using System.Threading.Tasks;
using Avalonia.Platform.Storage;

namespace VideoConversionApp.Abstractions;

public interface IStorageDialogProvider
{
    IStorageProvider GetStorageProvider();
}