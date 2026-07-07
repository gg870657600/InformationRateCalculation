using Avalonia.Data.Converters;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InformationRateCalculation
{
    //使用方式：<NumericUpDown  Value="{Binding #IP_Pack.Value,Converter={StaticResource Math},ConverterParameter='-10'}"
    public class MathConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // 支持多种数值类型，包括 NumericUpDown 的 decimal
            double numericValue = value switch
            {
                decimal dec => (double)dec,
                double d => d,
                float f => f,
                int i => i,
                long l => l,
                _ => 0.0
            };

            string expr = parameter?.ToString() ?? "0";

            if (expr.StartsWith("+")) return numericValue + ParseNumber(expr[1..]);
            if (expr.StartsWith("-")) return numericValue - ParseNumber(expr[1..]);
            if (expr.StartsWith("*")) return numericValue * ParseNumber(expr[1..]);
            if (expr.StartsWith("/")) return numericValue / ParseNumber(expr[1..]);

            return numericValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // 同样的逻辑用于反向转换
            double numericValue = value switch
            {
                decimal dec => (double)dec,
                double d => d,
                float f => f,
                int i => i,
                long l => l,
                _ => 0.0
            };

            string expr = parameter?.ToString() ?? "0";

            if (expr.StartsWith("+")) return numericValue - ParseNumber(expr[1..]);
            if (expr.StartsWith("-")) return numericValue + ParseNumber(expr[1..]);
            if (expr.StartsWith("*")) return numericValue / ParseNumber(expr[1..]);
            if (expr.StartsWith("/")) return numericValue * ParseNumber(expr[1..]);

            return numericValue;
        }

        private static double ParseNumber(string str)
        {
            return double.TryParse(str, out var result) ? result : 0.0;
        }
    }
}
