using Simbuino.Emulator;
using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Simbuino.UI.Converters
{
	// not being used anymore
	public class LcdImageConverter : IValueConverter
	{
		static uint[] pixels = new uint[LcdDevice.Width * LcdDevice.Height];

		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			var src = value as byte[];
			if (src == null)
				return null;
			int width = LcdDevice.Width;
			int height = LcdDevice.Height;			
			for (int y = 0, i = 0; y < height; y++)
				for (int x = 0; x < width; x++, i++)
				{
					uint col = src[y * LcdDevice.Width + x];
					uint rgb = 0x40 * col / 255;
					uint a = 0xff * col / 255;
					pixels[i] = (a << 24) | (rgb << 16) | (rgb << 8) | rgb;
				}
			BitmapSource source = BitmapSource.Create(width, height, 96, 96,  PixelFormats.Pbgra32, null, pixels, width*4);
			return source;
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
