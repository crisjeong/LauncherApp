using System.Globalization;
using System.Windows.Data;

namespace LauncherApp.Converter;

public class StatusToActionConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        string status = value as string;
        return status == "Running" ? "Stop" : "Run";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
