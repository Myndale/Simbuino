using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Simbuino.Emulator
{
	// this class represents the AVRs context (i.e. current state). most members are static variables to improve emulator performance.
	public static class AtmelContext
	{
		public static int[] Flash;
		public static int[] EEPROM;
		public static Register[] RAM;
		public static int PC;
		public static long Clock;
		public static int Flags;

		public static bool InterruptPending;
		public static int Timer0;
		public static bool Timer0OutputCompareFlag;
		public static int Timer1;
		public static bool Timer1OutputCompareFlag;
		public static bool Timer1OverflowFlag;
		public static long LastTimerUpdate;
		public static long NextTimerEvent;
		public static bool UDRE_InterruptPending;
		public static bool SPI_InterruptPending;
		
		public static AtmelFlagsRegister SREG;
		public static int[] R;
		public static Register[] IO;
		public static PortRegister B;
		public static PortRegister C;
		public static PortRegister D;
		public static IndirectAddressRegister X;
		public static IndirectAddressRegister Y;
		public static IndirectAddressRegister Z;
		public static MemoryMappedWordRegister SP;

		private const int FlashSize = 32 * 1024 / 2;
		private const int EEPROMSize = 1024;		

		// general register file mapped from 0x00 to 0x1f
		private const int FirstReg = 0x00;
		private const int NumRegs = 32;

		// regular I/O from 0x20 to 0x5f and extended I/O from 0x60-0xff
		public const int FirstIO = 0x20;
		public const int NumIO = 224;

		// main SRAM
		public const int FirstSRAM = 0x100;
		public const int RAMSize = 2 * 1024;

		// some registers cause hardware changes when simply read e.g. reading SPDR clears the SPIF flag.
		// this flag is used to disable such changes so that code like the display update can access those
		// registers without inadvertently modifying the context state.
		public static bool Active = true;

		
		static AtmelContext()
		{
			Flash = new int[FlashSize];
			EEPROM = new int[EEPROMSize];
			R = new int[NumRegs + NumIO + RAMSize];
			var mappedR = Enumerable.Range(FirstReg, NumRegs).Select<int, Register>(addr => new MemoryMappedRegister(addr)).ToArray();
			IO = Enumerable.Range(FirstIO, NumIO).Select<int, Register>(addr => new ObservableRegister(addr)).ToArray();
			IO[AtmelFlagsRegister.FlagsIndex - FirstIO] = SREG = new AtmelFlagsRegister();
			var sram = Enumerable.Range(FirstSRAM, RAMSize).Select(addr => new MemoryMappedRegister(addr)).ToArray();
			RAM = mappedR.Cast<Register>()
				.Concat(IO.Cast<Register>())
				.Concat(sram.Cast<Register>())
				.ToArray();
			B = new PortRegister(0x23, 0x25, 0x24);
			C = new PortRegister(0x26, 0x28, 0x27);
			D = new PortRegister(0x29, 0x2b, 0x2a);
			X = new IndirectAddressRegister(26);
			Y = new IndirectAddressRegister(28);
			Z = new IndirectAddressRegister(30);
			SP = new MemoryMappedWordRegister(0x5d);			
			ClearEEPROM();

			(RAM[AtmelIO.CLKPR] as ObservableRegister).OnRegisterChanged += InvalidateTimers;
			(RAM[AtmelIO.TCCR0A] as ObservableRegister).OnRegisterChanged += InvalidateTimers;
			(RAM[AtmelIO.TCCR0B] as ObservableRegister).OnRegisterChanged += InvalidateTimers;
			(RAM[AtmelIO.TCNT1H] as ObservableRegister).OnRegisterChanged += InvalidateTimers;
			(RAM[AtmelIO.TCNT1L] as ObservableRegister).OnRegisterChanged += InvalidateTimers;
			(RAM[AtmelIO.TCCR1A] as ObservableRegister).OnRegisterChanged += InvalidateTimers;
			(RAM[AtmelIO.TCCR1B] as ObservableRegister).OnRegisterChanged += InvalidateTimers;
			
		}

		public static void Reset()
		{
			for (int i = 0; i < RAM.Length; i++)
				RAM[i].Reset();
			PC = 0;
			Clock = 0;
			Timer0 = 0;
			Timer1 = 0;
			InterruptPending = false;
			Timer0OutputCompareFlag = false;
			Timer1OutputCompareFlag = false;
			Timer1OverflowFlag = false;
			LastTimerUpdate = 0;
			NextTimerEvent = 0;
			UDRE_InterruptPending = false;
			SPI_InterruptPending = false;			
		}

		static void InvalidateTimers(int oldVal, int newVal)
		{
			if (oldVal != newVal)
				AtmelContext.NextTimerEvent = AtmelContext.Clock;
		}

		public static void UpdateInterruptFlags()
		{
			InterruptPending = false;
			UDRE_InterruptPending = false;
			SPI_InterruptPending = false;

			// if the data transmit register is set and interrupts are enabled then trigger a USART interrupt
			if ((USART.UCSR0A[AtmelIO.TXC0] != 0) && (USART.UCSR0B[AtmelIO.UDRIE0] != 0) && (SREG.I == 0))
				UDRE_InterruptPending = true;

			// if transfer complete flag is set and interrupts are enabled then trigger an SPI interrupt
			if ((SPI.SPSR[AtmelIO.SPIF] != 0) && (SPI.SPCR[AtmelIO.SPIE] != 0) && (SREG.I == 0))
				SPI_InterruptPending = true;

			if (UDRE_InterruptPending)
				InterruptPending = true;
			if (SPI_InterruptPending)
				InterruptPending = true;
			if (Timer0OutputCompareFlag)
				InterruptPending = true;
			if (Timer1OutputCompareFlag)
				InterruptPending = true;
			if (Timer1OverflowFlag)
				InterruptPending = true;
		}

		public static void ClearEEPROM()
		{
			for (int i=0; i<EEPROM.Length; i++)
				EEPROM[i] = 0xff;
		}

		

	}
	
}
