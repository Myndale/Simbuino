$(function () {

	// helper class for accessing the gamebuino buttons
	Buttons =
	{
		Init: function()
		{
			this.UP_PORT = AtmelContext.B; // D9
			this.UP_BIT = 1;
			this.RIGHT_PORT = AtmelContext.D; // D7
			this.RIGHT_BIT = 7;
			this.DOWN_PORT = AtmelContext.D; // D6
			this.DOWN_BIT = 6;
			this.LEFT_PORT = AtmelContext.B; // D8
			this.LEFT_BIT = 0;
			this.A_PORT = AtmelContext.D; // D4
			this.A_BIT = 4;
			this.B_PORT = AtmelContext.D; // D2
			this.B_BIT = 2;
			this.C_PORT = AtmelContext.C; // A3
			this.C_BIT = 3;

			this.Reset();
		},

		Reset: function ()
		{
			this.Up().set(false);
			this.Down().set(false);
			this.Left().set(false);
			this.Right().set(false);
			this.A().set(false);
			this.B().set(false);
			this.C().set(false);
		},

		Up: function()
		{
			var self = this;
			var result =
			{
				get: function () { return self.UP_PORT.ReadRegister.get().get_bit(self.UP_BIT) == 0; },
				set: function (value) { self.UP_PORT.ReadRegister.get().set_bit(self.UP_BIT, value ? 0 : 1); }
			};
			return result;
		},

		Down: function()
		{
			var self = this;
			var result =
			{
				get: function () { return self.DOWN_PORT.ReadRegister.get().get_bit(self.DOWN_BIT) == 0; },
				set: function (value) { self.DOWN_PORT.ReadRegister.get().set_bit(self.DOWN_BIT, value ? 0 : 1); }
			};
			return result;
		},

		Left: function()
		{
			var self = this;
			var result =
			{
				get: function () { return self.LEFT_PORT.ReadRegister.get().get_bit(self.LEFT_BIT) == 0; },
				set: function (value) { self.LEFT_PORT.ReadRegister.get().set_bit(self.LEFT_BIT, value ? 0 : 1); }
			};
			return result;
		},

		Right: function()
		{
			var self = this;
			var result =
			{
				get: function () { return self.RIGHT_PORT.ReadRegister.get().get_bit(self.RIGHT_BIT) == 0; },
				set: function (value) { self.RIGHT_PORT.ReadRegister.get().set_bit(self.RIGHT_BIT, value ? 0 : 1); }
			};
			return result;
		},

		A: function()
		{
			var self = this;
			var result =
			{
				get: function () { return self.A_PORT.ReadRegister.get().get_bit(self.A_BIT) == 0; },
				set: function (value) { self.A_PORT.ReadRegister.get().set_bit(self.A_BIT, value ? 0 : 1); }
			};
			return result;
		},

		B: function()
		{
			var self = this;
			var result =
			{
				get: function () { return self.B_PORT.ReadRegister.get().get_bit(self.B_BIT) == 0; },
				set: function (value) { self.B_PORT.ReadRegister.get().set_bit(self.B_BIT, value ? 0 : 1); }
			};
			return result;
		},

		C: function()
		{
			var self = this;
			var result =
			{
				get: function () { return self.C_PORT.ReadRegister.get().get_bit(self.C_BIT) == 0; },
				set: function (value) { self.C_PORT.ReadRegister.get().set_bit(self.C_BIT, value ? 0 : 1); }
			};
			return result;
		}
	};


});
