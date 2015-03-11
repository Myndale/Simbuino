using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace Simbuino.UI.Capture
{
	public class CaptureFrame
	{
		public Color Backlight { get; set; }
		public byte[] Pixels { get; set; }
		public double Angle { get; set; }
	}
}
