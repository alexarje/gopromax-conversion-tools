using System.Threading.Tasks;
using VideoConversionApp.Models;

namespace VideoConversionApp.Abstractions;

public interface IAppSettingsService
{
    Task LoadSettingsAsync();
    AppSettings GetSettings();
    // TODO segment and implement GetSettings<T>();
}