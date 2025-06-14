using System.Threading.Tasks;
using VideoConversionApp.Models;

namespace VideoConversionApp.Abstractions;

public interface IAppSettingsService
{
    Task LoadSettingsAsync();
    AppSettings GetSettings();
    // TODO segment and implement GetSettings<T>();
    // TODO when settings change, have events raised, notify do that for example ffmpeg-path reliant places
    // can get notifications.
}