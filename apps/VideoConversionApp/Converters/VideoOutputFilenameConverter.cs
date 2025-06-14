using System;
using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;
using VideoConversionApp.Abstractions;

namespace VideoConversionApp.Converters;

public class VideoOutputFilenameConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null)
            return false;

        if (value is IConvertableVideo video && parameter is IConversionManager conversionManager)
        {
            return conversionManager.GetFilenameFromPattern(video.MediaInfo, video.TimelineCrop, "%o-%c"); // TODO
        }

        return "";

    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return BindingOperations.DoNothing;
    }
}