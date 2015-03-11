using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace Simbuino.UI.Converters
{
	public class EqualityConverter : IMultiValueConverter
	{
		public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			if (values.Count() != 2)
				return null;
			if (values[0] == null)
				return values[1] == null;
			return values[0].Equals(values[1]);
		}

		public object[] ConvertBack(object values, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
