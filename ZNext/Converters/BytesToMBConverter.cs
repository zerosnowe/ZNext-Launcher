using Microsoft.UI.Xaml.Data;
using System;

namespace ZNext.Converters
{
    public class BytesToMBConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is long bytes)
            {
                return (bytes / 1024.0 / 1024.0).ToString("F2");
            }
            return "0.00";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
