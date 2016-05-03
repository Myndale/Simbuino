$(function () {

	// this is a memory-mapped register class that is used mainly to provide change notification for when a register changes,
	// it's needed by devices (LCD etc) that need to know when a memory location has been written to so they can react accordingly.

	MemoryMappedRegister = Class.create({

		ctor: function (index) {
			this.Index = index;
		},

		get: function()
		{
			return AtmelContext.R[this.Index];
		},

		set: function(value)
		{
			var oldVal = AtmelContext.R[this.Index];
			if (oldVal != value)
				AtmelContext.R[this.Index] = value & 0xff;
		},

		get_bit: function(index)
		{
			return ((AtmelContext.R[this.Index] >> index) & 1);
		},

		set_bit: function(index, value)
		{
			if (value == 0)
				this.set(this.get() & ~(1 << index));
			else
				this.set(this.get() | (1 << index));
		},

		Reset: function()
		{
			AtmelContext.R[this.Index] = 0;
		},
	});


	ObservableRegister = Class.create({

		ctor: function(index)
		{
			this.Index = index;
			this.OnRegisterChanged = [];
			this.OnRegisterRead = [];
		},

		get: function()
		{
			var val = AtmelContext.R[this.Index];
			if (this.OnRegisterRead != null)
				for (var i=0; i<this.OnRegisterRead.length; i++)
					val = this.OnRegisterRead[i](val);
			return val;
		},

		set: function(value)
		{
			var oldVal = AtmelContext.R[this.Index];
			AtmelContext.R[this.Index] = value & 0xff;
			if (this.OnRegisterChanged != null)
				for (var i = 0; i < this.OnRegisterChanged.length; i++)
					this.OnRegisterChanged[i](oldVal, value);
		},

		get_bit: function(index)
		{
			return ((AtmelContext.R[this.Index] >> index) & 1);
		},

		set_bit: function(index, value)
		{
			if (value == 0)
				this.set(this.get() & ~(1 << index));
			else
				this.set(this.get() | (1 << index));
		},

		Reset: function()
		{
			AtmelContext.R[this.Index] = 0;
		}
	});

	MemoryMappedWordRegister = Class.create({

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
			var lo = AtmelContext.R[this.Index];
			var hi = AtmelContext.R[this.Index + 1];
			return (lo | (hi << 8));
		},

		set: function(value)
		{
			AtmelContext.R[this.Index] = value & 0xff;
			AtmelContext.R[this.Index + 1] = (value >> 8) & 0xff;
		},

		get_bit: function(index)
		{
			return ((this.get() >> index) & 1);
		},

		set_bit: function(index, value)
		{
			if (value == 0)
				this.set((this.get() & ~(1 << index)));
			else
				this.set((this.get() | (1 << index)));
		}
		
	});

});