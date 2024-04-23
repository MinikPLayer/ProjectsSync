using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace ProjectsSyncAv.Converters
{
    internal class BooleanToBrushConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            Color positiveColor = Colors.Green;
            Color negativeColor = Colors.Red;
            Color nullColor = Colors.Gray;

            if(parameter is string s)
            {
                var splitted = s.Split(';');
                if(splitted.Length > 0)
                    positiveColor = Color.Parse(splitted[0]);
                
                if(splitted.Length > 1)
                    negativeColor = Color.Parse(splitted[1]);

                if(splitted.Length > 2)
                    nullColor = Color.Parse(splitted[2]);
            }

            if (value == null)
                return new SolidColorBrush(nullColor);

            if (value is not bool boolVal)
                throw new NotSupportedException("Invalid type");

            Color color = boolVal ? positiveColor : negativeColor;
            return new SolidColorBrush(color);
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
