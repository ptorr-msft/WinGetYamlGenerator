using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Windows.UI.Xaml.Data;

namespace WinGetYamlGenerator
{
    // Converts enum value into index for combobox
    //
    // EXE--> 0
    // MSI --> 1
    // MSIX --> 2

    public class InstallerKindValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return (int)value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return (InstallerKind)(int)value;
        }
    }
}
