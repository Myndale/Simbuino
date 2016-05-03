$(function () {

	// these classes are accessors for specialty AVR registers i.e. those with different read/write values and/or addresses
	PortRegister = Class.create({

		ctor: function(readIndex, writeIndex, dirIndex)
		{
			var self = this;
			this.ReadIndex = readIndex;
			this.WriteIndex = writeIndex;
			this.DirIndex = dirIndex;

			this.ReadRegister =
			{
				get: function() { return AtmelContext.RAM[self.ReadIndex]; },
				set: function(value) { }
			}

			this.WriteRegister =
			{
				get: function() { return AtmelContext.RAM[self.WriteIndex]; },
				set: function(value) { }
			}

			AtmelContext.RAM[writeIndex].OnRegisterChanged.push(
				function (oldVal, newVal) { self.PortRegister_OnRegisterChanged(oldVal, newVal); }
			);

		},

		PortRegister_OnRegisterChanged: function(oldVal, newVal)
		{
			// if a pin is set as an output then writing to its write register sets the input value on it's read register as well
			var readVal = AtmelContext.RAM[this.ReadIndex].get();
			var direction = AtmelContext.RAM[this.DirIndex].get();
			readVal = readVal & ~direction;
			readVal = readVal | (direction & newVal);
			AtmelContext.RAM[this.ReadIndex].set(readVal);
		},

		get: function()
		{
			return AtmelContext.RAM[this.ReadIndex].get();
		},

		set: function(value)
		{
			AtmelContext.RAM[this.WriteIndex].set(value);
		},

		get_bit: function(index)
		{
			return ((AtmelContext.RAM[this.ReadIndex].get() >> index) & 1);
		},

		set_bit: function(index, value)
		{
			if (value == 0)
				AtmelContext.RAM[this.WriteIndex].set(AtmelContext.RAM[this.WriteIndex].get() & ~(1 << index));
			else
				AtmelContext.RAM[this.WriteIndex].set(AtmelContext.RAM[this.WriteIndex].get() | (1 << index));
		},

		Reset: function()
		{
			AtmelContext.RAM[this.WriteIndex].Reset();
			AtmelContext.RAM[this.ReadIndex].Reset();
		}
	});

	IndirectAddressRegister  = Class.create({

		ctor: function(index)
		{
			this.Index = index;
		},

		Reset: function()
		{
			this.set(0);
		},

		get: function()
		{
			var lo = AtmelContext.RAM[this.Index].get();
			var hi = AtmelContext.RAM[(this.Index + 1)].get();
			return lo | (hi << 8);
		},

		set: function(value)
		{
			AtmelContext.RAM[this.Index].set(value & 0xff);
			AtmelContext.RAM[this.Index + 1].set(value >> 8);
		},

		get_bit: function(index)
		{
			return ((this.get() >> index) & 1);
		},

		set_bit: function(index, value)
		{
			if (value == 0)
				set((get() & ~(1 << index)));
			else
				set((get() | (1 << index)));
		}

	});

});
