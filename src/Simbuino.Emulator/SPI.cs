using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Simbuino.Emulator
{
	// emulates the SPI functionality of the AVR chip, needed by the LCD and SD card devices (among other things)
	public class SPI
	{
		public delegate void ReceivedByteHandler(int data);
		public event ReceivedByteHandler OnReceivedByte;

		private static int SPDR_ReadBuffer;

		PortRegister SDIN_PORT = AtmelContext.B; // 11
		const int SDIN_BIT = 3;
		PortRegister SCLK_PORT = AtmelContext.B; // 13
		const int SCLK_BIT = 5;

		public static ObservableRegister SPCR;
		public static ObservableRegister SPSR;		
		public static ObservableRegister SPDR;

		int CurrentByte = 0;
		int CurrentBit = 0;

		public SPI()
		{
			// subscribe to SPI events
			SCLK_PORT.WriteRegister.OnRegisterChanged += OnClkChanged;
			SPCR = AtmelContext.RAM[AtmelIO.SPCR] as ObservableRegister;
			SPSR = AtmelContext.RAM[AtmelIO.SPSR] as ObservableRegister;
			SPDR = AtmelContext.RAM[AtmelIO.SPDR] as ObservableRegister;
			SPDR.OnRegisterChanged += SPDR_OnRegisterChanged;
			SPDR.OnRegisterRead += SPDR_OnRegisterRead;
		}

		void SPDR_OnRegisterRead(ref int val)
		{
			// fill SPDR with the incoming byte
			val = SPDR_ReadBuffer;

			if (AtmelContext.Active)
			{
				// clear SPIF flag
				SPI.SPSR[AtmelIO.SPIF] = 0;
			}
		}

		public static void ReceiveByte(int val)
		{
			SPDR_ReadBuffer = val;

			// set the SPIF flag
			SPI.SPSR[AtmelIO.SPIF] = 1;
			AtmelContext.UpdateInterruptFlags();
		}

		void SPDR_OnRegisterChanged(int oldVal, int newVal)
		{
			// make sure SPI is enabled
			if (SPI.SPCR[AtmelIO.SPE] == 0)
				return;

			// broadcast the byte immediately
			if (this.OnReceivedByte != null)
				this.OnReceivedByte(newVal);
			
			// set the transfer complete flag
			SPI.SPSR[AtmelIO.SPIF] = 1;
			AtmelContext.UpdateInterruptFlags();
		}

		void OnClkChanged(int oldVal, int newVal)
		{
			// make sure SPI is enabled - wait, what's going on here? this should be 0?
			if (SPI.SPCR[AtmelIO.SPE] == 1)
				return;

			// make sure it's the right bit that has changed
			var changed = oldVal ^ newVal;
			if ((changed & (1 << SCLK_BIT)) == 0)
				return;

			// make sure we're on the rising edge
			if ((newVal & (1 << SCLK_BIT)) == 0)
				return;

			// make note of this bit
			this.CurrentByte <<= 1;
			if ((SDIN_PORT.WriteRegister.Value & (1<<SDIN_BIT)) != 0)
				this.CurrentByte |= 1;

			// advance the bit
			this.CurrentBit++;
			if (this.CurrentBit < 8)
				return;

			// pass it on to any SPI devices listening
			if (this.OnReceivedByte != null)
				this.OnReceivedByte(this.CurrentByte);
			this.CurrentBit = 0;
			this.CurrentByte = 0;
		}
		

	}
}
