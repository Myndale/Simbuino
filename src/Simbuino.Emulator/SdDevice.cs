using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Simbuino.Emulator
{
	// basic SD card emulation class
	public class SdDevice : INotifyPropertyChanged
	{
		public event PropertyChangedEventHandler PropertyChanged;

		PortRegister CS_PORT = AtmelContext.B; // PB2
		const int CS_BIT = 2;

		private int Index = 0;
		private int[] Received = new int[6];
		private SPI SPI;
		private int Status = 0;
		private byte[] Buffer = new byte[550];
		private int BytesToSend = 0;
		private int SendIndex = 0;
		private bool AppCommand = false;

		public string ImgFile = "";

		public SdDevice(SPI spi)
		{
			this.SPI = spi;
			spi.OnReceivedByte += spi_OnReceivedByte;
		}

		public void Reset()
		{
			this.Status = 0;
			this.Index = 0;
			this.BytesToSend = 0;
			this.SendIndex = 0;
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
			// make sure the SD card is currently enabled
			if ((CS_PORT.WriteRegister[CS_BIT]) != 0)
				return;

			if (this.BytesToSend > 0)
			{
				this.BytesToSend--;
				SPI.ReceiveByte(this.Buffer[this.SendIndex++]);
				return;
			}

			this.Received[this.Index++] = data;
			if (!this.AppCommand)
			{
				switch (this.Received[0])
				{
					// CMD0 - RESET
					case 0x40:
						if (this.Index == 6)
						{
							this.Status = 0xff;
							this.Index = 0;
							this.SendIndex = 0;
							this.BytesToSend = 0;
							this.Buffer[this.BytesToSend++] = 0x01;
						}
						break;

					// CMD8 - SEND_IF_COND
					case 0x40 + 8:
						if (this.Index == 6)
						{
							this.Status = 0xff;
							this.Index = 0;
							this.SendIndex = 0;
							this.BytesToSend = 0;
							this.Buffer[this.BytesToSend++] = 0x04;	// invalid
							/*
							this.Buffer[this.BytesToSend++] = 0x00;	// R1
							this.Buffer[this.BytesToSend++] = 0xaa;
							this.Buffer[this.BytesToSend++] = 0xaa;
							this.Buffer[this.BytesToSend++] = 0xaa;
							this.Buffer[this.BytesToSend++] = 0xaa;
							 * */
						}
						break;

					// CMD16 - SET_BLOCKLEN
					case 0x40 + 16:
						if (this.Index == 6)
						{
							this.Status = 0x01;
							this.Index = 0;
							this.SendIndex = 0;
							this.BytesToSend = 0;
							this.Buffer[this.BytesToSend++] = 0x00;
						}
						break;

					// CMD17 - READ_SINGLE_BLOCK
					case 0x40 + 17:
						if (this.Index == 6)
						{
							this.Status = 0x01;
							this.Index = 0;
							this.SendIndex = 0;
							this.BytesToSend = 0;
							this.Buffer[this.BytesToSend++] = 0x00;
							this.Buffer[this.BytesToSend++] = 0xFE;
							try
							{
								using (var file = File.Open(this.ImgFile, FileMode.Open))
								{
									var offset = (this.Received[1] << 24) + (this.Received[2] << 16) + (this.Received[3] << 8) + this.Received[4];
									file.Seek(offset, SeekOrigin.Begin);
									file.Read(this.Buffer, this.BytesToSend, 512);									
								}
							}
							catch
							{
								Array.Clear(this.Buffer, this.BytesToSend, 512);
							}
							this.BytesToSend += 512;
							this.Buffer[this.BytesToSend++] = 0x00;
							this.Buffer[this.BytesToSend++] = 0x00;
						}
						break;

					// CMD23 - Number of blocks
					case 0x40 + 23:
						if (this.Index == 6)
						{
							this.Status = 0x01;
							this.Index = 0;
						}
						break;

					// CMD55 - ACMD
					case 0x40 + 55:
						if (this.Index == 6)
						{
							this.Status = 0xff;
							this.Index = 0;
							this.SendIndex = 0;
							this.BytesToSend = 0;
							this.Buffer[this.BytesToSend++] = 0;
							this.AppCommand = true;
						}
						break;

					// CMD58 - READ_OCR
					case 0x40 + 58:
						if (this.Index == 6)
						{
							this.Status = 0xff;
							this.Index = 0;
							this.SendIndex = 0;
							this.BytesToSend = 0;
							this.Buffer[this.BytesToSend++] = 0;
							this.AppCommand = true;
						}
						break;

					case 0xff:
						this.Index = 0;
						this.Status = 0xff;
						break;

					default:
						if (this.Index == 6)
						{
							// unknown command
							this.Status = 0x04;
							this.Index = 0;
						}
						break;
				}
			}
			else
			{
				switch (this.Received[0])
				{
					// SD_SEND_OP_COND
					case 0x40 + 41:
						if (this.Index == 6)
						{
							this.Status = 0xff;
							this.Index = 0;
							this.SendIndex = 0;
							this.BytesToSend = 0;
							this.Buffer[this.BytesToSend++] = 0x00;
							this.AppCommand = false;
						}
						break;

					case 0xff:
						this.Index = 0;
						this.Status = 0xff;
						break;

					default:
						if (this.Index == 6)
						{
							// unknown acmd
							this.Status = 0x04;
							this.Index = 0;
							this.AppCommand = false;
						}
						break;
				}
			}

			SPI.ReceiveByte(this.Status);
		}
	}
}
