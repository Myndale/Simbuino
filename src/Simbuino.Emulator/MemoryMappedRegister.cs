using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Simbuino.Emulator
{
	// this is a memory-mapped register class that is used mainly to provide change notification for when a register changes,
	// it's needed by devices (LCD etc) that need to know when a memory location has been written to so they can react accordingly.

	public interface IMemoryMappedRegister : Register
	{
	}

	public class MemoryMappedRegister : Register
	{
		private int Index;
		public MemoryMappedRegister(int index)
		{
			this.Index = index;
		}

		public int Value
		{
			[MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
			get { return AtmelContext.R[Index]; }
			
			[MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
			set
			{
				int oldVal = AtmelContext.R[Index];
				if (oldVal != value)
					AtmelContext.R[Index] = value & 0xff;
			}
		}

		public int this[int index]
		{
			[MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
			get
			{
				return ((AtmelContext.R[Index] >> index) & 1);
			}
			
			[MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
			set
			{
				if (value == 0)
					this.Value = this.Value & ~(1 << index);
				else
					this.Value = this.Value | (1 << index);
			}
		}

		public void Reset()
		{
			AtmelContext.R[Index] = 0;
		}
	}

	
	public delegate void RegisterChangedHandler(int oldVal, int newVal);
	public delegate void RegisterReadHandler(ref int val);

	public class ObservableRegister : Register
	{
		private int Index;

		public event RegisterChangedHandler OnRegisterChanged;
		public event RegisterReadHandler OnRegisterRead;

		public ObservableRegister(int index)
		{
			this.Index = index;
		}

		public int Value
		{
			[MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
			get
			{
				var val = AtmelContext.R[Index];
				if (this.OnRegisterRead != null)
					this.OnRegisterRead(ref val);
				return val;
			}

			[MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
			set
			{
				int oldVal = AtmelContext.R[Index];
				AtmelContext.R[Index] = value & 0xff;
				if (this.OnRegisterChanged != null)
					this.OnRegisterChanged(oldVal, value);
			}
		}

		public int this[int index]
		{
			[MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
			get
			{
				return ((AtmelContext.R[Index] >> index) & 1);
			}

			[MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
			set
			{
				if (value == 0)
					this.Value = this.Value & ~(1 << index);
				else
					this.Value = this.Value | (1 << index);
			}
		}

		public void Reset()
		{
			AtmelContext.R[Index] = 0;
		}
	}

	public class MemoryMappedWordRegister : WordRegister
	{
		private int Index;

		public MemoryMappedWordRegister(int index)
		{
			this.Index = index;
		}

		public ushort Value
		{
			[MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
			get
			{
				int lo = AtmelContext.R[this.Index];
				int hi = AtmelContext.R[this.Index + 1];
				return (ushort)(lo | (hi << 8));
			}

			[MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
			set
			{
				AtmelContext.R[this.Index] = (byte)(value & 0xff);
				AtmelContext.R[this.Index + 1] = (byte)(value >> 8);
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
