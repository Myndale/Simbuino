using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Simbuino.Emulator
{
	// helper class for accessing the individual bits in the AVR flags register
	public class AtmelFlagsRegister : Register
	{
		public int Value
		{
			[MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
			get
			{
				return AtmelContext.Flags;
			}
			
			[MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
			set
			{
				AtmelContext.Flags = value;
			}
		}

		public int this[int index]
		{
			[MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
			get
			{
				return ((AtmelContext.Flags >> index) & 1);
			}

			[MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
			set
			{
				if (value == 0)
					AtmelContext.Flags = AtmelContext.Flags & ~(1 << index);
				else
					AtmelContext.Flags = AtmelContext.Flags | (1 << index);
			}
		}

		private const int CPOS = 0;		
		public int C
		{
			[MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
			get { return ((AtmelContext.Flags >> CPOS) & 1); }

			[MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
			set { AtmelContext.Flags = (value == 0) ? AtmelContext.Flags & ~(1 << CPOS) : AtmelContext.Flags | (1 << CPOS); }
		}

		private const int ZPOS = 1;
		public int Z
		{
			[MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
			get { return ((AtmelContext.Flags >> ZPOS) & 1); }

			[MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
			set { AtmelContext.Flags = (value == 0) ? AtmelContext.Flags & ~(1 << ZPOS) : AtmelContext.Flags | (1 << ZPOS); }
		}

		private const int NPOS = 2;
		public int N
		{
			[MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
			get { return ((AtmelContext.Flags >> NPOS) & 1); }

			[MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
			set { AtmelContext.Flags = (value == 0) ? AtmelContext.Flags & ~(1 << NPOS) : AtmelContext.Flags | (1 << NPOS); }
		}

		private const int VPOS = 3;
		public int V
		{
			[MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
			get { return ((AtmelContext.Flags >> VPOS) & 1); }

			[MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
			set { AtmelContext.Flags = (value == 0) ? AtmelContext.Flags & ~(1 << VPOS) : AtmelContext.Flags | (1 << VPOS); }
		}

		private const int SPOS = 4;
		public int S
		{
			[MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
			get { return ((AtmelContext.Flags >> SPOS) & 1); }
			
			[MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
			set { AtmelContext.Flags = (value == 0) ? AtmelContext.Flags & ~(1 << SPOS) : AtmelContext.Flags | (1 << SPOS); }
		}

		private const int HPOS = 5;
		public int H
		{
			[MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
			get { return ((AtmelContext.Flags >> HPOS) & 1); }

			[MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
			set { AtmelContext.Flags = (value == 0) ? AtmelContext.Flags & ~(1 << HPOS) : AtmelContext.Flags | (1 << HPOS); }
		}

		private const int TPOS = 6;
		public int T
		{
			[MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
			get { return ((AtmelContext.Flags >> TPOS) & 1); }

			[MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
			set { AtmelContext.Flags = (value == 0) ? AtmelContext.Flags & ~(1 << TPOS) : AtmelContext.Flags | (1 << TPOS); }
		}

		private const int IPOS = 7;
		public int I
		{
			[MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
			get { return ((AtmelContext.Flags >> IPOS) & 1); }

			[MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
			set { AtmelContext.Flags = (value == 0) ? AtmelContext.Flags & ~(1 << IPOS) : AtmelContext.Flags | (1 << IPOS); }
		}

		public const int FlagsIndex = 0x5f;

		public AtmelFlagsRegister()
		{			
		}

		public void Reset()
		{
			AtmelContext.R[FlagsIndex] = 0;
		}
	}
}
