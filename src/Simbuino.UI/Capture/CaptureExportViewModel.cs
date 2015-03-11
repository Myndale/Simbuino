using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using Gif.Components;
using MvvmDialogs.ViewModels;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Simbuino.UI.Capture
{
	public class CaptureExportViewModel : ViewModelBase, IUserDialogViewModel
	{
		private string _Message = "";
		public string Message
		{
			get { return _Message; }
			set { _Message = value; RaisePropertyChanged(() => this.Message); }
		}

		public int NumFrames
		{
			get { return (this.Frames==null) ? 0 : this.Frames.Count(); }
		}

		private int _CurrentFrame = 0;
		public int CurrentFrame
		{
			get { return _CurrentFrame; }
			set { _CurrentFrame = value; RaisePropertyChanged(() => this.CurrentFrame); }
		}


		CancellationTokenSource Cancel = new CancellationTokenSource();
		private string Filename;
		private double Angle;
		private List<CaptureFrame> Frames;
		private System.Windows.Media.Color LcdForeground;
		
		public CaptureExportViewModel(string filename, List<CaptureFrame> frames, double angle)
		{
			this.Filename = filename;
			this.Angle = angle;
			this.Frames = frames;
			this.Message = "Initializing...";
			this.Success = false;
		}

		public System.Windows.Media.Color GetColor(int i, CaptureFrame frame)
		{
			int r1 = frame.Backlight.R;
			int g1 = frame.Backlight.G;
			int b1 = frame.Backlight.B;
			int r2 = this.LcdForeground.R;
			int g2 = this.LcdForeground.G;
			int b2 = this.LcdForeground.B;
			int r = r1 + i * (r2 - r1) / 255;
			int g = g1 + i * (g2 - g1) / 255;
			int b = b1 + i * (b2 - b1) / 255;
			return System.Windows.Media.Color.FromArgb(0xff, (byte)r, (byte)g, (byte)b);
		}

		private static System.Drawing.Bitmap BitmapFromWriteableBitmap(WriteableBitmap writeBmp)
		{
			System.Drawing.Bitmap bmp;
			using (MemoryStream outStream = new MemoryStream())
			{
				BitmapEncoder enc = new BmpBitmapEncoder();
				enc.Frames.Add(BitmapFrame.Create((BitmapSource)writeBmp));
				enc.Save(outStream);
				bmp = new System.Drawing.Bitmap(outStream);
			}
			return bmp;
		}


		public bool IsModal
		{
			get { return true; }
		}

		public bool Success
		{
			get;
			private set;
		}

		public void RequestClose()
		{
			if (this.DialogClosing != null)
				this.DialogClosing(this, new EventArgs());
		}

		public event EventHandler DialogClosing;

		public ICommand LoadedCommand { get { return new RelayCommand(LoadedCommand_Execute); } }
		private async void LoadedCommand_Execute()
		{
			await ExportFullPalette(this.Filename);
			//await ExportSmallPalette(@"c:\temp\GamebuinoGames\small.gif");
			this.RequestClose();
		}

		// exports image the the full 256-color palette (even those only 3 of the entries are actually used)
		private async Task ExportFullPalette(string filename)
		{
			await Task.Run(() =>
			{
				try
				{
					if (File.Exists(filename))
						File.Delete(filename);

					AnimatedGifEncoder e = new AnimatedGifEncoder();
					e.Start(filename);
					e.SetDelay(1000 / 30);
					//-1:no repeat,0:always repeat
					e.SetRepeat(0);
					
					byte[] scaledPixels = new byte[168 * 96];
					for (int frameNum = 0; frameNum < this.Frames.Count(); frameNum++)
					{
						this.Cancel.Token.ThrowIfCancellationRequested();

						this.CurrentFrame = frameNum + 1;
						this.Message = String.Format("Exporting frame {0}/{1}...", this.CurrentFrame, this.Frames.Count());

						var frame = this.Frames[frameNum];
						var palette = new BitmapPalette(new System.Windows.Media.Color[] {
							GetColor(0, frame),
							GetColor(128, frame),
							GetColor(255, frame)
						});
						WriteableBitmap writeable = null;
						if (this.Angle == 0.0)
						{
							var destRect = new Int32Rect(0, 0, 168, 96);
							writeable = new WriteableBitmap(168, 96, 96, 96, PixelFormats.Indexed8, palette);
							for (int y = 0; y < 48; y++)
								for (int x = 0; x < 84; x++)
								{
									var pixel = frame.Pixels[y * 84 + x];
									scaledPixels[2 * y * 168 + 2 * x] =
									scaledPixels[(2 * y + 1) * 168 + 2 * x] =
									scaledPixels[2 * y * 168 + 2 * x + 1] =
									scaledPixels[(2 * y + 1) * 168 + 2 * x + 1] =
										(pixel < 64) ? (byte)0 :
										(pixel > 190) ? (byte)2 :
										(byte)1;
								}
							writeable.WritePixels(destRect, scaledPixels, 168, 0);
						}

						else if (this.Angle == -90.0)
						{
							var destRect = new Int32Rect(0, 0, 96, 168);
							writeable = new WriteableBitmap(96, 168, 96, 96, PixelFormats.Indexed8, palette);
							for (int y = 0; y < 84; y++)
								for (int x = 0; x < 48; x++)
								{
									var pixel = frame.Pixels[x * 84 + (83-y)];
									scaledPixels[2 * y * 96 + 2 * x] =
									scaledPixels[(2 * y + 1) * 96 + 2 * x] =
									scaledPixels[2 * y * 96 + 2 * x + 1] =
									scaledPixels[(2 * y + 1) * 96 + 2 * x + 1] =
										(pixel < 64) ? (byte)0 :
										(pixel > 190) ? (byte)2 :
										(byte)1;
								}
							writeable.WritePixels(destRect, scaledPixels, 96, 0);
						}

						else if (this.Angle == 90.0)
						{
							var destRect = new Int32Rect(0, 0, 96, 168);
							writeable = new WriteableBitmap(96, 168, 96, 96, PixelFormats.Indexed8, palette);
							for (int y = 0; y < 84; y++)
								for (int x = 0; x < 48; x++)
								{
									var pixel = frame.Pixels[(47 - x) * 84 + y];
									scaledPixels[2 * y * 96 + 2 * x] =
									scaledPixels[(2 * y + 1) * 96 + 2 * x] =
									scaledPixels[2 * y * 96 + 2 * x + 1] =
									scaledPixels[(2 * y + 1) * 96 + 2 * x + 1] =
										(pixel < 64) ? (byte)0 :
										(pixel > 190) ? (byte)2 :
										(byte)1;
								}
							writeable.WritePixels(destRect, scaledPixels, 96, 0);
						}

						else if (this.Angle == 180.0)
						{
							var destRect = new Int32Rect(0, 0, 168, 96);
							writeable = new WriteableBitmap(168, 96, 96, 96, PixelFormats.Indexed8, palette);
							for (int y = 0; y < 48; y++)
								for (int x = 0; x < 84; x++)
								{
									var pixel = frame.Pixels[(47-y) * 84 + (83-x)];
									scaledPixels[2 * y * 168 + 2 * x] =
									scaledPixels[(2 * y + 1) * 168 + 2 * x] =
									scaledPixels[2 * y * 168 + 2 * x + 1] =
									scaledPixels[(2 * y + 1) * 168 + 2 * x + 1] =
										(pixel < 64) ? (byte)0 :
										(pixel > 190) ? (byte)2 :
										(byte)1;
								}
							writeable.WritePixels(destRect, scaledPixels, 168, 0);
						}
						if (writeable != null)
						{							
							var bitmap = BitmapFromWriteableBitmap(writeable);
							e.AddFrame(bitmap);
						}
					}
					e.Finish();
					this.Success = true;
				}
				catch (OperationCanceledException)
				{
				}
				finally
				{
				}
			});
		}

		// exports image with a 3-color palette
		private async Task ExportSmallPalette(string filename)
		{
			await Task.Run(() =>
			{
				try
				{
					if (File.Exists(filename))
						File.Delete(filename);
					AnimatedGifEncoder e = new AnimatedGifEncoder();
					e.Start(filename);
					e.SetDelay(1000 / 30);
					//-1:no repeat,0:always repeat
					e.SetRepeat(0);

					var destRect = new Int32Rect(0, 0, 168, 96);
					byte[] packedPixels = new byte[168 * 96 / 4];
					for (int frameNum = 0; frameNum < this.Frames.Count(); frameNum++)
					{
						this.Cancel.Token.ThrowIfCancellationRequested();

						this.CurrentFrame = frameNum + 1;
						this.Message = String.Format("Exporting frame {0}/{1}...", this.CurrentFrame, this.Frames.Count());

						var frame = this.Frames[frameNum];
						var palette = new BitmapPalette(new System.Windows.Media.Color[] {
							GetColor(0, frame),
							GetColor(128, frame),
							GetColor(255, frame)
						});
						var writeable = new WriteableBitmap(168, 96, 96, 96, PixelFormats.Indexed2, palette);
						for (int y = 0; y < 48; y++)
							for (int x = 0; x < 84; x++)
							{
								int color = frame.Pixels[y * 84 + x];
								color = (color < 64) ? 0 :
									(color > 190) ? 2 :
									1;
								SetPackedPixel(packedPixels, 2 * x, 2 * y, color);
								SetPackedPixel(packedPixels, 2 * x + 1, 2 * y, color);
								SetPackedPixel(packedPixels, 2 * x, 2 * y + 1, color);
								SetPackedPixel(packedPixels, 2 * x + 1, 2 * y + 1, color);
							}
						writeable.WritePixels(destRect, packedPixels, 168/4, 0);
						var bitmap = BitmapFromWriteableBitmap(writeable);
						e.AddFrame(bitmap);
					}
					e.Finish();
					this.Success = true;
				}
				catch (OperationCanceledException)
				{
				}
				finally
				{
				}
			});
		}

		public ICommand CancelCommand { get { return new RelayCommand(CancelCommand_Execute); } }
		private void CancelCommand_Execute()
		{
			this.Cancel.Cancel();
		}

		private void SetPackedPixel(byte[] packedPixels, int x, int y, int color)
		{
			var pixelNum = y * 168 + x;
			int byteNum = pixelNum / 4;
			int ofs = 3-pixelNum % 4;
			color <<= (ofs * 2);
			var mask = ~(3 << (ofs * 2));
			packedPixels[byteNum] = (byte)(packedPixels[byteNum] & mask);
			packedPixels[byteNum] = (byte)(packedPixels[byteNum] | color);
		}

	}
}
