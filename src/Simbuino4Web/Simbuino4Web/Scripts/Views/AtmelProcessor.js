$(function() {

	// contains look-up tables and code for the emulation of the AtMega328P chip itself
	AtmelProcessor =
	{
		ClockSpeed: 16000000,
		OpCodeMap: [],
		InstrTable: [],
		OpCodeAttribs: [],
		PCModifyMap: [],
		OpCodeSizes: [],

		// todo: declare the interrupt table somwhere		
		ClockTable: [1, 2, 4, 8, 16, 32, 64, 128, 256, 1, 1, 1, 1, 1, 1, 1],
		ClockSelectTable: [0, 1, 8, 64, 256, 1024, 1, 1],

		// interrupt vectors
		TIM1_COMPA: 0x16,
		TIM1_OVF: 0x1A,
		TIM0_OVF: 0x20,
		SPI_STC: 0x22,
		USART_UDRE: 0x26,

		FlagsAdd: [],
		FlagsSub: [],

		/* = 0xc0
		FlagsMask: ~(
			(1 << AtmelFlagsRegister.HPOS) |
			(1 << AtmelFlagsRegister.NPOS) |
			(1 << AtmelFlagsRegister.VPOS) |
			(1 << AtmelFlagsRegister.SPOS) |
			(1 << AtmelFlagsRegister.ZPOS) |
			(1 << AtmelFlagsRegister.CPOS)
		) & 0xff,
		*/

		Init: function() {
			this.InitOpCodes();
			this.InitHandlerMap();
			this.InitFlagTables();
		},

		InitFlagTables: function() {
			this.InitFlagsAdd();
			this.InitFlagsSub();
			AtmelFlagsRegister.set(0); // we've trashed it
		},

		InitFlagsAdd: function() {
			var reg = AtmelFlagsRegister;
			reg.set(0);
			this.FlagsAdd = [];
			for (var C = 0; C < 2; C++)
				for (var Rd = 0; Rd < 256; Rd++)
					for (var Rr = 0; Rr < 256; Rr++) {
						var R = Rd + Rr + C;
						var Rd3 = (Rd >> 3) & 1;
						var not_Rd3 = 1 - Rd3;
						var Rd7 = (Rd >> 7) & 1;
						var not_Rd7 = 1 - Rd7;
						var Rr3 = (Rr >> 3) & 1;
						var not_Rr3 = 1 - Rr3;
						var Rr7 = (Rr >> 7) & 1;
						var not_Rr7 = 1 - Rr7;
						var R3 = (R >> 3) & 1;
						var not_R3 = 1 - R3;
						var R7 = (R >> 7) & 1;
						var not_R7 = 1 - R7;
						reg.H.set((Rd3 & Rr3) | (Rr3 & not_R3) | (not_R3 & Rd3));
						reg.N.set((R >> 7) & 1);
						reg.V.set((Rd7 & Rr7 & not_R7) | (not_Rd7 & not_Rr7 & R7));
						reg.S.set(reg.N.get() ^ reg.V.get());
						reg.Z.set(((R & 0xff) == 0) ? 1 : 0);
						reg.C.set((Rd7 & Rr7) | (Rr7 & not_R7) | (not_R7 & Rd7));
						this.FlagsAdd[(C << 16) | (Rd << 8) | Rr] = reg.get();
					}
		},

		InitInstrTable: function() {
			var len = AtmelContext.FlashSize;
			this.InstrTable = [];
			for (var pc = 0; pc < len; pc++) {
				var opcode = AtmelContext.Flash[pc];
				var attrib = this.OpCodeAttribs[opcode];
				if (attrib)
					this.InstrTable[pc] = attrib.Handler;
				else
					this.InstrTable[pc] = this.Skip;
			}
		},

		Skip: function() {
			AtmelContext.Clock++;
			AtmelContext.PC++;
		},

		InitFlagsSub: function() {
			var reg = AtmelFlagsRegister;
			reg.set(0);
			this.FlagsSub = [];
			for (var C = 0; C < 2; C++)
				for (var Rd = 0; Rd < 256; Rd++)
					for (var Rr = 0; Rr < 256; Rr++) {
						var R = Rd - Rr - C;
						var Rd3 = (Rd >> 3) & 1;
						var not_Rd3 = 1 - Rd3;
						var Rd7 = (Rd >> 7) & 1;
						var not_Rd7 = 1 - Rd7;
						var Rr3 = (Rr >> 3) & 1;
						var not_Rr3 = 1 - Rr3;
						var Rr7 = (Rr >> 7) & 1;
						var not_Rr7 = 1 - Rr7;
						var R3 = (R >> 3) & 1;
						var not_R3 = 1 - R3;
						var R7 = (R >> 7) & 1;
						var not_R7 = 1 - R7;
						reg.H.set((not_Rd3 & Rr3) | (Rr3 & R3) | (R3 & not_Rr3));
						reg.N.set((R >> 7) & 1);
						reg.V.set((Rd7 & not_Rr7 & not_R7) | (not_Rd7 & Rr7 & R7));
						reg.S.set(reg.N.get() ^ reg.V.get());
						reg.Z.set(((R & 0xff) == 0) ? 1 : 0);
						reg.C.set((not_Rd7 & Rr7) | (Rr7 & R7) | (R7 & not_Rd7));
						this.FlagsSub[(C << 16) | (Rd << 8) | Rr] = reg.get();
					}
		},

		InitHandlerMap: function() {
			for (var i = 0; i < this.OpCodes.length; i++)
				this.InitOpCode(this.OpCodes[i]);
		},

		InitOpCode: function(opCode) {
			for (var i = 0; i < opCode.BitStrings.length; i++) {
				var bitString = opCode.BitStrings[i]
				while (bitString.indexOf(" ", 0) > 0)
					bitString = bitString.replace(" ", "");
				this.InitHandler(0, 0, bitString, opCode.Handler, opCode);
			}
		},

		InitHandler: function(bitNum, value, bits, handler, attrib) {
			if (bitNum == 16) {
				if (this.OpCodeMap[value] == null) {
					this.OpCodeMap[value] = handler;
					this.PCModifyMap[value] = attrib.ModifiesPC;
					this.OpCodeSizes[value] = attrib.OpCodeSize;
					this.OpCodeAttribs[value] = attrib;
				}
				return;
			}
			switch (bits[(15 - bitNum)]) {
				case '0':
					this.InitHandler(bitNum + 1, value, bits, handler, attrib);
					break;

				case '1':
					this.InitHandler(bitNum + 1, value + (1 << bitNum), bits, handler, attrib);
					break;

				default:
					this.InitHandler(bitNum + 1, value, bits, handler, attrib);
					this.InitHandler(bitNum + 1, value + (1 << bitNum), bits, handler, attrib);
					break;
			}
		},

		ReadRamWord: function(address) {
			var lo = AtmelContext.RAM[address].get();
			var hi = AtmelContext.RAM[(address + 1)].get();
			return hi * 256 + lo;
		},

		RunTo: function(lastCycle) {
			do {
				// if any timers or interrupts have triggered then go take care of them
				if (AtmelContext.Clock >= AtmelContext.NextTimerEvent)
					this.UpdateTimers();
				if (AtmelContext.InterruptPending && this.CheckInterrupts()) {
					AtmelContext.UpdateInterruptFlags();
					continue;
				}

				// get the current op code and call its handler
				for (var i=0; i<64; i++)
					this.InstrTable[AtmelContext.PC]();

			} while (AtmelContext.Clock < lastCycle);
		},

		CheckInterrupts: function() {
			// make sure interrupts are enabled
			if (AtmelContext.SREG.I.get() == 0)
				return false;

			// is timer 1 compare signalling an interrupt?
			if (AtmelContext.Timer1OutputCompareFlag) {
				// yes, make sure the timer0 interupt flag has been set
				if (AtmelContext.RAM[AtmelIO.TIFR1].get_bit(AtmelIO.TOV1) != 0) {
					// generate an interrupt.
					AtmelContext.RAM[AtmelContext.SP.get()].set(AtmelContext.PC & 0xff);
					AtmelContext.SP.set(AtmelContext.SP.get() - 1);
					AtmelContext.RAM[AtmelContext.SP.get()].set((AtmelContext.PC >> 8) & 0xff);
					AtmelContext.SP.set(AtmelContext.SP.get() - 1);
					AtmelContext.PC = this.TIM1_COMPA;
					AtmelContext.SREG.I.set(0);
					AtmelContext.RAM[AtmelIO.TIFR1].set_bit(AtmelIO.TOV1, 0);
					AtmelContext.Timer1OutputCompareFlag = false;
					AtmelContext.Clock += 4;
					return true;
				}
			}

			// is timer 1 overflow signalling an interrupt?
			else if (AtmelContext.Timer1OverflowFlag) {
				// yes, make sure the timer0 interupt flag has been set
				if (AtmelContext.RAM[AtmelIO.TIFR1].get_bit(AtmelIO.TOV1) != 0) {
					// generate an interrupt.
					AtmelContext.RAM[AtmelContext.SP.get()].set(AtmelContext.PC & 0xff);
					AtmelContext.SP.set(AtmelContext.SP.get() - 1);
					AtmelContext.RAM[AtmelContext.SP.get()].set((AtmelContext.PC >> 8) & 0xff);
					AtmelContext.SP.set(AtmelContext.SP.get() - 1);
					AtmelContext.PC = this.TIM1_OVF;
					AtmelContext.SREG.I.set(0);
					AtmelContext.RAM[AtmelIO.TIFR1].set_bit(AtmelIO.TOV1, 0);
					AtmelContext.Timer1OverflowFlag = false;
					AtmelContext.Clock += 4;
					return true;
				}
			}

			// is timer 0 signalling an interrupt?
			if (AtmelContext.Timer0OutputCompareFlag) {
				// yes, make sure the timer0 interupt flag has been set
				if (AtmelContext.RAM[AtmelIO.TIFR0].get_bit(AtmelIO.TOV0) != 0) {
					// generate an interrupt.
					AtmelContext.RAM[AtmelContext.SP.get()].set(AtmelContext.PC & 0xff);
					AtmelContext.SP.set(AtmelContext.SP.get() - 1);
					AtmelContext.RAM[AtmelContext.SP.get()].set((AtmelContext.PC >> 8) & 0xff);
					AtmelContext.SP.set(AtmelContext.SP.get() - 1);
					AtmelContext.PC = this.TIM0_OVF;
					AtmelContext.SREG.I.set(0);
					AtmelContext.RAM[AtmelIO.TIFR0].set_bit(AtmelIO.TOV0, 0);
					AtmelContext.Timer0OutputCompareFlag = false;
					AtmelContext.Clock += 4;
					return true;
				}
			}

			// is there a UART interrupt pending?
			if (AtmelContext.UDRE_InterruptPending) {
				// generate an interrupt
				AtmelContext.RAM[AtmelContext.SP.get()].set(AtmelContext.PC & 0xff);
				AtmelContext.SP.set(AtmelContext.SP.get() - 1);
				AtmelContext.RAM[AtmelContext.SP.get()].set((AtmelContext.PC >> 8) & 0xff);
				AtmelContext.SP.set(AtmelContext.SP.get() - 1);
				AtmelContext.PC = this.USART_UDRE;
				AtmelContext.SREG.I.set(0);
				USART.UCSR0A.set_bit(AtmelIO.TXC0, 0);
				AtmelContext.Clock += 4;
				return true;
			}

			// is there an SPI interrupt pending?
			if (AtmelContext.SPI_InterruptPending) {
				// yes, so generate an interrupt
				AtmelContext.RAM[AtmelContext.SP.get()].set(AtmelContext.PC & 0xff);
				AtmelContext.SP.set(AtmelContext.SP.get() - 1);
				AtmelContext.RAM[AtmelContext.SP.get()].set((AtmelContext.PC >> 8) & 0xff);
				AtmelContext.SP.set(AtmelContext.SP.get() - 1);
				AtmelContext.PC = this.SPI_STC;
				AtmelContext.SREG.I.set(0);
				SPI.SPCR.set_bit(AtmelIO.SPIF, 0);
				AtmelContext.Clock += 4;
				return true;
			}

			return false;
		},

		UpdateTimers: function() {
			var cycles = (AtmelContext.Clock - AtmelContext.LastTimerUpdate);
			if (cycles < 0)
				cycles = 0;
			AtmelContext.NextTimerEvent = AtmelContext.Clock + this.ClockSpeed; // minimum 1 second update for timers
			var clock = this.ClockTable[AtmelContext.RAM[AtmelIO.CLKPR].get()];

			// timer 0
			var clockSelect0 = this.ClockSelectTable[AtmelContext.RAM[AtmelIO.TCCR0B].get() & 7];
			var div0 = clock * clockSelect0;
			if (div0 > 0) {
				AtmelContext.Timer0 += cycles;
				var wgm0 = (AtmelContext.RAM[AtmelIO.TCCR0A].get() & 0x03) | ((AtmelContext.RAM[AtmelIO.TCCR0B].get() >> 1) & 0x04);
				var tcnt0 = AtmelContext.RAM[AtmelIO.TCNT0].get();
				tcnt0 += Math.floor(AtmelContext.Timer0 / div0);
				AtmelContext.Timer0 %= div0;
				if (tcnt0 >= 0x100) {
					AtmelContext.RAM[AtmelIO.TIFR0].set_bit(AtmelIO.TOV0, 1);
					AtmelContext.Timer0OutputCompareFlag = true;
					AtmelContext.InterruptPending = true;
					tcnt0 = tcnt0 & 0xff;
				}
				AtmelContext.RAM[AtmelIO.TCNT0].set(tcnt0);
				AtmelContext.NextTimerEvent = Math.min(AtmelContext.NextTimerEvent, div0 * (0x100 - tcnt0));
			}

			// timer 1
			var clockSelect1 = this.ClockSelectTable[AtmelContext.RAM[AtmelIO.TCCR1B].get() & 7];
			var div1 = clock * clockSelect1;
			if (div1 > 0) {
				// ctc
				AtmelContext.Timer1 += cycles;
				var wgm = (AtmelContext.RAM[AtmelIO.TCCR1A].get() & 0x03) | ((AtmelContext.RAM[AtmelIO.TCCR1B].get() >> 1) & 0x0C);
				switch (wgm) {
					// normal mode
					case 0:
					case 8: // todo: this is wrong, give mode 8 its own handler and implement it properly
						{
							var tcnt1h = AtmelContext.RAM[AtmelIO.TCNT1H].get();
							var tcnt1l = AtmelContext.RAM[AtmelIO.TCNT1L].get();
							var tcnt1 = tcnt1h * 256 + tcnt1l;
							tcnt1 += Math.floor(AtmelContext.Timer1 / div1);
							AtmelContext.Timer1 %= div1;
							if (tcnt1 >= 0x10000) {
								AtmelContext.RAM[AtmelIO.TIFR1].set_bit(AtmelIO.TOV1, 1);
								AtmelContext.Timer1OverflowFlag = true;
								AtmelContext.InterruptPending = true;
								tcnt1 = tcnt1 & 0xffff;
							}
							AtmelContext.RAM[AtmelIO.TCNT1H].set(tcnt1 / 256);
							AtmelContext.RAM[AtmelIO.TCNT1L].set(tcnt1 % 256);
							AtmelContext.NextTimerEvent = Math.min(AtmelContext.NextTimerEvent, div1 * (0x10000 - tcnt1));
						}
						break;

						// Phase correct PWM mode
					case 1:
					case 2:
					case 3:
					case 10:
					case 11:
						{
							// todo: implement this
						}
						break;

						// Fast PWM Mode
					case 5:
					case 6:
					case 7:
					case 14:
					case 15:
						{
							// todo: implement this
						}
						break;

						// Phase and frequency correct PWM
						//case 8:
						//{
						//	// todo: implement this
						//}
						//break;

						// Clear Timer on Compare Match (CTC) Mode
					case 4:
					case 12:
						{
							var tcnt1h = AtmelContext.RAM[AtmelIO.TCNT1H].get();
							var tcnt1l = AtmelContext.RAM[AtmelIO.TCNT1L].get();
							var tcnt1 = tcnt1h * 256 + tcnt1l;
							tcnt1 += Math.floor(AtmelContext.Timer1 / div1);
							AtmelContext.Timer1 %= div1;
							var OCR1A = AtmelContext.RAM[AtmelIO.OCR1AH].get() * 256 + AtmelContext.RAM[AtmelIO.OCR1AL].get();
							if (tcnt1 >= OCR1A) {
								AtmelContext.RAM[AtmelIO.TIFR1].set_bit(AtmelIO.TOV1, 1);
								if (OCR1A == 280) {
									AtmelContext.Timer1OutputCompareFlag = true;
									AtmelContext.InterruptPending = true;
								}
								if (OCR1A > 0)
									tcnt1 %= OCR1A;
							}
							AtmelContext.RAM[AtmelIO.TCNT1H].set(tcnt1 / 256);
							AtmelContext.RAM[AtmelIO.TCNT1L].set(tcnt1 % 256);
							if (OCR1A > 0)
								AtmelContext.NextTimerEvent = Math.min(AtmelContext.NextTimerEvent, div1 * (OCR1A - tcnt1));
						}
						break;

				}
			}

			AtmelContext.NextTimerEvent += AtmelContext.Clock;
			AtmelContext.LastTimerUpdate = AtmelContext.Clock;
		},

		InitOpCodes: function() {
			var self = this;
			self.OpCodes =
			[
				// ADC
				{
					OpCodeSize: 1,
					ModifiesPC: false,
					BitStrings: ["0001 11rd dddd rrrr"],
					Handler: function() {
						var opcode = AtmelContext.Flash[AtmelContext.PC];
						var d = (opcode >> 4) & 0x1f;
						var Rd = AtmelContext.R[d];
						var Rr = AtmelContext.R[(opcode & 0x0f) + ((opcode >> 5) & 0x10)];
						var C = AtmelContext.SREG.C.get();
						AtmelContext.Flags = (AtmelContext.Flags & 0xc0 /*self.FlagsAddMask*/) | self.FlagsAdd[(C << 16) | (Rd << 8) | Rr];
						AtmelContext.R[d] = (Rd + Rr + C) & 0xff;
						AtmelContext.Clock++;
						AtmelContext.PC++;
					}
				},

				// ADD
				{
					OpCodeSize: 1,
					ModifiesPC: false,
					BitStrings: ["0000 11rd dddd rrrr"],
					Handler: function() {
						var opcode = AtmelContext.Flash[AtmelContext.PC];
						var d = (opcode >> 4) & 0x1f;
						var Rd = AtmelContext.R[d];
						var r = (opcode & 0x0f) + ((opcode >> 5) & 0x10);
						var Rr = AtmelContext.R[r];
						AtmelContext.Flags = (AtmelContext.Flags & 0xc0 /*self.FlagsAddMask*/) | self.FlagsAdd[(Rd << 8) | Rr];
						var R = Rd + Rr;
						AtmelContext.R[d] = R & 0xff;
						AtmelContext.Clock++;
						AtmelContext.PC++;
					}
				},

				// ADIW
				{
					OpCodeSize: 1,
					ModifiesPC: false,
					BitStrings: ["1001 0110 KKdd KKKK"],
					Handler: function() {
						var opcode = AtmelContext.Flash[AtmelContext.PC];
						var d = ((opcode >> 4) & 0x03);
						var K = (opcode & 0x0f) + ((opcode >> 2) & 0x30);
						var lo = 24 + (d << 1);
						var hi = lo + 1;
						var Rd = (AtmelContext.R[hi] << 8) | AtmelContext.R[lo];
						var R = Rd + K;
						var Rdh7 = (Rd >> 15) & 1;
						var not_Rdh7 = 1 - Rdh7;
						var R15 = (R >> 15) & 1;
						var not_R15 = 1 - R15;
						AtmelContext.SREG.N.set(R15);
						AtmelContext.SREG.V.set(not_Rdh7 & R15);
						AtmelContext.SREG.S.set(AtmelContext.SREG.N.get() ^ AtmelContext.SREG.V.get());
						AtmelContext.SREG.Z.set(((R & 0xffff) == 0) ? 1 : 0);
						AtmelContext.SREG.C.set(not_R15 & Rdh7);
						AtmelContext.R[lo] = R & 0xff;
						AtmelContext.R[hi] = (R >> 8) & 0xff;
						AtmelContext.Clock += 2;
						AtmelContext.PC++;
					}
				},

				// AND
				{
					OpCodeSize: 1,
					ModifiesPC: false,
					BitStrings: ["0010 00rd dddd rrrr"],
					Handler: function() {
						var opcode = AtmelContext.Flash[AtmelContext.PC];
						var d = (opcode >> 4) & 0x1f;
						var r = (opcode & 0x0f) + ((opcode >> 5) & 0x10);
						var R = AtmelContext.R[d] & AtmelContext.R[r];
						AtmelContext.SREG.Z.set(((R & 0xff) == 0) ? 1 : 0);
						AtmelContext.SREG.N.set((R >> 7) & 1);
						AtmelContext.SREG.V.set(0);
						AtmelContext.SREG.S.set(AtmelContext.SREG.N.get() ^ AtmelContext.SREG.V.get());
						AtmelContext.R[d] = R & 0xff;
						AtmelContext.Clock++;
						AtmelContext.PC++;
					}
				},

				// ANDI
				{
					OpCodeSize: 1,
					ModifiesPC: false,
					BitStrings: ["0111 KKKK dddd KKKK"],
					Handler: function() {
						var opcode = AtmelContext.Flash[AtmelContext.PC];
						var d = 16 + ((opcode >> 4) & 0x0f);
						var K = (opcode & 0x0f) + ((opcode >> 4) & 0xf0);
						var R = AtmelContext.R[d] & K;
						AtmelContext.SREG.Z.set(((R & 0xff) == 0) ? 1 : 0);
						AtmelContext.SREG.N.set((R >> 7) & 1);
						AtmelContext.SREG.V.set(0);
						AtmelContext.SREG.S.set(AtmelContext.SREG.N.get() ^ AtmelContext.SREG.V.get());
						AtmelContext.R[d] = R & 0xff;
						AtmelContext.Clock++;
						AtmelContext.PC++;
					}
				},

				// ASR
				{
					OpCodeSize: 1,
					ModifiesPC: false,
					BitStrings: ["1001 010d dddd 0101"],
					Handler: function() {
						var opcode = AtmelContext.Flash[AtmelContext.PC];
						var d = ((opcode >> 4) & 0x1f);
						var Rd = AtmelContext.R[d];
						var R = (Rd < 128) ? Rd : -(0x100 - Rd);
						R >>= 1;
						AtmelContext.SREG.Z.set(((R & 0xff) == 0) ? 1 : 0);
						AtmelContext.SREG.N.set((R >> 7) & 1);
						AtmelContext.SREG.C.set(Rd & 1);
						AtmelContext.SREG.S.set(AtmelContext.SREG.N.get() ^ AtmelContext.SREG.V.get());
						AtmelContext.SREG.V.set(AtmelContext.SREG.N.get() ^ AtmelContext.SREG.C.get());
						AtmelContext.R[d] = R & 0xff;
						AtmelContext.Clock++;
						AtmelContext.PC++;
					}
				},

				// BCLR
				{
					OpCodeSize: 1,
					ModifiesPC: false,
					BitStrings: ["1001 0100 1sss 1000"],
					Handler: function() {
						var opcode = AtmelContext.Flash[AtmelContext.PC];
						var s = (opcode >> 4) & 0x07;
						AtmelContext.Flags = (AtmelContext.Flags & ~(1 << s)) & 0xff;
						AtmelContext.Clock++;
						AtmelContext.PC++;
					}
				},

				// BLD
				{
					OpCodeSize: 1,
					ModifiesPC: false,
					BitStrings: ["1111 100d dddd 0bbb"],
					Handler: function() {
						var opcode = AtmelContext.Flash[AtmelContext.PC];
						var d = (opcode >> 4) & 0x1f;
						var b = opcode & 0x07;
						if (AtmelContext.SREG.T.get() == 0)
							AtmelContext.R[d] = AtmelContext.R[d] & ~(1 << b);
						else
							AtmelContext.R[d] = AtmelContext.R[d] | (1 << b);
						AtmelContext.Clock++;
						AtmelContext.PC++;
					}
				},

				// BRBC
				{
					OpCodeSize: 1,
					ModifiesPC: true,
					BitStrings: ["1111 01kk kkkk ksss"],
					Handler: function() {
						// no flags set
						var opcode = AtmelContext.Flash[AtmelContext.PC];
						if (AtmelContext.SREG.get_bit(opcode & 7) == 0) {
							var k = (opcode >> 3) & 0x7f;
							k = (k <= 0x3f) ? k : k - 0x80;
							AtmelContext.PC += k + 1;
							AtmelContext.Clock += 2;
						}
						else {
							AtmelContext.PC++;
							AtmelContext.Clock++;
						}
					}
				},

				// BRBS
				{
					OpCodeSize: 1,
					ModifiesPC: true,
					BitStrings: ["1111 00kk kkkk ksss"],
					Handler: function() {
						// no flags set
						var opcode = AtmelContext.Flash[AtmelContext.PC];
						var s = opcode & 7;
						if (AtmelContext.SREG.get_bit(s) != 0) {
							var k = (opcode >> 3) & 0x7f;
							k = (k <= 0x3f) ? k : k - 0x80;
							AtmelContext.PC += k + 1;
							AtmelContext.Clock += 2;
						}
						else {
							AtmelContext.PC++;
							AtmelContext.Clock++;
						}
					}
				},

				// BREAK
				{
					OpCodeSize: 1,
					ModifiesPC: false,
					BitStrings: ["1001 0101 1001 1000"],
					Handler: function() {
						// not implemented
						AtmelContext.Clock++;
						AtmelContext.PC++;
					}
				},

				// BSET
				{
					OpCodeSize: 1,
					ModifiesPC: false,
					BitStrings: ["1001 0100 0sss 1000"],
					Handler: function() {
						var opcode = AtmelContext.Flash[AtmelContext.PC];
						var s = (opcode >> 4) & 0x07;
						AtmelContext.Flags = (AtmelContext.Flags | (1 << s)) & 0xff;
						AtmelContext.Clock++;
						AtmelContext.PC++;
					}
				},

				// BST
				{
					OpCodeSize: 1,
					ModifiesPC: false,
					BitStrings: ["1111 101d dddd 0bbb"],
					Handler: function() {
						var opcode = AtmelContext.Flash[AtmelContext.PC];
						var d = (opcode >> 4) & 0x1f;
						var b = opcode & 0x07;
						AtmelContext.SREG.T.set((AtmelContext.R[d] >> b) & 1);
						AtmelContext.Clock++;
						AtmelContext.PC++;
					}
				},

				// CALL
				{
					OpCodeSize: 2,
					ModifiesPC: true,
					BitStrings: ["1001 010k kkkk 111k"],
					Handler: function() {
						// calculate the address we're jumping to
						var opcode = AtmelContext.Flash[AtmelContext.PC];
						var addr = AtmelContext.Flash[AtmelContext.PC + 1];
						addr += (opcode & 1) << 8;
						addr += ((opcode >> 4) & 0x1f) << 9;

						// push next PC onto the stack
						var nextPC = AtmelContext.PC + 2;
						AtmelContext.RAM[AtmelContext.SP.get()].set((nextPC >> 8) & 0xff);
						AtmelContext.RAM[AtmelContext.SP.get() - 1].set(nextPC);
						AtmelContext.SP.set((AtmelContext.SP.get() - 2) & 0xffff);

						// no flags set
						AtmelContext.PC = addr;
						AtmelContext.Clock += 4;
					}
				},

				// CBI
				{
					OpCodeSize: 1,
					ModifiesPC: false,
					BitStrings: ["1001 1000 AAAA Abbb"],
					Handler: function() {
						var opcode = AtmelContext.Flash[AtmelContext.PC];
						var b = opcode & 0x07;
						var A = (opcode >> 3) & 0x1f;
						AtmelContext.IO[A].set(AtmelContext.IO[A].get() & ~(1 << b));
						AtmelContext.Clock++;
						AtmelContext.PC++;
					}
				},

				// COM
				{
					OpCodeSize: 1,
					ModifiesPC: false,
					BitStrings: ["1001 010d dddd 0000"],
					Handler: function() {
						var opcode = AtmelContext.Flash[AtmelContext.PC];
						var d = (opcode >> 4) & 0x1f;
						var R = 0xff - AtmelContext.R[d];
						AtmelContext.SREG.N.set((R >> 7) & 1);
						AtmelContext.SREG.V.set(0);
						AtmelContext.SREG.S.set(AtmelContext.SREG.N.get() ^ AtmelContext.SREG.V.get());
						AtmelContext.SREG.Z.set(((R & 0xff) == 0) ? 1 : 0);
						AtmelContext.SREG.C.set(1);
						AtmelContext.R[d] = R & 0xff;
						AtmelContext.Clock++;
						AtmelContext.PC++;
					}
				},

				// CP
				{
					OpCodeSize: 1,
					ModifiesPC: false,
					BitStrings: ["0001 01rd dddd rrrr"],
					Handler: function() {
						var opcode = AtmelContext.Flash[AtmelContext.PC];
						var d = (opcode >> 4) & 0x1f;
						var Rd = AtmelContext.R[d];
						var r = (opcode & 0x0f) + ((opcode >> 5) & 0x10);
						var Rr = AtmelContext.R[r];
						AtmelContext.Flags = (AtmelContext.Flags & 0xc0 /*self.FlagsSubMask*/) | self.FlagsSub[(Rd << 8) | Rr];
						AtmelContext.Clock++;
						AtmelContext.PC++;
					}
				},

				// CPC
				{
					OpCodeSize: 1,
					ModifiesPC: false,
					BitStrings: ["0000 01rd dddd rrrr"],
					Handler: function() {
						var opcode = AtmelContext.Flash[AtmelContext.PC];
						var d = (opcode >> 4) & 0x1f;
						var Rd = AtmelContext.R[d];
						var r = (opcode & 0x0f) + ((opcode >> 5) & 0x10);
						var Rr = AtmelContext.R[r];
						var C = AtmelContext.SREG.C.get();
						var R = Rd - Rr - AtmelContext.SREG.C.get();
						var Z = AtmelContext.SREG.Z.get();
						AtmelContext.Flags = (AtmelContext.Flags & 0xc0 /*self.FlagsSubMask*/) | self.FlagsSub[(C << 16) | (Rd << 8) | Rr];
						AtmelContext.SREG.Z.set((R == 0) ? Z : 0);
						AtmelContext.Clock++;
						AtmelContext.PC++;
					}
				},

				// CPI
				{
					OpCodeSize: 1,
					ModifiesPC: false,
					BitStrings: ["0011 KKKK dddd KKKK"],
					Handler: function() {
						var opcode = AtmelContext.Flash[AtmelContext.PC];
						var d = 16 + ((opcode >> 4) & 0x0f);
						var K = (opcode & 0x0f) + ((opcode >> 4) & 0xf0);
						var Rd = AtmelContext.R[d];
						AtmelContext.Flags = (AtmelContext.Flags & 0xc0 /*self.FlagsSubMask*/) | self.FlagsSub[(Rd << 8) | K];
						AtmelContext.Clock++;
						AtmelContext.PC++;
					}
				},

				// CPSE
				{
					OpCodeSize: 1,
					ModifiesPC: true,
					BitStrings: ["0001 00rd dddd rrrr"],
					Handler: function() {
						// no flags set
						var opcode = AtmelContext.Flash[AtmelContext.PC];
						var d = (opcode >> 4) & 0x1f;
						var r = (opcode & 0x0f) + ((opcode >> 5) & 0x10);
						AtmelContext.PC++;
						if (AtmelContext.R[d] == AtmelContext.R[r]) {
							// skip
							var nextOpCode = AtmelContext.Flash[AtmelContext.PC];
							var nextOpCodeSize = self.OpCodeSizes[nextOpCode];
							AtmelContext.PC += nextOpCodeSize;
							AtmelContext.Clock += 1 + nextOpCodeSize;
						}
						else {
							// no skip
							AtmelContext.Clock++;
						}
					}
				},

				// DEC
				{
					OpCodeSize: 1,
					ModifiesPC: false,
					BitStrings: ["1001 010d dddd 1010"],
					Handler: function() {
						var opcode = AtmelContext.Flash[AtmelContext.PC];
						var d = (opcode >> 4) & 0x1f;
						var R = AtmelContext.R[d] - 1;
						AtmelContext.SREG.V.set((AtmelContext.R[d] == 0x80) ? 1 : 0);
						AtmelContext.SREG.N.set((R >> 7) & 1);
						AtmelContext.SREG.S.set(AtmelContext.SREG.N.get() ^ AtmelContext.SREG.V.get());
						AtmelContext.SREG.Z.set(((R & 0xff) == 0) ? 1 : 0);
						AtmelContext.R[d] = R & 0xff;
						AtmelContext.Clock++;
						AtmelContext.PC++;
					}
				},

				// DES
				{
					OpCodeSize: 1,
					ModifiesPC: false,
					BitStrings: ["1001 0100 KKKK 1011"],
					Handler: function() {
						// not implemented
						AtmelContext.Clock++;
						AtmelContext.PC++;
					}
				},

				// EICALL
				{
					OpCodeSize: 1,
					ModifiesPC: false,
					BitStrings: ["1001 0101 0001 1001"],
					Handler: function() {
						// not implemented
						AtmelContext.Clock++;
						AtmelContext.PC++;
					}
				},

				// EIJMP
				{
					OpCodeSize: 1,
					ModifiesPC: false,
					BitStrings: ["1001 0100 0001 1001"],
					Handler: function() {
						// not implemented
						AtmelContext.Clock++;
						AtmelContext.PC++;
					}
				},

				// ELPM1
				{
					OpCodeSize: 1,
					ModifiesPC: false,
					BitStrings: ["1001 0101 1101 1000"],
					Handler: function() {
						// not implemented
						AtmelContext.Clock++;
						AtmelContext.PC++;
					}
				},

				// ELPM2
				{
					OpCodeSize: 1,
					ModifiesPC: false,
					BitStrings: ["1001 000d dddd 0110"],
					Handler: function() {
						// not implemented
						AtmelContext.Clock++;
						AtmelContext.PC++;
					}
				},

				// ELPM3
				{
					OpCodeSize: 1,
					ModifiesPC: false,
					BitStrings: ["1001 000d dddd 0111"],
					Handler: function() {
						// not implemented
						AtmelContext.Clock++;
						AtmelContext.PC++;
					}
				},

				// EOR
				{
					OpCodeSize: 1,
					ModifiesPC: false,
					BitStrings: ["0010 01rd dddd rrrr"],
					Handler: function() {
						var opcode = AtmelContext.Flash[AtmelContext.PC];
						var d = (opcode >> 4) & 0x1f;
						var r = (opcode & 0x0f) + ((opcode >> 5) & 0x10);
						var R = AtmelContext.R[d] ^ AtmelContext.R[r];
						AtmelContext.SREG.Z.set(((R & 0xff) == 0) ? 1 : 0);
						AtmelContext.SREG.N.set((R >> 7) & 1);
						AtmelContext.SREG.V.set(0);
						AtmelContext.SREG.S.set(AtmelContext.SREG.N.get());
						AtmelContext.R[d] = R & 0xff;
						AtmelContext.Clock++;
						AtmelContext.PC++;
					}
				},

				// FMUL
				{
					OpCodeSize: 1,
					ModifiesPC: false,
					BitStrings: ["0000 0011 0ddd 1rrr"],
					Handler: function() {
						// not implemented
						AtmelContext.Clock++;
						AtmelContext.PC++;
					}
				},

				// FMULS
				{
					OpCodeSize: 1,
					ModifiesPC: false,
					BitStrings: ["0000 0011 1ddd 0rrr"],
					Handler: function() {
						// not implemented
						AtmelContext.Clock++;
						AtmelContext.PC++;
					}
				},

				// ICALL
				{
					OpCodeSize: 1,
					ModifiesPC: true,
					BitStrings: ["1001 0101 0000 1001"],
					Handler: function() {
						// calculate the address we're jumping to
						var addr = AtmelContext.Z.get();

						// push next PC onto the stack
						var nextPC = AtmelContext.PC + 1;
						AtmelContext.RAM[AtmelContext.SP.get()].set((nextPC >> 8) & 0xff);
						AtmelContext.RAM[AtmelContext.SP.get() - 1].set(nextPC);
						AtmelContext.SP.set((AtmelContext.SP.get() - 2) & 0xffff);

						// no flags set
						AtmelContext.PC = addr;
						AtmelContext.Clock += 3;
					}
				},

				// IJMP
				{
					OpCodeSize: 1,
					ModifiesPC: true,
					BitStrings: ["1001 0100 0000 1001"],
					Handler: function() {
						AtmelContext.PC = AtmelContext.Z.get();
						AtmelContext.Clock += 2;
					}
				},

				// IN
				{
					OpCodeSize: 1,
					ModifiesPC: false,
					BitStrings: ["1011 0AAd dddd AAAA"],
					Handler: function() {
						// no flags set
						var opcode = AtmelContext.Flash[AtmelContext.PC];
						var A = (opcode & 0x0f) + ((opcode & 0x600) >> 5);
						var r = ((opcode >> 4) & 0x01f);
						AtmelContext.R[r] = AtmelContext.IO[A].get();
						AtmelContext.Clock++;
						AtmelContext.PC++;
					}
				},

				// INC
				{
					OpCodeSize: 1,
					ModifiesPC: false,
					BitStrings: ["1001 010d dddd 0011"],
					Handler: function() {
						var opcode = AtmelContext.Flash[AtmelContext.PC];
						var d = (opcode >> 4) & 0x1f;
						var R = AtmelContext.R[d] + 1;
						AtmelContext.SREG.V.set((AtmelContext.R[d] == 0x7f) ? 1 : 0);
						AtmelContext.SREG.N.set((R >> 7) & 1);
						AtmelContext.SREG.S.set(AtmelContext.SREG.N.get() ^ AtmelContext.SREG.V.get());
						AtmelContext.SREG.Z.set(((R & 0xff) == 0) ? 1 : 0);
						AtmelContext.R[d] = R & 0xff;
						AtmelContext.Clock++;
						AtmelContext.PC++;
					}
				},

				// JMP
				{
					OpCodeSize: 2,
					ModifiesPC: true,
					BitStrings: ["1001 010k kkkk 110k"],
					Handler: function() {
						// no flags set
						var opcode = AtmelContext.Flash[AtmelContext.PC];
						var addr = AtmelContext.Flash[AtmelContext.PC + 1];
						addr += (opcode & 1) << 8;
						addr += ((opcode >> 4) & 0x1f) << 9;
						AtmelContext.PC = addr;
						AtmelContext.Clock += 3;
					}
				},

				// LAC
				{
					OpCodeSize: 1,
					ModifiesPC: false,
					BitStrings: ["1001 001r rrrr 0110"],
					Handler: function() {
						// not implemented
						AtmelContext.Clock++;
						AtmelContext.PC++;
					}
				},

				// LAS
				{
					OpCodeSize: 1,
					ModifiesPC: false,
					BitStrings: ["1001 001r rrrr 0101"],
					Handler: function() {
						// not implemented
						AtmelContext.Clock++;
						AtmelContext.PC++;
					}
				},

				// LAT
				{
					OpCodeSize: 1,
					ModifiesPC: false,
					BitStrings: ["1001 001r rrrr 0111"],
					Handler: function() {
						// not implemented
						AtmelContext.Clock++;
						AtmelContext.PC++;
					}
				},

				// LD_X
				{
					OpCodeSize: 1,
					ModifiesPC: false,
					BitStrings: ["1001 000d dddd 1100", "1001 000d dddd 1101", "1001 000d dddd 1110"],
					Handler: function() {
						// todo: timings in self function need to be fixed
						var opcode = AtmelContext.Flash[AtmelContext.PC];
						var d = (opcode >> 4) & 0x1f;
						switch (opcode & 0x0f) {
							case 0x0c:			// (i) unchanged
								AtmelContext.R[d] = AtmelContext.RAM[AtmelContext.X.get()].get();
								AtmelContext.Clock++;
								AtmelContext.PC++;
								return;

							case 0x0d:			// (ii) post-increment
								AtmelContext.R[d] = AtmelContext.RAM[AtmelContext.X.get()].get();
								AtmelContext.X.set((AtmelContext.X.get() + 1) & 0xffff);
								AtmelContext.Clock += 2;
								AtmelContext.PC++;
								return;

							case 0x0e:			// (iii) pre-decrement
								AtmelContext.X.set((AtmelContext.X.get() - 1) & 0xffff);
								AtmelContext.R[d] = AtmelContext.RAM[AtmelContext.X.get()].get();
								AtmelContext.Clock += 3;
								AtmelContext.PC++;
								return;

							default:
								AtmelContext.PC++;
						}
					}
				},

				// LD_Y
				{
					OpCodeSize: 1,
					ModifiesPC: false,
					BitStrings: ["1000 000d dddd 1000", "1001 000d dddd 1001", "1001 000d dddd 1010", "10q0 qq0d dddd 1qqq"],
					Handler: function() {
						var opcode = AtmelContext.Flash[AtmelContext.PC];
						var d = (opcode >> 4) & 0x1f;
						var q = ((opcode >> 8) & 0x20) | ((opcode >> 7) & 0x18) | (opcode & 0x07);
						switch (opcode & 0x1007) {
							case 0x1001:			// (ii) post-increment
								AtmelContext.R[d] = AtmelContext.RAM[AtmelContext.Y.get()].get();
								AtmelContext.Y.set((AtmelContext.Y.get() + 1) & 0xffff);
								break;

							case 0x1002:			// (iii) pre-decrement
								AtmelContext.Y.set((AtmelContext.Y.get() - 1) & 0xffff);
								AtmelContext.R[d] = AtmelContext.RAM[AtmelContext.Y.get()].get();
								break;

							default:			// (i) unchanged and (iv) unchanged with q displacement
								AtmelContext.R[d] = AtmelContext.RAM[AtmelContext.Y.get() + q].get();
								break;
						}
						AtmelContext.Clock += 2;
						AtmelContext.PC++;
					}
				},

				// LD_Z
				{
					OpCodeSize: 1,
					ModifiesPC: false,
					BitStrings: ["1000 000d dddd 0000", "1001 000d dddd 0001", "1001 000d dddd 0010", "10q0 qq0d dddd 0qqq"],
					Handler: function() {
						// todo: timings in self function need to be fixed
						var opcode = AtmelContext.Flash[AtmelContext.PC];
						var d = (opcode >> 4) & 0x1f;
						var q = ((opcode >> 8) & 0x20) | ((opcode >> 7) & 0x18) | (opcode & 0x07);
						switch (opcode & 0x1007) {
							case 0x1001:			// (ii) post-increment
								AtmelContext.R[d] = AtmelContext.RAM[AtmelContext.Z.get()].get();
								AtmelContext.Z.set((AtmelContext.Z.get() + 1) & 0xffff);
								break;

							case 0x1002:			// (iii) pre-decrement
								AtmelContext.Z.set((AtmelContext.Z.get() - 1) & 0xffff);
								AtmelContext.R[d] = AtmelContext.RAM[AtmelContext.Z.get()].get();
								break;

							default:			// // (i) unchanged and (iv) unchanged with q displacement
								AtmelContext.R[d] = AtmelContext.RAM[(AtmelContext.Z.get() + q)].get();
								break;
						}
						AtmelContext.Clock += 2;
						AtmelContext.PC++;
					}
				},

				// LDI
				{
					OpCodeSize: 1,
					ModifiesPC: false,
					BitStrings: ["1110 KKKK dddd KKKK"],
					Handler: function() {
						// no flags set
						var opcode = AtmelContext.Flash[AtmelContext.PC];
						var d = 16 + ((opcode >> 4) & 0x0f);
						var K = (opcode & 0x0f) + ((opcode >> 4) & 0xf0);
						AtmelContext.R[d] = K & 0xff;
						AtmelContext.Clock++;
						AtmelContext.PC++;
					}
				},

				// LDS
				{
					OpCodeSize: 2,
					ModifiesPC: false,
					BitStrings: ["1001 000d dddd 0000"],
					Handler: function() {
						var opcode = AtmelContext.Flash[AtmelContext.PC];
						var d = (opcode >> 4) & 0x1f;
						var k = AtmelContext.Flash[(AtmelContext.PC + 1)];
						AtmelContext.R[d] = AtmelContext.RAM[k].get();
						AtmelContext.Clock += 2;
						AtmelContext.PC += 2;
					}
				},

				// LPM
				{
					OpCodeSize: 1,
					ModifiesPC: false,
					BitStrings: ["1001 0101 1100 1000", "1001 000d dddd 0100", "1001 000d dddd 0101"],
					Handler: function() {
						var opcode = AtmelContext.Flash[AtmelContext.PC];
						var d = (opcode >> 4) & 0x1f;
						var b = AtmelContext.Flash[AtmelContext.Z.get() >> 1];
						if ((AtmelContext.Z.get() & 1) == 1)
							b = (b >> 8) & 0xff;
						else
							b = b & 0xff;
						switch (opcode & 0x0f) {
							case 0x08:			// (i)
								AtmelContext.R[0] = b & 0xff;
								break;

							case 0x04:			// (ii)
								AtmelContext.R[d] = b & 0xff;
								break;

							case 0x05:			// (iii)
								AtmelContext.R[d] = b & 0xff;
								AtmelContext.Z.set((AtmelContext.Z.get() + 1) & 0xffff);
								break;

							default:
								throw new InvalidOperationException();
						}
						AtmelContext.Clock += 3;
						AtmelContext.PC++;
					}
				},

				// LSR
				{
					OpCodeSize: 1,
					ModifiesPC: false,
					BitStrings: ["1001 010d dddd 0110"],
					Handler: function() {
						var opcode = AtmelContext.Flash[AtmelContext.PC];
						var d = ((opcode >> 4) & 0x1f);
						var Rd = AtmelContext.R[d];
						var R = Rd >> 1;
						AtmelContext.SREG.Z.set(((R & 0xff) == 0) ? 1 : 0);
						AtmelContext.SREG.N.set(0);
						var C = Rd & 1;
						AtmelContext.SREG.C.set(C); // todo: double-check that these are all correct
						AtmelContext.SREG.V.set(C);
						AtmelContext.SREG.S.set(C);
						AtmelContext.R[d] = R & 0xff;
						AtmelContext.Clock++;
						AtmelContext.PC++;
					}
				},

				// MOV
				{
					OpCodeSize: 1,
					ModifiesPC: false,
					BitStrings: ["0010 11rd dddd rrrr"],
					Handler: function() {
						// no flags set
						var opcode = AtmelContext.Flash[AtmelContext.PC];
						var d = ((opcode >> 4) & 0x1f);
						var r = (opcode & 0x0f) + ((opcode >> 5) & 0x10);
						AtmelContext.R[d] = AtmelContext.R[r];
						AtmelContext.Clock++;
						AtmelContext.PC++;
					}
				},

				// MOVW
				{
					OpCodeSize: 1,
					ModifiesPC: false,
					BitStrings: ["0000 0001 dddd rrrr"],
					Handler: function() {
						// no flags set
						var opcode = AtmelContext.Flash[AtmelContext.PC];
						var d = 2 * ((opcode >> 4) & 0x0f);
						var r = 2 * (opcode & 0x0f);
						AtmelContext.R[d] = AtmelContext.R[r];
						AtmelContext.R[d + 1] = AtmelContext.R[r + 1];
						AtmelContext.Clock++;
						AtmelContext.PC++;
					}
				},

				// MUL
				{
					OpCodeSize: 1,
					ModifiesPC: false,
					BitStrings: ["1001 11rd dddd rrrr"],
					Handler: function() {
						var opcode = AtmelContext.Flash[AtmelContext.PC];
						var d = (opcode >> 4) & 0x1f;
						var r = (opcode & 0x0f) + ((opcode >> 5) & 0x10);
						var Rd = AtmelContext.R[d] & 0xff;
						var Rr = AtmelContext.R[r] & 0xff;
						var R = Rd * Rr;
						AtmelContext.R[0] = R & 0xff;
						AtmelContext.R[1] = (R >> 8) & 0xff;
						AtmelContext.SREG.C.set((R >> 15) & 1);
						AtmelContext.SREG.Z.set((R == 0) ? 1 : 0);
						AtmelContext.Clock += 2;
						AtmelContext.PC++;
					}
				},

				// MULS
				{
					OpCodeSize: 1,
					ModifiesPC: false,
					BitStrings: ["0000 0010 dddd rrrr"],
					Handler: function() {
						var opcode = AtmelContext.Flash[AtmelContext.PC];
						var d = 16 + ((opcode >> 4) & 0xf);
						var r = 16 + (opcode & 0x0f);
						var Rd = AtmelContext.R[d];
						Rd = (Rd < 128) ? Rd : -(0x100 - Rd);
						var Rr = AtmelContext.R[r];
						Rr = (Rr < 128) ? Rr : -(0x100 - Rr);
						var R = Rd * Rr;
						AtmelContext.R[0] = R & 0xff;
						AtmelContext.R[1] = (R >> 8) & 0xff;
						AtmelContext.SREG.C.set((R >> 15) & 1);
						AtmelContext.SREG.Z.set((R == 0) ? 1 : 0);
						AtmelContext.Clock += 2;
						AtmelContext.PC++;
					}
				},

				// MULSU
				{
					OpCodeSize: 1,
					ModifiesPC: false,
					BitStrings: ["0000 0011 0ddd 0rrr"],
					Handler: function() {
						var opcode = AtmelContext.Flash[AtmelContext.PC];
						var d = 16 + ((opcode >> 4) & 0x07);
						var r = 16 + ((opcode & 0x07));
						var Rd = AtmelContext.R[d];
						Rd = (Rd < 128) ? Rd : -(0x100 - Rd);
						var Rr = AtmelContext.R[r];
						Rr = (Rr < 128) ? Rr : -(0x100 - Rr);
						var R = Rd * Rr;
						AtmelContext.R[0] = R & 0xff;
						AtmelContext.R[1] = (R >> 8) & 0xff;
						AtmelContext.SREG.C.set((R >> 15) & 1);
						AtmelContext.SREG.Z.set((R == 0) ? 1 : 0);
						AtmelContext.Clock += 2;
						AtmelContext.PC++;
					}
				},

				// NEG
				{
					OpCodeSize: 1,
					ModifiesPC: false,
					BitStrings: ["1001 010d dddd 0001"],
					Handler: function() {
						var opcode = AtmelContext.Flash[AtmelContext.PC];
						var d = ((opcode >> 4) & 0x1f);
						var Rd = AtmelContext.R[d];
						var R = (-Rd) & 0xff;
						AtmelContext.SREG.H.set(((Rd & 0x08) | (R & 0x08)) == 0 ? 0 : 1);
						AtmelContext.SREG.V.set((R == 0x80) ? 1 : 0);
						AtmelContext.SREG.Z.set((R == 0) ? 1 : 0);
						AtmelContext.SREG.N.set((R >> 7) & 1);
						AtmelContext.SREG.C.set((R == 0) ? 0 : 1);
						AtmelContext.SREG.S.set(AtmelContext.SREG.N.get() ^ AtmelContext.SREG.V.get());
						AtmelContext.R[d] = R;
						AtmelContext.Clock++;
						AtmelContext.PC++;
					}
				},

				// NOP
				{
					OpCodeSize: 1,
					ModifiesPC: false,
					BitStrings: ["0000 0000 0000 0000"],
					Handler: function() {
						// no flags
						AtmelContext.Clock++;
						AtmelContext.PC++;
					}
				},

				// OR
				{
					OpCodeSize: 1,
					ModifiesPC: false,
					BitStrings: ["0010 10rd dddd rrrr"],
					Handler: function() {
						var opcode = AtmelContext.Flash[AtmelContext.PC];
						var d = (opcode >> 4) & 0x1f;
						var r = (opcode & 0x0f) + ((opcode >> 5) & 0x10);
						var R = AtmelContext.R[d] | AtmelContext.R[r];
						AtmelContext.SREG.Z.set(((R & 0xff) == 0) ? 1 : 0);
						AtmelContext.SREG.N.set((R >> 7) & 1);
						AtmelContext.SREG.V.set(0);
						AtmelContext.SREG.S.set(AtmelContext.SREG.N.get() ^ AtmelContext.SREG.V.get());
						AtmelContext.R[d] = R & 0xff;
						AtmelContext.Clock++;
						AtmelContext.PC++;
					}
				},

				// ORI
				{
					OpCodeSize: 1,
					ModifiesPC: false,
					BitStrings: ["0110 KKKK dddd KKKK"],
					Handler: function() {
						var opcode = AtmelContext.Flash[AtmelContext.PC];
						var d = 16 + ((opcode >> 4) & 0x0f);
						var K = (opcode & 0x0f) + ((opcode >> 4) & 0xf0);
						var R = AtmelContext.R[d] | (K & 0xff);
						AtmelContext.SREG.Z.set(((R & 0xff) == 0) ? 1 : 0);
						AtmelContext.SREG.N.set((R >> 7) & 1);
						AtmelContext.SREG.V.set(0);
						AtmelContext.SREG.S.set(AtmelContext.SREG.N.get() ^ AtmelContext.SREG.V.get());
						AtmelContext.R[d] = R & 0xff;
						AtmelContext.Clock++;
						AtmelContext.PC++;
					}
				},

				// OUT
				{
					OpCodeSize: 1,
					ModifiesPC: false,
					BitStrings: ["1011 1AAr rrrr AAAA"],
					Handler: function() {
						// no flags set
						var opcode = AtmelContext.Flash[AtmelContext.PC];
						var A = (opcode & 0x0f) + ((opcode & 0x600) >> 5);
						var r = ((opcode >> 4) & 0x01f);
						AtmelContext.IO[A].set(AtmelContext.R[r]);
						AtmelContext.Clock++;
						AtmelContext.PC++;
					}
				},

				// POP
				{
					OpCodeSize: 1,
					ModifiesPC: false,
					BitStrings: ["1001 000d dddd 1111"],
					Handler: function() {
						var opcode = AtmelContext.Flash[AtmelContext.PC];
						var d = (opcode >> 4) & 0x1f;
						AtmelContext.SP.set((AtmelContext.SP.get() + 1) & 0xffff);
						AtmelContext.R[d] = AtmelContext.RAM[AtmelContext.SP.get()].get();
						AtmelContext.Clock += 2;
						AtmelContext.PC++;
					}
				},

				// PUSH
				{
					OpCodeSize: 1,
					ModifiesPC: false,
					BitStrings: ["1001 001d dddd 1111"],
					Handler: function() {
						// no flags set
						var opcode = AtmelContext.Flash[AtmelContext.PC];
						var d = (opcode >> 4) & 0x1f;
						AtmelContext.RAM[AtmelContext.SP.get()].set(AtmelContext.R[d]);
						AtmelContext.SP.set((AtmelContext.SP.get() - 1) & 0xffff);
						AtmelContext.Clock += 2;
						AtmelContext.PC++;
					}
				},

				// RCALL
				{
					OpCodeSize: 1,
					ModifiesPC: true,
					BitStrings: ["1101 kkkk kkkk kkkk"],
					Handler: function() {
						// calculate the address we're jumping to
						var opcode = AtmelContext.Flash[AtmelContext.PC];
						var k = opcode & 0xfff;
						k = (k <= 0x7ff) ? k : k - 0x1000;
						var addr = AtmelContext.PC + k + 1;

						// push next PC onto the stack
						var nextPC = AtmelContext.PC + 1;
						AtmelContext.RAM[AtmelContext.SP.get()].set((nextPC >> 8) & 0xff);
						AtmelContext.RAM[AtmelContext.SP.get() - 1].set(nextPC);
						AtmelContext.SP.set((AtmelContext.SP.get() - 2) & 0xffff);

						// no flags set
						AtmelContext.PC = addr;
						AtmelContext.Clock += 3;
					}
				},

				// RET
				{
					OpCodeSize: 1,
					ModifiesPC: true,
					BitStrings: ["1001 0101 0000 1000"],
					Handler: function() {
						// no flags set
						AtmelContext.PC = self.ReadRamWord(AtmelContext.SP.get() + 1);
						AtmelContext.SP.set((AtmelContext.SP.get() + 2) & 0xffff);
						AtmelContext.Clock += 4;
					}
				},

				// RETI
				{
					OpCodeSize: 1,
					ModifiesPC: true,
					BitStrings: ["1001 0101 0001 1000"],
					Handler: function() {
						AtmelContext.SP.set(AtmelContext.SP.get() + 1);
						var hi = AtmelContext.RAM[AtmelContext.SP.get()].get();
						AtmelContext.SP.set(AtmelContext.SP.get() + 1);
						var lo = AtmelContext.RAM[AtmelContext.SP.get()].get();
						AtmelContext.PC = lo + hi * 256;
						AtmelContext.SREG.I.set(1);
						AtmelContext.Clock += 4;
					}
				},

				// RJMP
				{
					OpCodeSize: 1,
					ModifiesPC: true,
					BitStrings: ["1100 kkkk kkkk kkkk"],
					Handler: function() {
						// no flags set
						var opcode = AtmelContext.Flash[AtmelContext.PC];
						var k = opcode & 0xfff;
						k = (k <= 0x7ff) ? k : k - 0x1000;
						AtmelContext.PC = AtmelContext.PC + k + 1;
						AtmelContext.Clock += 2;
					}
				},

				// ROR
				{
					OpCodeSize: 1,
					ModifiesPC: false,
					BitStrings: ["1001 010d dddd 0111"],
					Handler: function() {
						var opcode = AtmelContext.Flash[AtmelContext.PC];
						var d = ((opcode >> 4) & 0x1f);
						var Rd = AtmelContext.R[d];
						var R = (AtmelContext.SREG.C.get() << 7) | (Rd >> 1);
						AtmelContext.SREG.Z.set(((R & 0xff) == 0) ? 1 : 0);
						AtmelContext.SREG.N.set((R >> 7) & 1);
						AtmelContext.SREG.C.set(Rd & 1);
						AtmelContext.SREG.S.set(AtmelContext.SREG.N.get() ^ AtmelContext.SREG.V.get());
						AtmelContext.SREG.V.set(AtmelContext.SREG.N.get() ^ AtmelContext.SREG.C.get());
						AtmelContext.R[d] = R & 0xff;
						AtmelContext.Clock++;
						AtmelContext.PC++;
					}
				},

				// SBC
				{
					OpCodeSize: 1,
					ModifiesPC: false,
					BitStrings: ["0000 10rd dddd rrrr"],
					Handler: function() {
						var opcode = AtmelContext.Flash[AtmelContext.PC];
						var d = (opcode >> 4) & 0x1f;
						var Rd = AtmelContext.R[d];
						var r = (opcode & 0x0f) + ((opcode >> 5) & 0x10);
						var Rr = AtmelContext.R[r];
						var C = AtmelContext.SREG.C.get();
						var R = Rd - Rr - C;
						var Z = AtmelContext.SREG.Z.get();
						AtmelContext.Flags = (AtmelContext.Flags & 0xc0 /*self.FlagsSubMask*/) | self.FlagsSub[(C << 16) | (Rd << 8) | Rr];
						AtmelContext.SREG.Z.set((R == 0) ? Z : 0);
						AtmelContext.R[d] = R & 0xff;
						AtmelContext.Clock++;
						AtmelContext.PC++;
					}
				},

				// SBCI
				{
					OpCodeSize: 1,
					ModifiesPC: false,
					BitStrings: ["0100 KKKK dddd KKKK"],
					Handler: function() {
						var opcode = AtmelContext.Flash[AtmelContext.PC];
						var d = 16 + ((opcode >> 4) & 0x0f);
						var Rd = AtmelContext.R[d];
						var K = (opcode & 0x0f) + ((opcode >> 4) & 0xf0);
						var C = AtmelContext.SREG.C.get();
						var R = Rd - K - C;
						var Z = AtmelContext.SREG.Z.get();
						AtmelContext.Flags = (AtmelContext.Flags & 0xc0 /*self.FlagsSubMask*/) | self.FlagsSub[(C << 16) | (Rd << 8) | K];
						AtmelContext.SREG.Z.set((R == 0) ? Z : 0);
						AtmelContext.R[d] = R & 0xff;
						AtmelContext.Clock++;
						AtmelContext.PC++;
					}
				},

				// SBI
				{
					OpCodeSize: 1,
					ModifiesPC: false,
					BitStrings: ["1001 1010 AAAA Abbb"],
					Handler: function() {
						var opcode = AtmelContext.Flash[AtmelContext.PC];
						var b = opcode & 0x07;
						var A = (opcode >> 3) & 0x1f;
						AtmelContext.IO[A].set(AtmelContext.IO[A].get() | (1 << b));
						AtmelContext.Clock++;
						AtmelContext.PC++;
					}
				},

				// SBIC
				{
					OpCodeSize: 1,
					ModifiesPC: true,
					BitStrings: ["1001 1001 AAAA Abbb"],
					Handler: function() {
						var opcode = AtmelContext.Flash[AtmelContext.PC];
						var b = opcode & 0x07;
						var A = (opcode >> 3) & 0x1f;
						AtmelContext.PC++;
						if ((AtmelContext.IO[A].get() & (1 << b)) == 0) {
							// skip
							var nextOpCode = AtmelContext.Flash[AtmelContext.PC];
							AtmelContext.PC += self.OpCodeSizes[nextOpCode];
							AtmelContext.Clock += 2;
						}
						else {
							// no skip
							AtmelContext.Clock++;
						}
					}
				},

				// SBIS
				{
					OpCodeSize: 1,
					ModifiesPC: true,
					BitStrings: ["1001 1011 AAAA Abbb"],
					Handler: function() {
						var opcode = AtmelContext.Flash[AtmelContext.PC];
						var b = opcode & 0x07;
						var A = (opcode >> 3) & 0x1f;
						AtmelContext.PC++;
						if ((AtmelContext.IO[A].get() & (1 << b)) != 0) {
							// skip
							var nextOpCode = AtmelContext.Flash[AtmelContext.PC];
							AtmelContext.PC += self.OpCodeSizes[nextOpCode];
							AtmelContext.Clock += 2;
						}
						else {
							// no skip
							AtmelContext.Clock++;
						}
					}
				},

				// SBIW
				{
					OpCodeSize: 1,
					ModifiesPC: false,
					BitStrings: ["1001 0111 KKdd KKKK"],
					Handler: function() {
						var opcode = AtmelContext.Flash[AtmelContext.PC];
						var d = ((opcode >> 4) & 0x03);
						var K = (opcode & 0x0f) + ((opcode >> 2) & 0x30);
						var lo = 24 + (d << 1);
						var hi = lo + 1;
						var Rd = (AtmelContext.R[hi] << 8) | AtmelContext.R[lo];
						var R = Rd - K;
						var Rdh7 = (Rd >> 15) & 1;
						var not_Rdh7 = 1 - Rdh7;
						var R15 = (R >> 15) & 1;
						var not_R15 = 1 - R15;
						AtmelContext.SREG.N.set(R15);
						AtmelContext.SREG.V.set(Rdh7 & not_R15);
						AtmelContext.SREG.S.set(AtmelContext.SREG.N.get() ^ AtmelContext.SREG.V.get());
						AtmelContext.SREG.Z.set(((R & 0xffff) == 0) ? 1 : 0);
						AtmelContext.SREG.C.set(R15 & not_Rdh7);
						AtmelContext.R[lo] = (R) & 0xff;
						AtmelContext.R[hi] = (R >> 8) & 0xff;
						AtmelContext.Clock += 2;
						AtmelContext.PC++;
					}
				},

				// SBRC
				{
					OpCodeSize: 1,
					ModifiesPC: true,
					BitStrings: ["1111 110r rrrr 0bbb"],
					Handler: function() {
						var opcode = AtmelContext.Flash[AtmelContext.PC];
						var b = opcode & 0x07;
						var r = (opcode >> 4) & 0x1f;
						AtmelContext.PC++;
						if ((AtmelContext.R[r] & (1 << b)) == 0) {
							// skip
							var nextOpCode = AtmelContext.Flash[AtmelContext.PC];
							AtmelContext.PC += self.OpCodeSizes[nextOpCode];
							AtmelContext.Clock += 2;
						}
						else {
							// no skip
							AtmelContext.Clock++;
						}
					}
				},

				// SBRS
				{
					OpCodeSize: 1,
					ModifiesPC: true,
					BitStrings: ["1111 111r rrrr 0bbb"],
					Handler: function() {
						var opcode = AtmelContext.Flash[AtmelContext.PC];
						var b = opcode & 0x07;
						var r = (opcode >> 4) & 0x1f;
						AtmelContext.PC++;
						if ((AtmelContext.R[r] & (1 << b)) != 0) {
							// set
							var nextOpCode = AtmelContext.Flash[AtmelContext.PC];
							AtmelContext.PC += self.OpCodeSizes[nextOpCode];
							AtmelContext.Clock += 2;
						}
						else {
							// clear
							AtmelContext.Clock++;
						}
					}
				},

				// SLEEP
				{
					OpCodeSize: 1,
					ModifiesPC: false,
					BitStrings: ["1001 0101 1000 1000"],
					Handler: function() {
						// not implemented
						AtmelContext.Clock++;
						AtmelContext.PC++;
					}
				},

				// SPM
				{
					OpCodeSize: 1,
					ModifiesPC: false,
					BitStrings: ["1001 0101 111x 1000"],
					Handler: function() {
						// not implemented
						AtmelContext.Clock++;
						AtmelContext.PC++;
					}
				},

				// ST
				{
					OpCodeSize: 1,
					ModifiesPC: false,
					BitStrings: ["1001 001r rrrr 1100", "1001 001r rrrr 1101", "1001 001r rrrr 1110"],
					Handler: function() {
						var opcode = AtmelContext.Flash[AtmelContext.PC];
						var r = (opcode >> 4) & 0x1f;
						switch (opcode & 0x03) {
							case 0x00:			// (i) unchanged
								AtmelContext.RAM[AtmelContext.X.get()].set(AtmelContext.R[r]);
								break;

							case 0x01:			// (ii) post-increment
								AtmelContext.RAM[AtmelContext.X.get()].set(AtmelContext.R[r]);
								AtmelContext.X.set((AtmelContext.X.get() + 1) & 0xffff);
								break;

							case 0x02:			// (iii) pre-decrement
								AtmelContext.X.set((AtmelContext.X.get() - 1) & 0xffff);
								AtmelContext.RAM[AtmelContext.X.get()].set(AtmelContext.R[r]);
								break;

							default:
								throw new InvalidOperationException();
						}
						AtmelContext.Clock += 2;
						AtmelContext.PC++;
					}
				},

				// ST_Y
				{
					OpCodeSize: 1,
					ModifiesPC: false,
					BitStrings: ["1000 001r rrrr 1000", "1001 001r rrrr 1001", "1001 001r rrrr 1010", "10q0 qq1r rrrr 1qqq"],
					Handler: function() {
						var opcode = AtmelContext.Flash[AtmelContext.PC];
						var r = (opcode >> 4) & 0x1f;
						var q = ((opcode >> 8) & 0x20) | ((opcode >> 7) & 0x18) | (opcode & 0x07);
						switch (opcode & 0x1007) {
							case 0x1001:			// (ii) post-increment
								AtmelContext.RAM[AtmelContext.Y.get()].set(AtmelContext.R[r]);
								AtmelContext.Y.set((AtmelContext.Y.get() + 1) & 0xffff);
								break;

							case 0x1002:			// (iii) pre-decrement
								AtmelContext.Y.set((AtmelContext.Y.get() - 1) & 0xffff);
								AtmelContext.RAM[AtmelContext.Y.get()].set(AtmelContext.R[r]);
								break;

							default:			// (i) unchanged and (iv) unchanged with q displacement
								AtmelContext.RAM[AtmelContext.Y.get() + q].set(AtmelContext.R[r]);
								break;
						}
						AtmelContext.Clock += 2;
						AtmelContext.PC++;
					}
				},

				// ST_Z
				{
					OpCodeSize: 1,
					ModifiesPC: false,
					BitStrings: ["1000 001r rrrr 0000", "1001 001r rrrr 0001", "1001 001r rrrr 0010", "10q0 qq1r rrrr 0qqq"],
					Handler: function() {
						var opcode = AtmelContext.Flash[AtmelContext.PC];
						var r = (opcode >> 4) & 0x1f;
						var q = ((opcode >> 8) & 0x20) | ((opcode >> 7) & 0x18) | (opcode & 0x07);
						switch (opcode & 0x1007) {
							case 0x1001:			// (ii) post-increment
								AtmelContext.RAM[AtmelContext.Z.get()].set(AtmelContext.R[r]);
								AtmelContext.Z.set((AtmelContext.Z.get() + 1) & 0xffff);
								break;

							case 0x1002:			// (iii) pre-decrement
								AtmelContext.Z.set((AtmelContext.Z.get() - 1) & 0xffff);
								AtmelContext.RAM[AtmelContext.Z.get()].set(AtmelContext.R[r]);
								break;

							default:			// (i) unchanged and (iv) unchanged with q displacement
								AtmelContext.RAM[AtmelContext.Z.get() + q].set(AtmelContext.R[r]);
								break;
						}
						AtmelContext.Clock += 2;
						AtmelContext.PC++;
					}
				},

				// STS
				{
					OpCodeSize: 2,
					ModifiesPC: false,
					BitStrings: ["1001 001d dddd 0000"],
					Handler: function() {
						// no flags
						var opcode = AtmelContext.Flash[AtmelContext.PC];
						var r = (opcode >> 4) & 0x1f;
						var k = AtmelContext.Flash[(AtmelContext.PC + 1)];
						AtmelContext.RAM[k].set(AtmelContext.R[r]);
						AtmelContext.Clock += 2;
						AtmelContext.PC += 2;
					}
				},

				// SUB
				{
					OpCodeSize: 1,
					ModifiesPC: false,
					BitStrings: ["0001 10rd dddd rrrr"],
					Handler: function() {
						var opcode = AtmelContext.Flash[AtmelContext.PC];
						var d = (opcode >> 4) & 0x1f;
						var Rd = AtmelContext.R[d];
						var r = (opcode & 0x0f) + ((opcode >> 5) & 0x10);
						var Rr = AtmelContext.R[r];
						AtmelContext.Flags = (AtmelContext.Flags & 0xc0 /*self.FlagsSubMask*/) | self.FlagsSub[(Rd << 8) | Rr];
						var R = Rd - Rr;
						AtmelContext.R[d] = R & 0xff;
						AtmelContext.Clock++;
						AtmelContext.PC++;
					}
				},

				// SUBI
				{
					OpCodeSize: 1,
					ModifiesPC: false,
					BitStrings: ["0101 KKKK dddd KKKK"],
					Handler: function() {
						var opcode = AtmelContext.Flash[AtmelContext.PC];
						var d = 16 + ((opcode >> 4) & 0x0f);
						var K = (opcode & 0x0f) + ((opcode >> 4) & 0xf0);
						var Rd = AtmelContext.R[d];
						AtmelContext.Flags = (AtmelContext.Flags & 0xc0 /*self.FlagsSubMask*/) | self.FlagsSub[(Rd << 8) | K];
						var R = Rd - K;
						AtmelContext.R[d] = R & 0xff;
						AtmelContext.Clock++;
						AtmelContext.PC++;
					}
				},

				// SWAP
				{
					OpCodeSize: 1,
					ModifiesPC: false,
					BitStrings: ["1001 010d dddd 0010"],
					Handler: function() {
						// no flags
						var opcode = AtmelContext.Flash[AtmelContext.PC];
						var d = (opcode >> 4) & 0x1f;
						var hi = AtmelContext.R[d] & 0xf0;
						var lo = AtmelContext.R[d] & 0x0f;
						AtmelContext.R[d] = ((hi >> 4) | (lo << 4)) & 0xff;
						AtmelContext.Clock++;
						AtmelContext.PC++;
					}
				},

				// WDR
				{
					OpCodeSize: 1,
					ModifiesPC: false,
					BitStrings: ["1001 0101 1010 1000"],
					Handler: function() {
						// not implemented
						AtmelContext.Clock++;
						AtmelContext.PC++;
					}
				},

				// XCH
				{
					OpCodeSize: 1,
					ModifiesPC: false,
					BitStrings: ["1001 001r rrrr 0100"],
					Handler: function() {
						var opcode = AtmelContext.Flash[AtmelContext.PC];
						var r = (opcode >> 4) & 0x1f;
						var addr = AtmelContext.Z.get();
						var R = AtmelContext.R[r];
						AtmelContext.R[r] = AtmelContext.RAM[addr].get() & 0xff;
						AtmelContext.RAM[addr].set(R & 0xff);
						AtmelContext.Clock++;
						AtmelContext.PC++;
					}
				}
			];
		}
	}

});
