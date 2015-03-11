using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Simbuino.Emulator
{
	// these classes are accessors for specialty AVR registers i.e. those with different read/write values and/or addresses
	
	public class PortRegister : Register
	{
		private readonly int ReadIndex;
		private readonly int WriteIndex;
		private readonly int DirIndex;

		public ObservableRegister ReadRegister {
			[MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
			get { return AtmelContext.RAM[ReadIndex] as ObservableRegister; }
		}
		public ObservableRegister WriteRegister {
			[MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
			get { return AtmelContext.RAM[WriteIndex] as ObservableRegister; }
		}

		public PortRegister(int readIndex, int writeIndex, int dirIndex)
		{
			this.ReadIndex = readIndex;
			this.WriteIndex = writeIndex;
			this.DirIndex = dirIndex;

			(AtmelContext.RAM[writeIndex] as ObservableRegister).OnRegisterChanged += PortRegister_OnRegisterChanged;
		}

		void PortRegister_OnRegisterChanged(int oldVal, int newVal)
		{
			// if a pin is set as an output then writing to its write register sets the input value on it's read register as well
			var readVal = AtmelContext.RAM[this.ReadIndex].Value;
			var direction = AtmelContext.RAM[this.DirIndex].Value;
			readVal &= ~direction;
			readVal |= (direction & newVal);
			AtmelContext.RAM[this.ReadIndex].Value = readVal;
		}

		public int Value
		{
			[MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
			get
			{
				return AtmelContext.RAM[this.ReadIndex].Value;
			}

			[MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
			set
			{
				AtmelContext.RAM[this.WriteIndex].Value = value;
			}
		}

		public int this[int index]
		{
			[MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
			get
			{
				return ((AtmelContext.RAM[this.ReadIndex].Value >> index) & 1);
			}

			[MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
			set
			{
				if (value == 0)
					AtmelContext.RAM[this.WriteIndex].Value = AtmelContext.RAM[this.WriteIndex].Value & ~(1 << index);
				else
					AtmelContext.RAM[this.WriteIndex].Value = AtmelContext.RAM[this.WriteIndex].Value | (1 << index);
			}
		}

		public void Reset()
		{
			AtmelContext.RAM[this.WriteIndex].Reset();
			AtmelContext.RAM[this.ReadIndex].Reset();
		}
	}

	public class IndirectAddressRegister : WordRegister
	{
		private readonly int Index;

		public IndirectAddressRegister(int Index)
		{
			this.Index = Index;
		}

		public ushort Value
		{
			[MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
			get
			{
				int lo = AtmelContext.RAM[this.Index].Value;
				int hi = AtmelContext.RAM[this.Index + 1].Value;
				return (ushort)(lo | (hi << 8));
			}

			[MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
			set
			{
				AtmelContext.RAM[this.Index].Value = value;
				AtmelContext.RAM[this.Index + 1].Value = value >> 8;
			}
		}

		public int this[int index]
		{
			[MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
			get
			{
				return ((Value >> index) & 1);
			}

			[MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
			set
			{
				if (value == 0)
					Value = (ushort)(Value & ~(1 << index));
				else
					Value = (ushort)(Value | (1 << index));
			}
		}

	}
}
