using Simbuino.UI.Main;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

/*
namespace Simbuino.UI.Converters
{
	public class MemoryConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			try
			{
				var bytesPerRow = 16;
				var src = (value as IEnumerable<DisplayRegister>).ToArray();
				var numRows = src.Count() / bytesPerRow;
				var rows = Enumerable.Range(0, numRows).Select(i => new MemoryRow(i * bytesPerRow, bytesPerRow, src)).ToArray();
				return rows;
			}
			catch
			{
				return null;
			}
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			throw new NotImplementedException();
		}

		internal class MemoryRow
		{
			public string DisplayText = "";

			public MemoryRow(int addr, int length, IList<DisplayRegister> regs)
			{
				var builder = new StringBuilder();
				builder.Append(String.Format("0x{0:x4}:", addr));
				for (int i = 0; i < length; i++)
				{
					regs[addr + i].PropertyChanged += MemoryRow_PropertyChanged;
					var val = regs[addr + i].Value;
					builder.Append(String.Format(" {0:x2}", val));
				}
				this.DisplayText = builder.ToString();
			}

			void MemoryRow_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
			{
				this
			}

			public override string ToString()
			{
				return this.DisplayText;
			}
		}
	}
	
}
*/
