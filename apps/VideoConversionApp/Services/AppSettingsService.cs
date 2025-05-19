using System.Threading.Tasks;
using VideoConversionApp.Abstractions;
using VideoConversionApp.Models;

namespace VideoConversionApp.Services;

public class AppSettingsService : IAppSettingsService
{
    public async Task LoadSettingsAsync()
    {
    }

    public AppSettings GetSettings()
    {
        // TODO
        return new AppSettings();
    }
}