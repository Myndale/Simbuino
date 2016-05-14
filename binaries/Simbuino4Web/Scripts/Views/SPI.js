$(function () {

	// emulates the SPI functionality of the AVR chip, needed by the LCD and SD card devices (among other things)
	SPI =
	{
		Init: function () {
			var self = this;
			this.OnReceivedByte = [];
			this.SPDR_ReadBuffer = 0;

			this.SDIN_PORT = AtmelContext.B; // 11
			this.SDIN_BIT = 3;
			this.SCLK_PORT = AtmelContext.B; // 13
			this.SCLK_BIT = 5;

			this.CurrentByte = 0;
			this.CurrentBit = 0;

			this.SCLK_PORT.WriteRegister.get().OnRegisterChanged.push(function (oldVal, newVal) { self.OnClkChanged(oldVal, newVal); });
			this.SPCR = AtmelContext.RAM[AtmelIO.SPCR];
			this.SPSR = AtmelContext.RAM[AtmelIO.SPSR];
			this.SPDR = AtmelContext.RAM[AtmelIO.SPDR];
			this.SPDR.OnRegisterChanged.push(function (oldVal, newVal) { self.SPDR_OnRegisterChanged(oldVal, newVal); });
			this.SPDR.OnRegisterRead.push(function (val) { return self.SPDR_OnRegisterRead(val); });
		},

		SPDR_OnRegisterRead: function(val)
		{
			// fill SPDR with the incoming byte
			val = this.SPDR_ReadBuffer;

			if (AtmelContext.Active)
			{
				// clear SPIF flag
				this.SPSR.set_bit(AtmelIO.SPIF, 0);
			}

			return val;
		},

		ReceiveByte: function(val)
		{
			this.SPDR_ReadBuffer = val;

			// set the SPIF flag
			this.SPSR.set_bit(AtmelIO.SPIF, 1);
			AtmelContext.UpdateInterruptFlags();
		},

		SPDR_OnRegisterChanged: function(oldVal, newVal)
		{
			// make sure SPI is enabled
			if (this.SPCR.get_bit(AtmelIO.SPE) == 0)
				return;

			// broadcast the byte immediately
			if (this.OnReceivedByte != null)
				for (var i = 0; i<this.OnReceivedByte.length; i++)
					this.OnReceivedByte[i](newVal);
			
			// set the transfer complete flag
			this.SPSR.set_bit(AtmelIO.SPIF, 1);
			AtmelContext.UpdateInterruptFlags();
		},

		OnClkChanged: function(oldVal, newVal)
		{
			// make sure SPI is enabled - wait, what's going on here? this should be 0?
			if (this.SPCR.get_bit(AtmelIO.SPE) == 1)
				return;

			// make sure it's the right bit that has changed
			var changed = oldVal ^ newVal;
			if ((changed & (1 << this.SCLK_BIT)) == 0)
				return;

			// make sure we're on the rising edge
			if ((newVal & (1 << this.SCLK_BIT)) == 0)
				return;

			// make note of this bit
			this.CurrentByte = this.CurrentByte << 1;
			if ((SDIN_PORT.WriteRegister.get().get() & (1 << this.SDIN_BIT)) != 0)
				this.CurrentByte += 1;

			// advance the bit
			this.CurrentBit++;
			if (this.CurrentBit < 8)
				return;

			// pass it on to any SPI devices listening
			if (this.OnReceivedByte != null)
				for (var i=0; i<this.OnReceivedByte.length; i++)
					this.OnReceivedByte[i](this.CurrentByte);
			this.CurrentBit = 0;
			this.CurrentByte = 0;
		}

	}
});
