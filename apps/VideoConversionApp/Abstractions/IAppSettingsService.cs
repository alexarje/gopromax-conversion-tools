using System.Threading.Tasks;
using VideoConversionApp.Models;

namespace VideoConversionApp.Abstractions;

public interface IAppSettingsService
{
    void LoadSettings();
    AppConfig GetSettings();
    void SaveSettings();
}