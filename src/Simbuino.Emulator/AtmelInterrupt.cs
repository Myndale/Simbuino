using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Simbuino.Emulator
{
	// standard interrupt vectors
	// todo: some of these are scattered throughout the code base and need to be moved here
	public static class AtmelInterrupt
	{
		// timers
		public const int TIM1_COMPA = 0x16;
		public const int TIM1_OVF = 0x1A;
		public const int TIM0_OVF = 0x20;

		// spi
		public const int SPI_STC = 0x22;

		// usart
		public const int USART_UDRE = 0x26;
	}
}
