
$(function () {

	AtmelFlagsRegister =
	{

		get: function () {
			return AtmelContext.Flags;
		},

		set: function (value) {
			AtmelContext.Flags = value;
		},

		get_bit: function (index) {
			return ((AtmelContext.Flags >> index) & 1);
		},

		set_bit: function (index, value) {
			if (value == 0)
				AtmelContext.Flags = AtmelContext.Flags & ~(1 << index);
			else
				AtmelContext.Flags = AtmelContext.Flags | (1 << index);
		},

		CPOS: 0,
		C:
		{
			get: function () { return ((AtmelContext.Flags >> AtmelFlagsRegister.CPOS) & 1); },
			set: function (value) { AtmelContext.Flags = (value == 0) ? AtmelContext.Flags & ~(1 << AtmelFlagsRegister.CPOS) : AtmelContext.Flags | (1 << AtmelFlagsRegister.CPOS); }
		},

		ZPOS: 1,
		Z:
		{
			get: function () { return ((AtmelContext.Flags >> AtmelFlagsRegister.ZPOS) & 1); },
			set: function (value) { AtmelContext.Flags = (value == 0) ? AtmelContext.Flags & ~(1 << AtmelFlagsRegister.ZPOS) : AtmelContext.Flags | (1 << AtmelFlagsRegister.ZPOS); }
		},

		NPOS: 2,
		N:
		{
			get: function () { return ((AtmelContext.Flags >> AtmelFlagsRegister.NPOS) & 1); },
			set: function (value) { AtmelContext.Flags = (value == 0) ? AtmelContext.Flags & ~(1 << AtmelFlagsRegister.NPOS) : AtmelContext.Flags | (1 << AtmelFlagsRegister.NPOS); }
		},

		VPOS: 3,
		V:
		{
			get: function () { return ((AtmelContext.Flags >> AtmelFlagsRegister.VPOS) & 1); },
			set: function (value) { AtmelContext.Flags = (value == 0) ? AtmelContext.Flags & ~(1 << AtmelFlagsRegister.VPOS) : AtmelContext.Flags | (1 << AtmelFlagsRegister.VPOS); }
		},

		SPOS: 4,
		S:
		{
			get: function () { return ((AtmelContext.Flags >> AtmelFlagsRegister.SPOS) & 1); },
			set: function (value) { AtmelContext.Flags = (value == 0) ? AtmelContext.Flags & ~(1 << AtmelFlagsRegister.SPOS) : AtmelContext.Flags | (1 << AtmelFlagsRegister.SPOS); }
		},

		HPOS: 5,
		H:
		{
			get: function () { return ((AtmelContext.Flags >> AtmelFlagsRegister.HPOS) & 1); },
			set: function (value) { AtmelContext.Flags = (value == 0) ? AtmelContext.Flags & ~(1 << AtmelFlagsRegister.HPOS) : AtmelContext.Flags | (1 << AtmelFlagsRegister.HPOS); }
		},

		TPOS: 6,
		T:
		{
			get: function () { return ((AtmelContext.Flags >> AtmelFlagsRegister.TPOS) & 1); },
			set: function (value) { AtmelContext.Flags = (value == 0) ? AtmelContext.Flags & ~(1 << AtmelFlagsRegister.TPOS) : AtmelContext.Flags | (1 << AtmelFlagsRegister.TPOS); }
		},

		IPOS: 7,
		I:
		{
			get: function () { return ((AtmelContext.Flags >> AtmelFlagsRegister.IPOS) & 1); },
			set: function (value) { AtmelContext.Flags = (value == 0) ? AtmelContext.Flags & ~(1 << AtmelFlagsRegister.IPOS) : AtmelContext.Flags | (1 << AtmelFlagsRegister.IPOS); }
		},

		FlagsIndex: 0x5f,

		Reset: function () {
			AtmelContext.R[this.FlagsIndex] = 0;
		}
	}

});
