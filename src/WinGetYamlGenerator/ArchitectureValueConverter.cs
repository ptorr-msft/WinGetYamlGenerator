using System;

using Windows.UI.Xaml.Data;

namespace WinGetYamlGenerator
{
    // Converts enum value into index for combobox
    //
    // x86 --> 0
    // x64 --> 1
    // ARM --> 2
    // ARM64 --> 3
    // Neutral --> 4

    public class ArchitectureValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return (int)value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return (ArchitectureKind)(int)value;
        }
    }
}
