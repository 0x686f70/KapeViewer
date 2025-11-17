using System.Globalization;
using System.Windows.Data;

namespace KapeViewer.Converters
{
    public class UtcLocalTimeConverter : IValueConverter
    {
        public bool IsUtcMode { get; set; } = true;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DateTime dateTime)
            {
                if (IsUtcMode)
                {
                    return DateTime.SpecifyKind(dateTime, DateTimeKind.Utc).ToString("yyyy-MM-dd HH:mm:ss.fff");
                }
                else
                {
                    return dateTime.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss.fff");
                }
            }

            return value?.ToString() ?? string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
