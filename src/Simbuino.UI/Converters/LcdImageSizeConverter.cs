using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace Simbuino.UI.Converters
{
	public class LcdImageSizeConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			var parm = parameter.ToString();
			if (parm == "Width")
				return Math.Floor((double)value / 84);
			else if (parm == "Height")
				return Math.Floor((double)value / 48);
			else
				return 0.0;
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
