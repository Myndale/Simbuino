$(function () {

	function range(start, count) {
		if(arguments.length == 1) {
			count = start;
			start = 0;
		}
		var foo = [];
		for (var i = 0; i < count; i++) {
			foo.push(0);
		}
		return foo;
	}

	// AtMega328P context (i.e. current state).
	AtmelContext =
	{
		Flash: [],
		EEPROM: [],
		RAM: [],
		PC: 0,
		Clock: 0,
		Flags: 0,

		InterruptPending: false,
		Timer0: 0,
		Timer0OutputCompareFlag: false,
		Timer1: 0,
		Timer1OutputCompareFlag: false,
		Timer1OverflowFlag: false,
		LastTimerUpdate: 0,
		NextTimerEvent: 0,
		UDRE_InterruptPending: false,
		SPI_InterruptPending: false,
		
		FlashSize: 32 * 1024 / 2,
		EEPROMSize: 1024,		

		// general register file mapped from 0x00 to 0x1f
		FirstReg: 0x00,
		NumRegs: 32,

		// regular I/O from 0x20 to 0x5f and extended I/O from 0x60-0xff
		FirstIO: 0x20,
		NumIO: 224,

		// main SRAM
		FirstSRAM: 0x100,
		RAMSize: 2 * 1024,

		// some registers cause hardware changes when simply read e.g. reading SPDR clears the SPIF flag.
		// this flag is used to disable such changes so that code like the display update can access those
		// registers without inadvertently modifying the context state.
		Active: true,

		Reset: function()
		{
			for (var i = 0; i < this.RAM.length; i++)
				this.RAM[i].Reset();
			this.PC = 0;
			this.Clock = 0;
			this.Timer0 = 0;
			this.Timer1 = 0;
			this.InterruptPending = false;
			this.Timer0OutputCompareFlag = false;
			this.Timer1OutputCompareFlag = false;
			this.Timer1OverflowFlag = false;
			this.LastTimerUpdate = 0;
			this.NextTimerEvent = 0;
			this.UDRE_InterruptPending = false;
			this.SPI_InterruptPending = false;			
		},

		InvalidateTimers: function(oldVal, newVal)
		{
			if (oldVal != newVal)
				this.NextTimerEvent = this.Clock;
		},

		UpdateInterruptFlags: function()
		{
			this.InterruptPending = false;
			this.UDRE_InterruptPending = false;
			this.SPI_InterruptPending = false;

			// if the data transmit register is set and interrupts are enabled then trigger a USART interrupt
			if ((USART.UCSR0A.get_bit(AtmelIO.TXC0) != 0) && (USART.UCSR0B.get_bit(AtmelIO.UDRIE0) != 0) && (SREG.I.get() == 0))
				this.UDRE_InterruptPending = true;

			// if transfer complete flag is set and interrupts are enabled then trigger an SPI interrupt
			if ((SPI.SPSR.get_bit(AtmelIO.SPIF) != 0) && (SPI.SPCR.get_bit(AtmelIO.SPIE) != 0) && (SREG.I.get() == 0))
				this.SPI_InterruptPending = true;

			// coalesce all interrupt flags into one so that we don't have to check them all in the inner loop
			this.InterruptPending |=
				this.UDRE_InterruptPending |
				this.SPI_InterruptPending |
				this.Timer0OutputCompareFlag |
				this.Timer1OutputCompareFlag |
				this.Timer1OverflowFlag;
		},

		ClearEEPROM: function()
		{
			for (var i = 0; i < this.EEPROM.length; i++)
				this.EEPROM[i] = 0xff;
		},

		Init: function()
		{
			var self = this;
			this.Flash = range(0, this.FlashSize);
			this.EEPROM = range(0, this.EEPROMSize);
			this.R = range(0, this.NumRegs + this.NumIO + this.RAMSize);
			var mappedR = [];
			for (var addr = 0; addr < this.NumRegs; addr++)
				mappedR[addr] = MemoryMappedRegister.create(addr);
			this.IO = [];
			for (var addr = 0; addr < this.NumIO; addr++)
				this.IO[addr] = ObservableRegister.create(this.FirstIO + addr);
			this.SREG = AtmelFlagsRegister;
			this.IO[(AtmelFlagsRegister.FlagsIndex - this.FirstIO)] = this.SREG;
			var sram = [];
			for (var addr = 0; addr < this.RAMSize; addr++)
				sram[addr] = MemoryMappedRegister.create(this.FirstSRAM + addr);

			this.RAM = [];
			for (var i = 0; i < mappedR.length; i++)
				this.RAM.push(mappedR[i]);
			for (var i = 0; i < this.IO.length; i++)
				this.RAM.push(this.IO[i]);
			for (var i = 0; i < sram.length; i++)
				this.RAM.push(sram[i]);

			// general purpose registers
			this.B = PortRegister.create(0x23, 0x25, 0x24);
			this.C = PortRegister.create(0x26, 0x28, 0x27);
			this.D = PortRegister.create(0x29, 0x2b, 0x2a);
			this.X = IndirectAddressRegister.create(26);
			this.Y = IndirectAddressRegister.create(28);
			this.Z = IndirectAddressRegister.create(30);
			this.SP = MemoryMappedWordRegister.create(0x5d);
			this.ClearEEPROM();

			// timer states have to be re-evaluated whenever any of their control variables are changed
			this.RAM[AtmelIO.CLKPR].OnRegisterChanged.push(function (oldVal, newVal) { self.InvalidateTimers(oldVal, newVal); });
			this.RAM[AtmelIO.TCCR0A].OnRegisterChanged.push(function (oldVal, newVal) { self.InvalidateTimers(oldVal, newVal); });
			this.RAM[AtmelIO.TCCR0B].OnRegisterChanged.push(function (oldVal, newVal) { self.InvalidateTimers(oldVal, newVal); });
			this.RAM[AtmelIO.TCNT1H].OnRegisterChanged.push(function (oldVal, newVal) { self.InvalidateTimers(oldVal, newVal); });
			this.RAM[AtmelIO.TCNT1L].OnRegisterChanged.push(function (oldVal, newVal) { self.InvalidateTimers(oldVal, newVal); });
			this.RAM[AtmelIO.TCCR1A].OnRegisterChanged.push(function (oldVal, newVal) { self.InvalidateTimers(oldVal, newVal); });
			this.RAM[AtmelIO.TCCR1B].OnRegisterChanged.push(function (oldVal, newVal) { self.InvalidateTimers(oldVal, newVal); });
		}
	}

});