using System;

using Windows.UI.Xaml.Data;

namespace WinGetYamlGenerator
{
    public class UriValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value == null)
            {
                return value;
            }

            return (value as Uri).AbsoluteUri;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            if (value == null || string.IsNullOrWhiteSpace(value as string))
            {
                return null;
            }

            if (!Uri.TryCreate(value as string, UriKind.Absolute, out var result))
            {
                if (!Uri.TryCreate($"http://{value}", UriKind.Absolute, out result))
                {
                    return new Uri("https://www.example.com/");
                }
            }

            return result;
        }
    }
}
