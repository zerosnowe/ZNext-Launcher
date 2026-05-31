using Microsoft.UI.Xaml.Data;
using System;

namespace ZNext.Converters
{
    public class RegionCodeToTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var raw = value?.ToString()?.Trim();
            if (string.IsNullOrWhiteSpace(raw))
            {
                return "未知地区";
            }

            return raw.ToLowerInvariant() switch
            {
                "cn" => "中国大陆",
                "cnos" => "港澳台地区",
                "oversea" => "海外",
                _ => raw
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
