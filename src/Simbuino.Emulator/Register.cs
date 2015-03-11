using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Simbuino.Emulator
{
	// generic base interfaces for classes that store a single byte or word in the AVR

	public interface Register
	{
		int Value { get; set; }
		int this[int index] { get; set; }
		void Reset();
	}

	public interface WordRegister
	{
		ushort Value { get; set; }
		int this[int index] { get; set; }
	}
	
}
