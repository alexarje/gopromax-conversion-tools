using System.Threading.Tasks;
using VideoConversionApp.Models;

namespace VideoConversionApp.Abstractions;

public interface IAppConfigService
{
    void LoadConfig();
    AppConfig GetConfig();
    void SaveConfig();
}