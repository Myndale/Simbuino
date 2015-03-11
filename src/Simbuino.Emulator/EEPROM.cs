using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Simbuino.Emulator
{
	// emulates AVR eeprom. todo: eeprom writes currently occur instantaneously, need to add a delay
	public class EEPROM
	{
		public EEPROM()
		{
			var eecr = AtmelContext.RAM[AtmelIO.EECR] as ObservableRegister;
			eecr.OnRegisterRead += EECR_OnRegisterRead;
			eecr.OnRegisterChanged += EECR_OnRegisterChanged;

			var eedr = AtmelContext.RAM[AtmelIO.EEDR] as ObservableRegister;
			eedr.OnRegisterRead += EEDR_OnRegisterRead;
		}

		void EECR_OnRegisterRead(ref int val)
		{
			// eeprom is alreadys ready to read and write
			val = 0;
		}

		void EEDR_OnRegisterRead(ref int val)
		{
			// yes, grab the address and data
			var address = AtmelContext.RAM[AtmelIO.EEARH].Value * 256 + AtmelContext.RAM[AtmelIO.EEARL].Value;

			// and do the read
			val = AtmelContext.EEPROM[address];
		}

		void EECR_OnRegisterChanged(int oldVal, int newVal)
		{
			/* todo: what's going on here? why isn't this value being set?
			// make sure master write enable has been set
			if ((newVal & (1 << EEMPE)) == 0)
				return;
			*/

			// are we trying to do a write?
			if ((newVal & (1 << AtmelIO.EEPE)) != 0)
			{
				// yes, grab the address and data
				var address = AtmelContext.RAM[AtmelIO.EEARH].Value * 256 + AtmelContext.RAM[AtmelIO.EEARL].Value;
				var data = AtmelContext.R[AtmelIO.EEDR];	// use the actual value written, not what it returns if we read it

				// and do the write
				AtmelContext.EEPROM[address] = data;
			}
		}				

	}
}
