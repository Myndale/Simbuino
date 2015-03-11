
using Simbuino.Emulator;
using System;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
namespace Simbuino.UI.Lcd
{
	/// <summary>
	/// Interaction logic for LcdControl.xaml
	/// </summary>
	public partial class LcdControl : UserControl
	{
		//BitmapSource Bitmap;

		public static readonly DependencyProperty PortDProperty = DependencyProperty.RegisterAttached(
			"PortD", typeof(int),
			typeof(LcdControl), new FrameworkPropertyMetadata(0, OnPortDChanged));

		public static void SetPortD(UIElement element, int value)
		{
			element.SetValue(PortDProperty, value);
		}

		public static int GetPortD(UIElement element)
		{
			return (int)element.GetValue(PortDProperty);
		}

		private static void OnPortDChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var control = d as LcdControl;
			if (control == null)
				return;
			control.PD3 = (GetPortD(control) & 0x08) != 0;
		}

		public bool PD3
		{
			get { return (bool)GetValue(PD3Property); }
			set { SetValue(PD3Property, value); }
		}

		// Using a DependencyProperty as the backing store for IsDirty.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty PD3Property =
			DependencyProperty.Register("PD3", typeof(bool), typeof(UserControl), new UIPropertyMetadata(false));

		public byte[] Pixels
		{
			get { return (byte[])GetValue(PixelsProperty); }
			set { SetValue(PixelsProperty, value); }
		}

		// Using a DependencyProperty as the backing store for Pixels.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty PixelsProperty =
			DependencyProperty.Register("Pixels", typeof(byte[]), typeof(LcdControl), new PropertyMetadata(null));

		public System.Windows.Media.Color LcdForeground
		{
			get { return (System.Windows.Media.Color)GetValue(LcdForegroundProperty); }
			set { SetValue(LcdForegroundProperty, value); }
		}

		// Using a DependencyProperty as the backing store for LcdForeground.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty LcdForegroundProperty =
			DependencyProperty.Register("LcdForeground", typeof(System.Windows.Media.Color), typeof(LcdControl),
			new PropertyMetadata(System.Windows.Media.Color.FromArgb(0xff, 0x40, 0x40, 0x40), OnForegroundChanged));

		public double LcdAngle
		{
			get { return (double)GetValue(LcdAngleProperty); }
			set { SetValue(LcdAngleProperty, value); }
		}

		// Using a DependencyProperty as the backing store for LcdAngle.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty LcdAngleProperty =
			DependencyProperty.Register("LcdAngle", typeof(double), typeof(LcdControl),
			new PropertyMetadata(0.0));

		public bool ImageReady
		{
			get { return (bool)GetValue(ImageReadyProperty); }
			set { SetValue(ImageReadyProperty, value); }
		}

		// Using a DependencyProperty as the backing store for ImageReady.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty ImageReadyProperty =
			DependencyProperty.Register("ImageReady", typeof(bool), typeof(LcdControl), new PropertyMetadata(false));

		public LcdDevice Device
		{
			get { return (LcdDevice)GetValue(DeviceProperty); }
			set { SetValue(DeviceProperty, value); }
		}

		// Using a DependencyProperty as the backing store for Device.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty DeviceProperty =
			DependencyProperty.Register("Device", typeof(LcdDevice), typeof(LcdControl), new PropertyMetadata(null, OnDeviceChanged));

		private WriteableBitmap theBitmap;
		private Int32Rect DestRect = new Int32Rect(0, 0, LcdDevice.Width, LcdDevice.Height);
		private bool NeedRefresh = true;

		public LcdControl()
		{
			InitializeComponent();

			CreateImageSource();
			CompositionTarget.Rendering += CompositionTarget_Rendering;
		}

		private static void OnForegroundChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var control = d as LcdControl;
			if (control != null)
				control.CreateImageSource();
		}

		private void CreateImageSource()
		{
			if (this.LcdForeground == null)
				return;
			var color = this.LcdForeground;
			var palette = new BitmapPalette(Enumerable.Range(0, 256)
				.Select(i => System.Windows.Media.Color.FromArgb((byte)i, color.R, color.G, color.B)).ToArray());
			this.theBitmap = new WriteableBitmap(LcdDevice.Width, LcdDevice.Height, 96, 96, PixelFormats.Indexed8, palette);
			this.theImage.Source = this.theBitmap;
		}

		private static void OnDeviceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var control = d as LcdControl;
			if (control == null)
				return;
			control.UnsubscribeFromImageChange(e.OldValue as LcdDevice);
			control.SubscribeToImageChange(e.NewValue as LcdDevice);
		}

		void SubscribeToImageChange(LcdDevice device)
		{
			if (device != null)
				device.ImageChanged += OnImageChanged;
		}

		void UnsubscribeFromImageChange(LcdDevice device)
		{
			if (device != null)
				device.ImageChanged += OnImageChanged;
		}

		void OnImageChanged(object sender, EventArgs e)
		{
			this.NeedRefresh = true;
		}

		void CompositionTarget_Rendering(object sender, EventArgs e)
		{			
			if (!NeedRefresh)
				return;
			if (this.theBitmap == null)
				return;
			if (this.Device == null)
				return;
			if (this.Device.Pixels == null)
				return;
			this.theBitmap.WritePixels(this.DestRect, this.Device.Pixels, LcdDevice.Width, 0);
		}

	}

}
