$(function () {

	// basic class for emulating the USART (i.e. Serial) functionality of the AVR chip
	USART = {
		Init: function()
		{
			var self = this;
			this.UCSR0A = AtmelContext.RAM[AtmelIO.UCSR0A];
			this.UCSR0B = AtmelContext.RAM[AtmelIO.UCSR0B];
			this.UDR0 = AtmelContext.RAM[AtmelIO.UDR0];

			this.UCSR0A.OnRegisterRead.push(function (val) { return self.UCSR0A_OnRegisterRead(val); });
			this.UCSR0A.OnRegisterChanged.push(function (oldVal, newVal) { self.UCSR0A_OnRegisterChanged(oldVal, newVal); });
			this.UCSR0B.OnRegisterChanged.push(function (oldVal, newVal) { self.UCSR0B_OnRegisterChanged(oldVal, newVal); });
			this.UDR0.OnRegisterChanged.push(function (oldVal, newVal) { self.UDR0_OnRegisterChanged(oldVal, newVal); });
		},
	
		// the UDREn bit in UCSRnA signals when the transmit buffer is empty. for now the transmit buffer is always empty.
		UCSR0A_OnRegisterRead: function(val)
		{
			return (1 << AtmelIO.UDRE0) | (1 << AtmelIO.TXC0);
		},

		// TXC0 bit is cleared by writing a 1 to its location
		UCSR0A_OnRegisterChanged: function(oldVal, newVal)
		{
			//if ((newVal & (1 << TXC0_BIT)) != 0)
			//{
			//	if ((UCSR0A.Value & (1 << TXC0_BIT)) != 0)
			//		UCSR0A.Value = newVal &= (1 << TXC0_BIT);
			//}
		},

		UCSR0B_OnRegisterChanged: function(oldVal, newVal)
		{
			// is the Transmit Compete Interrupt flags being enabled?
			if ((newVal & (1 << AtmelIO.UDRIE0)) != 0)
			{
				// yep, so check if we need to generate an interrupt
				AtmelContext.UpdateInterruptFlags();
			}
		},

		UDR0_OnRegisterChanged: function(oldVal, newVal)
		{
			// byte is being transmitted
			//TransmitLog += Convert.ToChar(newVal);

			// assume transfer happened immediately and set TXC0
			UCSR0A.set(UCSR0A.get() | (1 << AtmelIO.TXC0));

			// check if we need to generate another interrupt
			AtmelContext.UpdateInterruptFlags();
		}
	}

});
