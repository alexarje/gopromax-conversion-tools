using System.Threading.Tasks;
using VideoConversionApp.Models;

namespace VideoConversionApp.Abstractions;

// TODO: how about...
// IConfigurable, IConfigurable<T> and ConfigurableServiceBase<T>
// Split config into several places, let them register themselves here or use discovery to
// serialize and deserialize configs. And use JSON because YAML is just probably overcomplicating it
// and bringing another dependency in.
// That way configs are where they belong and do not have a direct dependency to here.
// Also, don't try to put output params like filename, dir etc. to IConvertableVideo,
// they are render queue-scoped, not per video, and should be settings in IVideoConverterService

// TODO also use IBitmapCache in IVideoPreviewService, cache thumbs and use them if they have been already generated. 

public interface IAppConfigService
{
    void LoadConfig();
    AppConfig GetConfig();
    void SaveConfig();
}