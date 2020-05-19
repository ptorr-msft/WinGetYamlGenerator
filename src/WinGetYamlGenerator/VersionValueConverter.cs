using System;

using Windows.UI.Xaml.Data;

namespace WinGetYamlGenerator
{
    public class VersionValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value == null)
            {
                return null;
            }

            return value.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            if (value == null)
            {
                return null;
            }

            if (!Version.TryParse((string)value, out var version))
            {
                if (!Version.TryParse($"{value}.0", out version))
                {
                    return new Version(0, 0);
                }
            }

            return version;
        }
    }
}
