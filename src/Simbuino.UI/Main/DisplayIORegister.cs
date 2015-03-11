using Simbuino.Emulator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Simbuino.UI.Main
{
	public class DisplayIORegister : DisplayRegister
	{
		private int Index;

		public string DisplayString
		{
			get
			{
				var name = ParameterHandler.PortNames[ParameterHandler.PortNames.Length - this.Index - 1] ?? "Reserved";
				return (this.Index < 0x40)
					? String.Format("0x{0:x2} (0x{1:x2}) - {2}:", this.Index, this.Index + 0x20, name.PadRight(10))
					: String.Format("     (0x{0:x2}) - {1}:", this.Index + 0x20, name.PadRight(10));
			}
		}

		public DisplayIORegister(int index)
		{
			this.Index = index;
		}

	}
}
