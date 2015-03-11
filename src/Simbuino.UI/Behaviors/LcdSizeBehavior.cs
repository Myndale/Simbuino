using Simbuino.Emulator;
using Simbuino.UI.Lcd;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interactivity;

namespace Simbuino.UI.Behaviors
{
	public class LcdSizeBehavior : Behavior<Image>
	{
		protected override void OnAttached()
		{
			var image = AssociatedObject as Image;
			(image.Parent as FrameworkElement).SizeChanged += LcdSizeBehavior_SizeChanged;
			base.OnAttached();
		}

		protected override void OnDetaching()
		{
			var image = AssociatedObject as Image;
			(image.Parent as FrameworkElement).SizeChanged -= LcdSizeBehavior_SizeChanged;
			base.OnDetaching();
		}

		void LcdSizeBehavior_SizeChanged(object sender, System.Windows.SizeChangedEventArgs e)
		{
			var xscale = Math.Floor(e.NewSize.Width / LcdDevice.Width);
			var yscale = Math.Floor(e.NewSize.Height / LcdDevice.Height);
			var scale = Math.Min(xscale, yscale);
			var image = AssociatedObject as Image;
			image.Width = scale * LcdDevice.Width;
			image.Height = scale * LcdDevice.Height;
		}

	}
}
