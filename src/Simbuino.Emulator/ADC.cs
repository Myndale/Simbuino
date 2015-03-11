using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Simbuino.Emulator
{
	// this class emulates the AVRs ADC circuitry
	public class ADC
	{
		public static ObservableRegister ADSCRA;
		public static ObservableRegister ADMUX;
		public static ObservableRegister ADCH;
		public static ObservableRegister ADCL;

		// hard-coded for now...
		private int BatteryLevel = 0x308;
		private int AmbientLightSensor = 0x00;

		public ADC()
		{
			ADSCRA = AtmelContext.RAM[AtmelIO.ADSCRA] as ObservableRegister;
			ADSCRA.OnRegisterChanged += ADSCRA_OnRegisterChanged;
			ADMUX = AtmelContext.RAM[AtmelIO.ADMUX] as ObservableRegister;
			ADCH = AtmelContext.RAM[AtmelIO.ADCH] as ObservableRegister;
			ADCH.OnRegisterRead += ADCH_OnRegisterRead;
			ADCL = AtmelContext.RAM[AtmelIO.ADCL] as ObservableRegister;
			ADCL.OnRegisterRead += ADCL_OnRegisterRead;
		}

		void ADSCRA_OnRegisterChanged(int oldVal, int newVal)
		{
			// finish adc conversions immediately
			if ((newVal & (1 << AtmelIO.ADSC)) != 0)
				ADSCRA.Value = newVal ^ (1 << AtmelIO.ADSC);
		}

		void ADCH_OnRegisterRead(ref int val)
		{
			// get the ADC channel num
			int adc = (ADMUX.Value & 0x0f);

			// fetch the appropriate value
			int result =
				(adc == 6) ? this.BatteryLevel :
				(adc == 7) ? this.AmbientLightSensor :
				0;

			// the ADLAR bit determines the format
			if (ADMUX[AtmelIO.ADLAR] == 0)
				val = (result >> 8) & 0x03;
			else
				val = (result >> 2) & 0xff;
		}

		void ADCL_OnRegisterRead(ref int val)
		{
			// get the ADC channel num
			int adc = (ADMUX.Value & 0x0f);

			// fetch the appropriate value
			int result =
				(adc == 6) ? this.BatteryLevel :
				(adc == 7) ? this.AmbientLightSensor :
				0;

			// the ADLAR bit determines the format
			if (ADMUX[AtmelIO.ADLAR] == 0)
				val = result & 0xff;
			else
				val = (result << 6) & 0xC0;
		}
		

	}
}
