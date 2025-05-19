using VideoConversionApp.Services;

namespace VideoConversionApp.Tests;

public class MediaInfoServiceTests
{
    [Fact]
    public async Task TestGetMediaInfo()
    {
        var mediaFile = "/drive/data/Media/VideoEditing/ProjectDirs/Snouk Season 24-25/GS010176.360";
        var service = new MediaInfoService();
        
        var info = await service.GetMediaInfoAsync(mediaFile);
        
    }
}