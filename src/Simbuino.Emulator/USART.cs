using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/*
 * Summary of USART operation:
 * 
 * Data gets written to the transmit buffer (EDR0) then moved to the shift register and shifted out.
 * 
 * When the transmit buffer is empty the Data Register Empty flag (UDRE0) is set. If the Data Register Empty Interupt Enable flag (UDRIE0) is set to 1 then an
 * interrupt is generated (provided the register is empty). The UDRE0 flag is cleared automatically when the transmit buffer (EDR0) is written to.
 * 
 * When the shift register has finished shifting the data out the Transmit Complete flag (TXC0) is set. If the Transmit Compete Interrupt Enable flag (TXCIE0) is set
 * to 1 then an interrupt is generated, this gets cleared by the interrupt or it can be written to.
 * 
 * */

namespace Simbuino.Emulator
{
	// basic class for emulating the USART (i.e. Serial) functionality of the AVR chip
	public class USART : INotifyPropertyChanged
	{
		public event PropertyChangedEventHandler PropertyChanged;

		public static ObservableRegister UCSR0A { get; private set; }
		public static ObservableRegister UCSR0B { get; private set; }
		public static ObservableRegister UDR0 { get; private set; }

		

		private string _TransmitLog = "";
		public string TransmitLog
		{
			get
			{
				return this._TransmitLog;
			}

			set
			{
				this._TransmitLog = value;
				OnPropertyChanged("TransmitLog");
			}

		}

		public USART()
		{
			UCSR0A = AtmelContext.RAM[AtmelIO.UCSR0A] as ObservableRegister;
			UCSR0B = AtmelContext.RAM[AtmelIO.UCSR0B] as ObservableRegister;
			UDR0 = AtmelContext.RAM[AtmelIO.UDR0] as ObservableRegister;

			UCSR0A.OnRegisterRead += UCSR0A_OnRegisterRead;
			UCSR0A.OnRegisterChanged += UCSR0A_OnRegisterChanged;
			UCSR0B.OnRegisterChanged += UCSR0B_OnRegisterChanged;
			UDR0.OnRegisterChanged += UDR0_OnRegisterChanged;
		}

		// the UDREn bit in UCSRnA signals when the transmit buffer is empty. for now the transmit buffer is always empty.
		void UCSR0A_OnRegisterRead(ref int val)
		{
			val = (1 << AtmelIO.UDRE0) | (1 << AtmelIO.TXC0);
		}

		// TXC0 bit is cleared by writing a 1 to its location
		void UCSR0A_OnRegisterChanged(int oldVal, int newVal)
		{
			/*
			if ((newVal & (1 << TXC0_BIT)) != 0)
			{
				if ((UCSR0A.Value & (1 << TXC0_BIT)) != 0)
					UCSR0A.Value = newVal &= (1 << TXC0_BIT);
			}
			 * */
		}

		void UCSR0B_OnRegisterChanged(int oldVal, int newVal)
		{
			// is the Transmit Compete Interrupt flags being enabled?
			if ((newVal & (1 << AtmelIO.UDRIE0)) != 0)
			{
				// yep, so check if we need to generate an interrupt
				AtmelContext.UpdateInterruptFlags();
			}
		}

		void UDR0_OnRegisterChanged(int oldVal, int newVal)
		{
			// byte is being transmitted
			TransmitLog += Convert.ToChar(newVal);

			// assume transfer happened immediately and set TXC0
			UCSR0A.Value |= (1 << AtmelIO.TXC0);

			// check if we need to generate another interrupt
			AtmelContext.UpdateInterruptFlags();
		}

		protected void OnPropertyChanged(string name)
		{
			PropertyChangedEventHandler handler = PropertyChanged;
			if (handler != null)
			{
				handler(this, new PropertyChangedEventArgs(name));
			}
		}

	}

}
