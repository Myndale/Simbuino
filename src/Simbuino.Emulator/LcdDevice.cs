using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace Simbuino.Emulator
{
	// emulates the Nokia 5110 LCD device
	public class LcdDevice : INotifyPropertyChanged
	{
		public event PropertyChangedEventHandler PropertyChanged;

		public const int Width = 84;
		public const int Height = 48;
		private byte[] Target { get; set; }
		public byte[] Pixels { get; private set; }

		public event EventHandler ImageChanged;
		
		
		public long[] Integrated { get; private set; }
		public long[] LastIntegrated { get; private set; }
		public long[] Times { get; private set; }
		public long LastRefresh = 0;

		PortRegister SCE_PORT = AtmelContext.C; // A1
		const int SCE_BIT = 1;
		PortRegister RESET_PORT = AtmelContext.C; // A0
		const int RESET_BIT = 0;
		PortRegister DC_PORT = AtmelContext.C; // A2
		const int DC_BIT = 2;		

		int CurrentX = 0;
		int CurrentY = 0;
		bool ExtendedMode = false;

		private bool _Persistence = false;
		public bool Persistence
		{
			get { return this._Persistence; }
			set
			{
				if (this._Persistence != value)
				{
					this._Persistence = value;
					OnPropertyChanged("Persistence");
				}
			}
		}

		private int _PersistenceAmount = 765;
		public int PersistenceAmount
		{
			get { return this._PersistenceAmount; }
			set
			{
				if (this._PersistenceAmount != value)
				{
					this._PersistenceAmount = value;
					OnPropertyChanged("PersistenceAmount");
				}
			}
		}

		private Color _LcdBackground = Color.FromRgb(0x8f, 0xa7, 0x9a);
		public Color LcdBackground
		{
			get
			{
				return _LcdBackground;
			}
			set { _LcdBackground = value; OnPropertyChanged("LcdBackground"); }
		}

		private Color _LcdForeground = Color.FromRgb(0x40, 0x40, 0x40);
		public Color LcdForeground
		{
			get { return _LcdForeground; }
			set { _LcdForeground = value; OnPropertyChanged("LcdForeground"); }
		}

		private Color _LcdBacklight = Color.FromRgb(0xce, 0xdd, 0xe7);
		public Color LcdBacklight
		{
			get { return _LcdBacklight; }
			set { _LcdBacklight = value; OnPropertyChanged("LcdBacklight"); }
		}

		private double _LcdAngle = 0.0;
		public double LcdAngle
		{
			get { return _LcdAngle; }
			set { _LcdAngle = value; OnPropertyChanged("LcdAngle"); }
		}

		private Color _LcdCurrentBacklight = Color.FromRgb(0xce, 0xdd, 0xe7);
		public Color LcdCurrentBacklight
		{
			get { return _LcdCurrentBacklight; }
			set { _LcdCurrentBacklight = value; OnPropertyChanged("LcdCurrentBacklight"); }
		}

		public LcdDevice(SPI spi)
		{
			Reset();
			RESET_PORT.WriteRegister.OnRegisterChanged += LcdDevice_OnResetChanged;
			spi.OnReceivedByte += spi_OnReceivedByte;

			AtmelContext.D.WriteRegister.OnRegisterChanged += WriteRegister_OnRegisterChanged;
		}

		void WriteRegister_OnRegisterChanged(int oldVal, int newVal)
		{
		}

		public void Reset()
		{
			this.Pixels = new byte[Width * Height];
			this.Target = new byte[Width * Height];
			this.Integrated = new long[Width * Height];
			this.LastIntegrated = new long[Width * Height];
			this.Times = new long[Width * Height];
			this.LastRefresh = 0;
			this.LcdCurrentBacklight = this.LcdBackground;
			Refresh(true);
		}

		void LcdDevice_OnResetChanged(int oldVal, int newVal)
		{
			var changed = oldVal ^ newVal;
			if ((changed & (1 << RESET_BIT)) == 0)
				return;
			if ((newVal & (1 << RESET_BIT)) == 0)
				return;
			this.CurrentX = 0;
			this.CurrentY = 0;
			this.ExtendedMode = false;
			for (int i = 0; i < Pixels.Length; i++)
				Target[i] = 0;
		}

		public void SetPixel(int x, int y, int color)
		{
			var ofs = y * Width + x;
			this.Integrated[ofs] += this.Target[ofs] * (AtmelContext.Clock - this.Times[ofs]);
			this.Times[ofs] = AtmelContext.Clock;
			if (color == 0)
				this.Target[y * Width + x] = 0;
			else
				this.Target[y * Width + x] = 255;
		}

		public void Refresh(bool force)
		{
			var elapsed = AtmelContext.Clock - this.LastRefresh;
			if (!force && (elapsed < AtmelProcessor.ClockSpeed / 30))
				return;
			if (elapsed == 0)
				return;

			if (this.Persistence)
			{
				for (int ofs = 0; ofs < Pixels.Length; ofs++)
				{
					this.Integrated[ofs] += this.Target[ofs] * (AtmelContext.Clock - this.Times[ofs]);
					this.Times[ofs] = AtmelContext.Clock;
				}

				for (int ofs = 0; ofs < Pixels.Length; ofs++)
				{
					if (elapsed != 0)
						this.Integrated[ofs] /= elapsed;
					var average = ((int)this.Integrated[ofs] + (int)this.LastIntegrated[ofs]) / 2;
					this.Pixels[ofs] =
						(average < 64) ? (byte)0 :
						(average > 190) ? (byte)255 :
						(byte)128;
				}

				for (int ofs = 0; ofs < Pixels.Length; ofs++)
				{
					this.LastIntegrated[ofs] = this.Integrated[ofs];
					this.Integrated[ofs] = 0;
				}
			}
			else
			{
				for (int ofs = 0; ofs < Pixels.Length; ofs++)
					this.Pixels[ofs] = this.Target[ofs];
			}
			CalculateBacklight();

			this.LastRefresh = AtmelContext.Clock;
			if (this.ImageChanged != null)
				this.ImageChanged(this, null);
		}

		private void CalculateBacklight()
		{
			int level = 0;
			var TCCR0A = AtmelContext.RAM[AtmelIO.TCCR0A].Value;
			if ((TCCR0A & (1 << AtmelIO.COM0B1)) != 0)
			{
				// pwm
				level = AtmelContext.RAM[AtmelIO.OCR0B].Value;
			}
			else
			{
				// digital
				if ((AtmelContext.D.WriteRegister.Value & 0x20) != 0)
					level = 255;
				else
					level = 0;
			}

			var r = this.LcdBackground.R + level * (this.LcdBacklight.R - this._LcdBackground.R) / 255;
			var g = this.LcdBackground.G + level * (this.LcdBacklight.G - this._LcdBackground.G) / 255;
			var b = this.LcdBackground.B + level * (this.LcdBacklight.B - this._LcdBackground.B) / 255;
			this.LcdCurrentBacklight = Color.FromRgb((byte)r, (byte)g, (byte)b);
		}

		protected void OnPropertyChanged(string name)
		{
			PropertyChangedEventHandler handler = PropertyChanged;
			if (handler != null)
			{
				handler(this, new PropertyChangedEventArgs(name));
			}
		}

		void spi_OnReceivedByte(int data)
		{
			// make sure the Lcd is currently enabled
			if ((SCE_PORT.WriteRegister.Value & (1 << SCE_BIT)) != 0)
				return;

			// what did we just receive?
			if ((DC_PORT.WriteRegister.Value & (1 << DC_BIT)) != 0)
			{
				// D/C is set to data, store this byte in display memory and advance the ptr
				for (int i = 0; i < 8; i++)
					SetPixel(this.CurrentX, this.CurrentY * 8 + i, (data >> i) & 1);
				this.CurrentX++;
				if (this.CurrentX >= Width)
				{
					this.CurrentX = 0;
					this.CurrentY++;
					if (this.CurrentY >= Height / 8)
					{
						// sent the last byte to the screen, force an update
						this.CurrentY = 0;
						this.Refresh(true);
					}
				}
			}
			else if (data == 0x00)
			{
				// nop
			}
			else if ((data & 0xf8) == 0x20)
			{
				// function set
				this.ExtendedMode = (data & 1) != 0;
			}
			else if (!this.ExtendedMode)
			{
				// H == 0
				if ((data & 0xf8) == 0x40)
					this.CurrentY = Math.Min(data & 7, Height - 1);
				else if ((data & 0x80) == 0x80)
					this.CurrentX = Math.Min(data & 0x7f, Width - 1);
			}
			else
			{
				// H == 1
			}
		}

	}
	
}
