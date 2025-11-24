using System;
using System.Globalization;
using System.Windows.Data;

namespace DesktopTaskAid.Converters
{
    /// <summary>
    /// Converts between TimeSpan and 24-hour time string (HH:mm format)
    /// </summary>
    public class TimeSpanToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is TimeSpan timeSpan)
            {
                return $"{timeSpan.Hours:D2}:{timeSpan.Minutes:D2}";
            }
            return "09:00";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string timeString && !string.IsNullOrWhiteSpace(timeString))
            {
                // Try to parse HH:mm format
                if (TimeSpan.TryParseExact(timeString, @"hh\:mm", CultureInfo.InvariantCulture, out TimeSpan result))
                {
                    return result;
                }
                // Try HH:mm format (24-hour)
                if (TimeSpan.TryParseExact(timeString, @"H\:mm", CultureInfo.InvariantCulture, out result))
                {
                    return result;
                }
                // Try H:mm format (single digit hour)
                if (TimeSpan.TryParseExact(timeString, @"H\:m", CultureInfo.InvariantCulture, out result))
                {
                    return result;
                }
                // Try parsing as general TimeSpan
                if (TimeSpan.TryParse(timeString, out result))
                {
                    return result;
                }
            }
            // Default to 9 AM if parsing fails
            return new TimeSpan(9, 0, 0);
        }
    }
}
