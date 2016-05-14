$(function () {

	EEPROM =
	{
		Init: function()
		{
			var self = this;
			var eecr = AtmelContext.RAM[AtmelIO.EECR];
			eecr.OnRegisterRead.push(function (val) { return self.EECR_OnRegisterRead(val); });
			eecr.OnRegisterChanged.push(function (oldVal, newVal) { self.EECR_OnRegisterChanged(oldVal, newVal); });
			var eedr = AtmelContext.RAM[AtmelIO.EEDR];
			eedr.OnRegisterRead.push(function (val) { return self.EEDR_OnRegisterRead(val); });
		},

		EECR_OnRegisterRead: function(val)
		{
			// eeprom is alreadys ready to read and write
			return 0;
		},

		EEDR_OnRegisterRead: function(val)
		{
			// yes, grab the address and data
			var address = AtmelContext.RAM[AtmelIO.EEARH].get() * 256 + AtmelContext.RAM[AtmelIO.EEARL].get();

			// and do the read
			return AtmelContext.EEPROM[address];
		},

		EECR_OnRegisterChanged: function(oldVal, newVal)
		{
			// todo: what's going on here? why isn't this value being set?
			// make sure master write enable has been set
			//if ((newVal & (1 << EEMPE)) == 0)
			//	return;

			// are we trying to do a write?
			if ((newVal & (1 << AtmelIO.EEPE)) != 0)
			{
				// yes, grab the address and data
				var address = AtmelContext.RAM[AtmelIO.EEARH].get() * 256 + AtmelContext.RAM[AtmelIO.EEARL].get();
				var data = AtmelContext.R[AtmelIO.EEDR];	// use the actual value written, not what it returns if we read it

				// and do the write
				AtmelContext.EEPROM[address] = data;
			}
		}
	}

});
