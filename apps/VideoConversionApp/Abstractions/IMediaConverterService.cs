using System.Collections.Generic;
using VideoConversionApp.Models;

namespace VideoConversionApp.Abstractions;

/// <summary>
/// The service that performs GoPro Max .360 video to equirectangular video conversion.
/// </summary>
public interface IMediaConverterService
{
    IReadOnlyList<CodecEntry> GetAvailableVideoCodecs();
    IReadOnlyList<CodecEntry> GetAvailableAudioCodecs();
}