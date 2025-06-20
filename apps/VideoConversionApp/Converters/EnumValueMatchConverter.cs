using System;
using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;

namespace VideoConversionApp.Converters;

public class EnumValueMatchConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null || parameter == null)
            return false;

        if (value.GetType() == parameter.GetType() && Enum.Equals(value, parameter))
            return true;
        
        return false;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return BindingOperations.DoNothing;
    }
}