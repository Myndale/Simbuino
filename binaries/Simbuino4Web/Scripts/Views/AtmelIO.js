$(function () {

	// IO register mappings and bit fields (refer to the ATMega328p data sheet for details)
	AtmelIO =
	{
		// Timers
		TCCR0A: 0x44,
		TCCR0B: 0x45,
		TIFR0: 0x35,
		TCNT0: 0x46,
		TOV0: 0,
		CLKPR: 0x61,
		TIFR1: 0x36,
		TOV1: 0,
		TCNT1H: 0x85,
		TCNT1L: 0x84,
		TCCR1A: 0x80,
		TCCR1B: 0x81,
		TCCR1C: 0x82,
		OCR1AH: 0x89,
		OCR1AL: 0x88,
		TCCR2A: 0xB0,
		TCCR2B: 0xB1,
		TCNT2: 0xB2,
		OCR2A: 0xB3,
		OCR2B: 0xB4,
		OCR0B: 0x48,
		COM0B1: 5,

		// ADC
		ADSCRA: 0x7a,
		ADMUX: 0x7c,
		ADCH: 0x79,
		ADCL: 0x78,
		ADSC: 6,
		ADLAR: 5,


		// EEPROM
		EECR: 0x3f,
		EEARH: 0x42,
		EEARL: 0x41,
		EEDR: 0x40,
		EEMPE: 2,
		EEPE: 1,

		// SPI
		SPCR: 0x4c,
		SPIE: 7,
		SPE: 6,
		SPSR: 0x4d,
		SPIF: 7,
		SPDR: 0x4e,

		// UART
		UCSR0A: 0xC0,
		UCSR0B: 0xC1,
		UDR0: 0xC6,
		UDRE0: 5,
		UDRIE0: 5,
		TXC0: 6,
		TXCIE0: 6,
	}

});
