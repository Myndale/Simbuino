using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Simbuino.Emulator
{
	// IO register mappings and bit fields (refer to the ATMega328p data sheet for details)
	public static class AtmelIO
	{
		// Timers
		public const int TCCR0A = 0x44;
		public const int TCCR0B = 0x45;
		public const int TIFR0 = 0x35;
		public const int TCNT0 = 0x46;
		public const int TOV0 = 0;
		public const int CLKPR = 0x61;
		public const int TIFR1 = 0x36;
		public const int TOV1 = 0;
		public const int TCNT1H = 0x85;
		public const int TCNT1L = 0x84;
		public const int TCCR1A = 0x80;
		public const int TCCR1B = 0x81;
		public const int TCCR1C = 0x82;
		public const int OCR1AH = 0x89;
		public const int OCR1AL = 0x88;
		public const int TCCR2A = 0xB0;
		public const int TCCR2B = 0xB1;
		public const int TCNT2 = 0xB2;
		public const int OCR2A = 0xB3;
		public const int OCR2B = 0xB4;
		public const int OCR0B = 0x48;
		public const int COM0B1 = 5;

		// ADC
		public const int ADSCRA = 0x7a;
		public const int ADMUX = 0x7c;
		public const int ADCH = 0x79;
		public const int ADCL = 0x78;
		public const int ADSC = 6;
		public const int ADLAR = 5;
		

		// EEPROM
		public const int EECR = 0x3f;
		public const int EEARH = 0x42;
		public const int EEARL = 0x41;
		public const int EEDR = 0x40;
		public const int EEMPE = 2;
		public const int EEPE = 1;

		// SPI
		public const int SPCR = 0x4c;
		public const int SPIE = 7;
		public const int SPE = 6;
		public const int SPSR = 0x4d;
		public const int SPIF = 7;
		public const int SPDR = 0x4e;

		// UART
		public const int UCSR0A = 0xC0;
		public const int UCSR0B = 0xC1;
		public const int UDR0 = 0xC6;
		public const int UDRE0 = 5;
		public const int UDRIE0 = 5;
		public const int TXC0 = 6;
		public const int TXCIE0 = 6;		
	}
}
