using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using MediaColor = System.Windows.Media.Color;

namespace LocalMusicPlayer.Views.Converters;

/// <summary>Maps (line index, current playback line index) to Spotify-style font size tiers.</summary>
public sealed class MainLyricFontSizeConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        var dist = MainLyricTierHelper.Distance(values);
        return dist switch
        {
            0 => 36.0,
            1 => 24.0,
            2 => 19.0,
            _ => 15.0
        };
    }

    public object[]? ConvertBack(object? value, Type[] targetTypes, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public sealed class MainLyricForegroundConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        var dist = MainLyricTierHelper.Distance(values);
        // Current line: orange; others: blue (matches main UI accents).
        var color = dist switch
        {
            0 => MediaColor.FromRgb(0xCE, 0x91, 0x78),
            1 => MediaColor.FromRgb(0x9C, 0xDC, 0xFE),
            2 => MediaColor.FromRgb(0x7A, 0xB0, 0xD8),
            _ => MediaColor.FromRgb(0x5C, 0x8A, 0xB0)
        };
        var brush = new SolidColorBrush(color);
        brush.Freeze();
        return brush;
    }

    public object[]? ConvertBack(object? value, Type[] targetTypes, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public sealed class MainLyricFontWeightConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        var dist = MainLyricTierHelper.Distance(values);
        return dist == 0 ? FontWeights.SemiBold : FontWeights.Normal;
    }

    public object[]? ConvertBack(object? value, Type[] targetTypes, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

internal static class MainLyricTierHelper
{
    public static int Distance(object[] values)
    {
        if (values.Length < 2)
            return 4;

        if (values[0] == DependencyProperty.UnsetValue || values[1] == DependencyProperty.UnsetValue)
            return 4;

        if (values[0] is not int lineIdx || values[1] is not int currentIdx)
            return 4;

        if (currentIdx < 0)
            return 4;

        return Math.Abs(lineIdx - currentIdx);
    }
}
