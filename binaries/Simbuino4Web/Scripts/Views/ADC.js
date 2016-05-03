$(function () {

	// this class emulates the AVRs ADC circuitry
	ADC = {
		BatteryLevel: 0x308,
		AmbientLightSensor: 0x00,

		Init: function()
		{
			var self = this;
			this.ADSCRA = AtmelContext.RAM[AtmelIO.ADSCRA];
			this.ADSCRA.OnRegisterChanged.push(function (oldVal, newVal) { self.ADSCRA_OnRegisterChanged(oldVal, newVal); });
			this.ADMUX = AtmelContext.RAM[AtmelIO.ADMUX];
			this.ADCH = AtmelContext.RAM[AtmelIO.ADCH];
			this.ADCH.OnRegisterRead.push(function (oldVal, newVal) { self.ADCH_OnRegisterRead(oldVal, newVal); });
			this.ADCL = AtmelContext.RAM[AtmelIO.ADCL];
			this.ADCL.OnRegisterRead.push(function (oldVal, newVal) { self.ADCL_OnRegisterRead(oldVal, newVal); });
		},

		ADSCRA_OnRegisterChanged: function(oldVal, newVal)
		{
			// finish adc conversions immediately
			if ((newVal & (1 << AtmelIO.ADSC)) != 0)
				this.ADSCRA.set(newVal ^ (1 << AtmelIO.ADSC));
		},

		ADCH_OnRegisterRead: function(val)
		{
			// get the ADC channel num
			var adc = (this.ADMUX.get() & 0x0f);

			// fetch the appropriate value
			var result =
			(adc == 6) ? this.BatteryLevel :
			(adc == 7) ? this.AmbientLightSensor :
			0;


			this.ADCL = AtmelContext.RAM[AtmelIO.ADCL];
			this.ADCL.OnRegisterRead.push(function (oldVal, newVal) { self.ADCL_OnRegisterRead(oldVal, newVal); });
		},

		ADSCRA_OnRegisterChanged: function(oldVal, newVal)
		{
			// finish adc conversions immediately
			if ((newVal & (1 << AtmelIO.ADSC)) != 0)
				this.ADSCRA.set(newVal ^ (1 << AtmelIO.ADSC));
		},

		ADCH_OnRegisterRead: function(val)
		{
			// get the ADC channel num
			var adc = (this.ADMUX.get() & 0x0f);

			// fetch the appropriate value
			var result =
			(adc == 6) ? this.BatteryLevel :
			(adc == 7) ? this.AmbientLightSensor :
			0;

			// the ADLAR bit determines the format
			if (this.ADMUX.get_bit(AtmelIO.ADLAR) == 0)
				return (result >> 8) & 0x03;
			else
				return (result >> 2) & 0xff;
		},

		ADCL_OnRegisterRead: function(val)
		{
			// get the ADC channel num
			var adc = (this.ADMUX.get() & 0x0f);

			// fetch the appropriate value
			var result =
			(adc == 6) ? this.BatteryLevel :
			(adc == 7) ? this.AmbientLightSensor :
			0;

			// the ADLAR bit determines the format
			if (this.ADMUX.get_bit(AtmelIO.ADLAR) == 0)
				return result & 0xff;
			else
				return (result << 6) & 0xC0;
		}

	}

});
