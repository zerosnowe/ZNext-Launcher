using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using System;

namespace ZNext.Converters
{
    public class LoadToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is int loadPercent)
            {
                if (loadPercent < 50)
                {
                    return new SolidColorBrush(Microsoft.UI.Colors.ForestGreen);
                }
                else if (loadPercent < 80)
                {
                    return new SolidColorBrush(Microsoft.UI.Colors.Gold);
                }
                else
                {
                    return new SolidColorBrush(Microsoft.UI.Colors.Red);
                }
            }
            return new SolidColorBrush(Microsoft.UI.Colors.ForestGreen);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
