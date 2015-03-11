using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Simbuino.Emulator
{
	// the ParameterHandler classes provide functionality needed to correctly display op-codes in the gui

	public class ParameterHandler
	{
		protected const int ParamWidth = 20;

		public virtual string GetParameters(int addr, string bits)
		{
			return "???";
		}

		protected string IntVector(int addr)
		{
			switch (addr/2)
			{
				case 0: return "RESET";
				case 1: return "INT0";
				case 2: return "INT1";
				case 3: return "PCINT0";
				case 4: return "PCINT1";
				case 5: return "PCINT2";
				case 6: return "WDT";
				case 7: return "TIMER2 COMPA";
				case 8: return "TIMER2 COMPB";
				case 9: return "TIMER2 OVF";
				case 10: return "TIMER1 CAPT";
				case 11: return "TIMER1 COMPA";
				case 12: return "TIMER1 COMPB";
				case 13: return "TIMER1 OVF";
				case 14: return "TIMER0 COMPA";
				case 15: return "TIMER0 COMPB";
				case 16: return "TIMER0 OVF";
				case 17: return "SPI, STC";
				case 18: return "USART, RX";
				case 19: return "USART, UDRE";
				case 20: return "USART, TX";
				case 21: return "ADC";
				case 22: return "EE READY";
				case 23: return "ANALOG COMP";
				case 24: return "TWI";
				case 25: return "SPM READY";
				default: return "";
			}
		}

		public static string[] PortNames =
		{
			// in reverse order (it was faster to copy from the data-sheet)
			null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,
			null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,
			null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,
			"UDR0","UBRR0H","UBRR0L",null,"UCSR0C","UCSR0B","UCSR0A",null,null,"TWAMR","TWCR","TWDR","TWAR","TWSR","TWBR",null,"ASSR",null,"OCR2B",
			"OCR2A","TCNT2","TCCR2B","TCCR2A",null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,
			null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,
			null,null,"OCR1BH","OCR1BL","OCR1AH","OCR1AL","ICR1H","ICR1L","TCNT1H","TCNT1L",null,"TCCR1C","TCCR1B","TCCR1A","DIDR1","DIDR0",null,"ADMUX","ADCSRB",
			"ADCSRA","ADCH","ADCL",null,null,null,null,null,null,null,"TIMSK2","TIMSK1","TIMSK0","PCMSK2","PCMSK1","PCMSK0",null,"EICRA","PCICR",
			null,"OSCCAL",null,"PRR",null,null,"CLKPR","WDTCSR","SREG","SPH","SPL",null,null,null,null,null,"SPMCSR",null,"MCUCR",
			"MCUSR","SMCR",null,null,"ACSR",null,"SPDR","SPSR","SPCR","GPIOR2","GPIOR1",null,"OCR0B","OCR0A","TCNT0","TCCR0B","TCCR0A","GTCCR","EEARH",
			"EEARL","EEDR","EECR","GPIOR0","EIMSK","EIFR","PCIFR",null,null,null,"TIFR2","TIFR1","TIFR0",null,null,null,null,null,null,
			null,null,null,"PORTD","DDRD","PIND","PORTC","DDRC","PINC","PORTB","DDRB","PINB",null,null,null
		};

		public static string PortString(int addr)
		{
			return PortNames[PortNames.Length - 1 - addr] ?? String.Format("0x{0:x2}", addr);
		}

		protected static string FlagName(int flag)
		{
			switch (flag)
			{
				case 0: return "C";
				case 1: return "Z";
				case 2: return "N";
				case 3: return "V";
				case 4: return "S";
				case 5: return "H";
				case 6: return "T";
				case 7: return "I";
				default: return "";
			}
		}
	}

	public class NoParametersHandler : ParameterHandler
	{
		public override string GetParameters(int addr, string bits)
		{
			return "";
		}
	}

	public class JmpParameterHandler : ParameterHandler
	{
		public override string GetParameters(int addr, string bits)
		{
			var opcode = AtmelContext.Flash[addr];
			var dest = AtmelContext.Flash[addr + 1];
			dest += (opcode & 1) << 8;
			dest += ((opcode >> 4) & 0x1f) << 9;
			if (addr <= 52)
				return String.Format("0x{0:x4}", dest * 2).PadRight(ParamWidth) + "; " + IntVector(addr);
			else
				return String.Format("0x{0:x4}", dest * 2);
		}
	}

	public class CallParameterHandler : ParameterHandler
	{
		public override string GetParameters(int addr, string bits)
		{
			var opcode = AtmelContext.Flash[addr];
			var dest = AtmelContext.Flash[addr + 1];
			dest += (opcode & 1) << 8;
			dest += ((opcode >> 4) & 0x1f) << 9;
			return String.Format("0x{0:x4}", dest * 2);
		}
	}

	public class DParameterHandler : ParameterHandler
	{
		public override string GetParameters(int addr, string bits)
		{
			var opcode = AtmelContext.Flash[addr];
			var d = (opcode >> 4) & 0x1f;
			return String.Format("r{0}", d);
		}
	}

	public class DXParameterHandler : ParameterHandler
	{
		public override string GetParameters(int addr, string bits)
		{
			var opcode = AtmelContext.Flash[addr];
			var d = (opcode >> 4) & 0x1f;
			switch (opcode & 0x0f)
			{
				case 0x0c: return String.Format("r{0}, (X)", d);		// unchanged
				case 0x0d: return String.Format("r{0}, (X+)", d);		// post-increment
				case 0x0e: return String.Format("r{0}, (-X)", d);		// pre-decrement
				default: return "???";
			}
		}
	}

	public class DYParameterHandler : ParameterHandler
	{
		public override string GetParameters(int addr, string bits)
		{
			var opcode = AtmelContext.Flash[addr];
			var d = (opcode >> 4) & 0x1f;
			var q = ((opcode >> 8) & 0x20) | ((opcode >> 7) & 0x18) | (opcode & 0x07);
			switch (opcode & 0x1007)
			{
				case 0x1001: return String.Format("r{0}, (Y+)", d);		// post-increment
				case 0x1002: return String.Format("r{0}, (-Y)", d);		// pre-decrement
				default:
					if (q == 0)
						return String.Format("r{0}, (Y)", d);		// unchanged
					else
						return String.Format("r{0}, (Y+{1})", d, q);	// unchanged with q displacement
			}
		}
	}

	public class DZParameterHandler : ParameterHandler
	{
		public override string GetParameters(int addr, string bits)
		{
			var opcode = AtmelContext.Flash[addr];
			var d = (opcode >> 4) & 0x1f;
			var q = ((opcode >> 8) & 0x20) | ((opcode >> 7) & 0x18) | (opcode & 0x07);
			switch (opcode & 0x1007)
			{
				case 0x1001: return String.Format("r{0}, (Z+)", d);		// post-increment
				case 0x1002: return String.Format("r{0}, (-Z)", d);		// pre-decrement
				default:
					if (q == 0)
						return String.Format("r{0}, (Z)", d);		// unchanged
					else
						return String.Format("r{0}, (Z+{1})", d, q);	// unchanged with q displacement
			}
		}
	}

	public class DataSpaceParameterHandler : ParameterHandler
	{
		public override string GetParameters(int addr, string bits)
		{
			var opcode = AtmelContext.Flash[addr];
			var k = AtmelContext.Flash[addr + 1];
			var d = (opcode >> 4) & 0x1f;
			return String.Format("r{0}, 0x{1:x4}", d, k);
		}
	}

	public class RParameterHandler : ParameterHandler
	{
		public override string GetParameters(int addr, string bits)
		{
			var opcode = AtmelContext.Flash[addr];
			var r = (opcode >> 4) & 0x1f;
			return String.Format("r{0}", r);
		}
	}

	public class XRParameterHandler : ParameterHandler
	{
		public override string GetParameters(int addr, string bits)
		{
			var opcode = AtmelContext.Flash[addr];
			var r = (opcode >> 4) & 0x1f;
			switch (opcode & 3)
			{
				case 0: return String.Format("(X), r{0}", r);			// unchanged
				case 1: return String.Format("(X+), r{0}", r);		// post-increment
				case 2: return String.Format("(-X), r{0}", r);		// pre-decrement
				default: return base.GetParameters(addr, bits);
			}
		}
	}

	public class YRParameterHandler : ParameterHandler
	{
		public override string GetParameters(int addr, string bits)
		{
			var opcode = AtmelContext.Flash[addr];
			var r = (opcode >> 4) & 0x1f;
			var q = ((opcode >> 8) & 0x20) | ((opcode >> 7) & 0x18) | (opcode & 0x07);
			switch (opcode & 0x1007)
			{
				case 0x1001: return String.Format("(Y+), r{0}", r);		// post-increment
				case 0x1002: return String.Format("(-Y), r{0}", r);		// pre-decrement
				default:
					if (q == 0)
						return String.Format("(Y), r{0}", r);		// unchanged
					else
						return String.Format("(Y+{1}), r{0}", r, q);	// unchanged, displacement
			}
		}
	}

	public class ZRParameterHandler : ParameterHandler
	{
		public override string GetParameters(int addr, string bits)
		{
			var opcode = AtmelContext.Flash[addr];
			var r = (opcode >> 4) & 0x1f;
			var q = ((opcode >> 8) & 0x20) | ((opcode >> 7) & 0x18) | (opcode & 0x07);
			switch (opcode & 0x1007)
			{
				case 0x1001: return String.Format("(Z+), r{0}", r);		// post-increment
				case 0x1002: return String.Format("(-Z), r{0}", r);		// pre-decrement
				default:
					if (q == 0)
						return String.Format("(Z), r{0}", r);		// unchanged
					else
						return String.Format("(Z+{1}), r{0}", r, q);	// unchanged, displacement
			}
		}
	}

	public class RDParameterHandler : ParameterHandler
	{
		public override string GetParameters(int addr, string bits)
		{
			var opcode = AtmelContext.Flash[addr];
			var d = (opcode >> 4) & 0x1f;
			var r = (opcode & 0x0f) + ((opcode >> 5) & 0x10);
			return String.Format("r{0}, r{1}", d, r);
		}
	}

	public class ReducedRDParameterHandler : ParameterHandler
	{
		public override string GetParameters(int addr, string bits)
		{
			var opcode = AtmelContext.Flash[addr];
			var d = 16 + ((opcode >> 4) & 0x0f);
			var r = 16 + (opcode & 0x0f);
			return String.Format("r{0}, r{1}", d, r);
		}
	}

	public class RDPairParameterHandler : ParameterHandler
	{
		public override string GetParameters(int addr, string bits)
		{
			var opcode = AtmelContext.Flash[addr];
			var d = 2 * ((opcode >> 4) & 0x0f);
			var r = 2 * (opcode & 0x0f);
			return String.Format("r{0}, r{1}", d, r);
		}
	}

	public class TinyRDParameterHandler : ParameterHandler
	{
		public override string GetParameters(int addr, string bits)
		{
			var opcode = AtmelContext.Flash[addr];
			var d = 16 + ((opcode >> 4) & 0x07);
			var r = 16 + ((opcode & 0x07));
			return String.Format("r{0}, r{1}", d, r);
		}
	}

	public class DBParameterHandler : ParameterHandler
	{
		public override string GetParameters(int addr, string bits)
		{
			var opcode = AtmelContext.Flash[addr];
			var d = (opcode >> 4) & 0x1f;
			var b = opcode & 0x07;
			return String.Format("r{0}, {1}", d, b);
		}
	}

	public class KParameterHandler : ParameterHandler
	{
		public override string GetParameters(int addr, string bits)
		{
			var opcode = AtmelContext.Flash[addr];
			var k = 2 * ((opcode << 22) >> 25);
			var dst = 2 * addr + 2 + k;
			if (k >= 0)
				return String.Format(".+{0}", k).PadRight(ParamWidth) + String.Format("; 0x{0:x2}", dst);
			else
				return String.Format(".-{0}", -k).PadRight(ParamWidth) + String.Format("; 0x{0:x2}", dst);
		}
	}

	public class KSParameterHandler : ParameterHandler
	{
		public override string GetParameters(int addr, string bits)
		{
			var opcode = AtmelContext.Flash[addr];
			var s = opcode & 7;
			var k = 2 * ((opcode << 22) >> 25);
			var dst = 2 * addr + 2 + k;
			if (k >= 0)
				return String.Format("{0}, .+{1}", s, k).PadRight(ParamWidth) + String.Format("; 0x{0:x2}", dst);
			else
				return String.Format("{0}, .-{1}", s, -k).PadRight(ParamWidth) + String.Format("; 0x{0:x2}", dst);
		}
	}

	public class KDParameterHandler : ParameterHandler
	{
		public override string GetParameters(int addr, string bits)
		{
			var opcode = AtmelContext.Flash[addr];
			var d = (opcode >> 4) & 0x03;
			var K = (opcode & 0x0f) + ((opcode >> 2) & 0x30);
			return String.Format("r{0}:r{1}, 0x{2:x2}", 24 + d * 2 + 1, 24 + d * 2, K).PadRight(ParamWidth) + String.Format("; {0}", K);
		}
	}

	public class RBParameterHandler : ParameterHandler
	{
		public override string GetParameters(int addr, string bits)
		{
			var opcode = AtmelContext.Flash[addr];
			var b = opcode & 0x07;
			var r = (opcode >> 4) & 0x1f;
			return String.Format("r{0}, {1}", r, b);
		}
	}

	public class ImmParameterHandler : ParameterHandler
	{
		public override string GetParameters(int addr, string bits)
		{
			var opcode = AtmelContext.Flash[addr];
			var d = 16 + ((opcode >> 4) & 0x0f);
			var K = (byte)((opcode & 0x0f) + ((opcode >> 4) & 0xf0));
			return String.Format("r{0}, 0x{1:x2}", d, K).PadRight(ParamWidth) + String.Format("; {0}", K);
		}
	}

	public class OutParameterHandler : ParameterHandler
	{
		public override string GetParameters(int addr, string bits)
		{
			var opcode = AtmelContext.Flash[addr];
			int A = (opcode & 0x0f) + ((opcode & 0x600) >> 5);
			int r = ((opcode >> 4) & 0x01f);
			return String.Format("{0}, r{1}", PortString(A), r).PadRight(ParamWidth) + String.Format("; {0}", A);
		}
	}

	public class InParameterHandler : ParameterHandler
	{
		public override string GetParameters(int addr, string bits)
		{
			var opcode = AtmelContext.Flash[addr];
			int A = (opcode & 0x0f) + ((opcode & 0x600) >> 5);
			int r = ((opcode >> 4) & 0x01f);
			return String.Format("r{0}, {1}", r, PortString(A)).PadRight(ParamWidth) + String.Format("; {0}", A);
		}
	}

	public class RelativeParameterHandler : ParameterHandler
	{
		public override string GetParameters(int addr, string bits)
		{
			var opcode = AtmelContext.Flash[addr];
			var k = ((opcode << 20) >> 20) * 2;
			var dst = 2 * addr + 2 + k;
			if (k >= 0)
				return String.Format(".+{0}", k).PadRight(ParamWidth) + String.Format("; 0x{0:x2}", dst);
			else
				return String.Format(".-{0}", -k).PadRight(ParamWidth) + String.Format("; 0x{0:x2}", dst);
		}
	}

	public class FlagBitParameterHandler : ParameterHandler
	{
		public override string GetParameters(int addr, string bits)
		{
			var opcode = AtmelContext.Flash[addr];
			var s = (opcode >> 4) & 0x07;
			return String.Format("{0}", s).PadRight(ParamWidth) + String.Format("; {0}", FlagName(s));
		}
	}

	public class ABParameterHandler : ParameterHandler
	{
		public override string GetParameters(int addr, string bits)
		{
			var opcode = AtmelContext.Flash[addr];
			var b = opcode & 0x07;
			var A = (opcode >> 3) & 0x1f;
			return String.Format("{0}, {1}", PortString(A), b).PadRight(ParamWidth) + String.Format("; {0}", A);
		}
	}

	public class LPMParameterHandler : ParameterHandler
	{
		public override string GetParameters(int addr, string bits)
		{
			var opcode = AtmelContext.Flash[addr];
			var d = (opcode >> 4) & 0x1f;
			var b = AtmelContext.Flash[AtmelContext.Z.Value >> 1];
			if ((AtmelContext.Z.Value & 1) == 1)
				b = (byte)(b >> 8);
			else
				b = (byte)b;
			switch (opcode & 0x0f)
			{
				case 0x08:			// (i)
					break;

				case 0x04:			// (ii)
					return String.Format("r{0}, (Z)", d);

				case 0x05:			// (iii)
					return String.Format("r{0}, (Z+)", d);
			}
			return "";
		}
	}

	

}
