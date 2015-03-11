using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Simbuino.Hardware
{
	// this class and library are for communicating with the real Gamebuino hardware, it's currently used to flash new firmware via the bootloader
	public class Device : IDisposable
	{
		const byte STK_GET_PARAMETER = 0x41; // 'A'
		const byte STK_READ_SIGN = 0x75; // 'u'
		const byte STK_PROG_PAGE = 0x64; // 'd'
		const byte STK_READ_PAGE = 0x74; // 't'
		const byte STK_INSYNC = 0x14; // ' '
		const byte STK_LOAD_ADDRESS = 0x55; // 'U'
		const byte STK_OK = 0x10;
		const byte CRC_EOP = 0x20; // 'SPACE'

		private string _PortName = "";
		public string PortName
		{
			get { return _PortName; }
			set { _PortName = value; }
		}


		private SerialPort Port = null;

		public void Dispose()
		{
			ClosePort();
		}

		private void ClosePort()
		{
			if ((this.Port != null) && this.Port.IsOpen)
			{
				this.Port.Close();
				this.Port = null;
			}
		}

		public string[] AvailablePorts
		{
			get
			{
				return SerialPort.GetPortNames();
			}
		}

		public async Task Flash(int[] flash, int minAddr, int maxAddr, Action<int> progressCallback, CancellationTokenSource cancel)
		{
			Exception error = null;
			await Task.Run(async () =>
			{
				try
				{
					ClosePort();
					await OpenPort(true);

					// flash the pages
					int blockSize = 64;
					int firstPage = (minAddr / 2) / blockSize;
					int lastPage = (maxAddr / 2) / blockSize;
					for (int pageNum = firstPage; pageNum <= lastPage; pageNum++)
					{
						cancel.Token.ThrowIfCancellationRequested();
						progressCallback(100 * (pageNum - firstPage + 1) / (lastPage - firstPage + 1));

						// set the address
						var addressLow = (byte)((pageNum * blockSize) & 0xff);
						var addressHigh = (byte)(((pageNum * blockSize) >> 8) & 0xff);
						this.Port.Write(new byte[] { STK_LOAD_ADDRESS, addressLow, addressHigh, CRC_EOP }, 0, 4);
						this.Port.BaseStream.Flush();
						if (this.Port.ReadByte() != STK_INSYNC)
							throw new Exception("Out of sync");
						if (this.Port.ReadByte() != STK_OK)
							throw new Exception("Out of sync");

						// write the page data
						var bytes = new List<byte>();
						bytes.Add(STK_PROG_PAGE);
						bytes.Add(0);
						bytes.Add((byte)(2 * blockSize)); // words -> bytes
						bytes.Add(0);
						var page = flash.Skip(pageNum * blockSize).Take(blockSize);
						foreach (int val in page)
						{
							bytes.Add((byte)(val & 0xff));
							bytes.Add((byte)((val >> 8) & 0xff));
						}
						bytes.Add(CRC_EOP);
						this.Port.Write(bytes.ToArray(), 0, bytes.Count());
						this.Port.BaseStream.Flush();
						if (this.Port.ReadByte() != STK_INSYNC)
							throw new Exception("Out of sync");
						if (this.Port.ReadByte() != STK_OK)
							throw new Exception("Out of sync");
					}
				}
				catch (OperationCanceledException)
				{
				}
				catch (Exception ex)
				{
					error = ex;
				}
			});

			if (error != null)
				throw error;
		}

		public async Task LoadUserSettings()
		{
			try
			{
				await OpenPort();

				// flash the pages

				int blockSize = 64;
				int addr = ((32 - 4) * 1024 - 128) / 2;
				var addressLow = (byte)(addr & 0xff);
				var addressHigh = (byte)((addr >> 8) & 0xff);
				this.Port.Write(new byte[] { STK_LOAD_ADDRESS, addressLow, addressHigh, CRC_EOP }, 0, 4);
				this.Port.BaseStream.Flush();
				if (this.Port.ReadByte() != STK_INSYNC)
					throw new Exception("Out of sync");
				if (this.Port.ReadByte() != STK_OK)
					throw new Exception("Out of sync");

				// request a page read
				var bytes = new List<byte>();
				bytes.Add(STK_READ_PAGE);
				bytes.Add(0);
				bytes.Add((byte)(2 * blockSize)); // words -> bytes
				bytes.Add(0);
				bytes.Add(CRC_EOP);
				this.Port.Write(bytes.ToArray(), 0, bytes.Count());
				this.Port.BaseStream.Flush();
				if (this.Port.ReadByte() != STK_INSYNC)
					throw new Exception("Out of sync");
				var response = new byte[blockSize * 2];
				this.Port.Read(response, 0, response.Length);
				if (this.Port.ReadByte() != STK_OK)
					throw new Exception("Out of sync");
				var chars = response.Select(b => (char)b).ToArray();
				Console.WriteLine("Done!");
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
			}
		}

		private async Task OpenPort(bool bootloader = false)
		{
			if ((this.Port != null) && Port.IsOpen)
				return;

			this.Port = new SerialPort(this.PortName);
			this.Port.BaudRate = 115200;
			this.Port.DtrEnable = true;
			this.Port.RtsEnable = true;
			this.Port.ReadTimeout = 1000;
			this.Port.Open();
			await Task.Delay(bootloader ? 500 : 2000);
			if (!bootloader)
			{
				string response = this.Port.ReadLine().Trim();
			}
		}

		public async Task CreateFile(string filename)
		{
			await Task.Run(async () =>
			{
				await OpenPort();
				this.Port.WriteLine(String.Format("create {0}", filename));
				var response = this.Port.ReadLine().Trim();
				if (response != "Ok")
					throw new Exception(response);
			});
		}

		public async Task WriteFile(string filename, byte[] bytes, int pos, int length)
		{
			await Task.Run(async () =>
			{
				await OpenPort();
				this.Port.Write(String.Format("write {0} ", filename));
				this.Port.Write(bytes, pos, length);
				var response = this.Port.ReadLine().Trim();
				if (response != "Ok")
					throw new Exception(response);
			});
		}

		public async Task Delete(string filename)
		{
			await Task.Run(async () =>
			{
				await OpenPort();
				this.Port.WriteLine(String.Format("del {0}", filename));
				var response = this.Port.ReadLine().Trim();
				if (response != "Ok")
					throw new Exception(response);
			});
		}
	}
}
