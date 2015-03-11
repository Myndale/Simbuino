using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Simbuino.Emulator
{
	// this attribute is used to decorate the op-code handlers in the AtmelProcessor class, it contains various information
	// needed by the simulation including bit-pattern strings for generating the op-code jump-tables, a parameter handler
	// for correctly displaying the op code in the gui and info on op-code size and whether or not it modifies the program counter

	[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
	class OpCodeAttribute : Attribute
	{
		public string Code { get; private set; }
		public string[] BitStrings { get; private set; }
		public bool ModifiesPC { get; private set; }
		public int OpCodeSize { get; private set; }
		public ParameterHandler ParamHandler { get; private set; }
		public int Priority {get; private set;}

		public OpCodeAttribute(int priority, string code, Type paramHandler, params string[] bitStrings)
		{
			this.Priority = priority;
			this.Code = code;
			this.ParamHandler = Activator.CreateInstance(paramHandler) as ParameterHandler;
			this.ModifiesPC = false;
			this.BitStrings = bitStrings;
			this.OpCodeSize = 1;
		}

		public OpCodeAttribute(int priority, string code, Type paramHandler, bool modifiesPC, params string[] bitStrings)
		{
			this.Priority = priority;
			this.Code = code;
			this.ParamHandler = Activator.CreateInstance(paramHandler) as ParameterHandler;
			this.ModifiesPC = modifiesPC;
			this.BitStrings = bitStrings;
			this.OpCodeSize = 1;
		}

		public OpCodeAttribute(int priority, string code, Type paramHandler, bool modifiesPC, int opCodeSize, params string[] bitStrings)
		{
			this.Priority = priority;
			this.Code = code;
			this.ParamHandler = Activator.CreateInstance(paramHandler) as ParameterHandler;
			this.ModifiesPC = modifiesPC;
			this.BitStrings = bitStrings;
			this.OpCodeSize = opCodeSize;
		}

		public OpCodeAttribute(int priority, string code, Type paramHandler, int opCodeSize, params string[] bitStrings)
		{
			this.Priority = priority;
			this.Code = code;
			this.ParamHandler = Activator.CreateInstance(paramHandler) as ParameterHandler;
			this.ModifiesPC = false;
			this.BitStrings = bitStrings;
			this.OpCodeSize = opCodeSize;
		}

	}

}
