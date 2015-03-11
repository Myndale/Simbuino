using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Simbuino.Emulator
{
	// this class simulates the AVR op codes, it also generates verious look-up tables for displaying them in the GUI
	public static class AtmelProcessor
	{
		public const int ClockSpeed = 16000000;
		private delegate int OpCodeHandler(int opcode);
		private static OpCodeHandler[] OpCodeMap = new OpCodeHandler[0x10000];
		private static string[] OpCodeBits = new string[0x10000];
		private static ParameterHandler[] OpCodeParamHandlers = new ParameterHandler[0x10000];
		private static Dictionary<OpCodeHandler, OpCodeAttribute> HandlerAttribs = new Dictionary<OpCodeHandler, OpCodeAttribute>();
		private static OpCodeAttribute[] OpCodeAttribs = new OpCodeAttribute[0x10000];
		private static bool[] PCModifyMap = new bool[0x10000];
		private static int[] OpCodeSizes = new int[0x10000];
		
		// todo: declare the interrupt table somwhere		
		public static readonly int[] ClockTable = { 1, 2, 4, 8, 16, 32, 64, 128, 256, 1, 1, 1, 1, 1, 1, 1 };
		public static readonly int[] ClockSelectTable = { 0, 1, 8, 64, 256, 1024, 1, 1 };

		static AtmelProcessor()
		{
			InitHandlerMap();
		}

		private static void InitHandlerMap()
		{
			var result = typeof(AtmelProcessor)
				.GetMethods(BindingFlags.NonPublic | BindingFlags.Static)
				.Select(handler => new { Handler = handler, Attribs = handler.GetCustomAttributes<OpCodeAttribute>() })
				.Where(x => x.Attribs.Count() > 0);
			foreach (var x in result)
				InitHandler(Delegate.CreateDelegate(typeof(OpCodeHandler), x.Handler) as OpCodeHandler, x.Attribs);
		}

		private static void InitHandler(OpCodeHandler handler, IEnumerable<OpCodeAttribute> attribs)
		{
			HandlerAttribs[handler] = attribs.Where(a => a.Priority == 0).FirstOrDefault();
			foreach (var attrib in attribs)
			{
				foreach (var bitString in attrib.BitStrings)
				{
					string bits = bitString.Replace(" ", "");
					Debug.Assert(bits.Length == 16);
					InitHandler(0, 0, bits, handler, attrib);
				}
			}
		}

		private static void InitHandler(int bitNum, int value, string bits, OpCodeHandler handler, OpCodeAttribute attrib)
		{
			if (bitNum == 16)
			{
				if ((OpCodeMap[value] == null) || (OpCodeAttribs[value].Priority < attrib.Priority))
				{
					OpCodeMap[value] = handler;
					OpCodeBits[value] = bits;
					PCModifyMap[value] = attrib.ModifiesPC;
					OpCodeSizes[value] = attrib.OpCodeSize;
					OpCodeAttribs[value] = attrib;
				}
				return;
			}
			switch (bits[15-bitNum])
			{
				case '0':
					InitHandler(bitNum + 1, value, bits, handler, attrib);
					break;

				case '1':
					InitHandler(bitNum + 1, value + (1 << bitNum), bits, handler, attrib);
					break;

				default:
					InitHandler(bitNum + 1, value, bits, handler, attrib);
					InitHandler(bitNum + 1, value + (1 << bitNum), bits, handler, attrib);
					break;
			}
		}

		internal static int ReadRamWord(int address)
		{
			int lo = AtmelContext.RAM[address].Value;
			int hi = AtmelContext.RAM[address + 1].Value;
			return hi * 256 + lo;
		}


		static HiPerfTimer ProfileTimer = new HiPerfTimer();
		static Dictionary<OpCodeHandler, long> ProfileCycles = new Dictionary<OpCodeHandler, long>();
		static Dictionary<OpCodeHandler, long> ProfileCalls = new Dictionary<OpCodeHandler, long>();
		static Dictionary<OpCodeHandler, double> ProfileTimes = new Dictionary<OpCodeHandler, double>();

		[Conditional("PROFILE")] 
		public static void StartProfiling()
		{
			foreach (var key in HandlerAttribs.Keys)
			{
				ProfileCycles[key] = 0;
				ProfileCalls[key] = 0;
				ProfileTimes[key] = 0;
			}
		}

		[Conditional("PROFILE")]
		public static void ReportProfiling()
		{
			Console.WriteLine("=================== PROFILE REPORT =====================");
			Console.WriteLine("   Op Code, Total Cycles, Total Calls,  Total Times (uS)");
			var handlers = ProfileTimes.Keys.OrderByDescending(key => ProfileTimes[key]);
			foreach (var key in handlers)
			{
				var attrib = HandlerAttribs[key];
				Console.WriteLine("{0,10}: {1,10}, {2,10}, {3,15}", attrib.Code, ProfileCycles[key], ProfileCalls[key], (ProfileTimes[key] * 1000000).ToString("0.0000"));
			}
		}

		public static void Step()
		{
			// if any timers or interrupts have triggered then go take care of them
			if (AtmelContext.Clock >= AtmelContext.NextTimerEvent)
				UpdateTimers();
			if (AtmelContext.InterruptPending && CheckInterrupts())
			{
				AtmelContext.UpdateInterruptFlags();
				return;
			}
			
			// get the current op code and call its handler
			var opCode = AtmelContext.Flash[AtmelContext.PC];
#if PROFILE
			ProfileTimer.Start();
#endif
			var handler = OpCodeMap[opCode];
			var cycles = handler(opCode);
#if PROFILE
			ProfileTimer.Stop();
			ProfileCycles[handler] += cycles;
			ProfileCalls[handler]++;
			ProfileTimes[handler] += ProfileTimer.Duration;
#endif
			// update the clock and PC (unless the op code modified it itself)
			AtmelContext.Clock += cycles;
			if (!PCModifyMap[opCode])
				AtmelContext.PC += OpCodeSizes[opCode];
			if (AtmelContext.PC >= AtmelContext.Flash.Length)
				AtmelContext.PC %= AtmelContext.Flash.Length;
		}

		// used for debugging. the R array represents bytes but simulation is faster if we store them as ints.
		// this code simply checks to make sure we haven't had an overflow somewhere.
		public static void CheckRAM()
		{
			for (int i=0; i<AtmelContext.R.Length; i++)
			{
				Debug.Assert(AtmelContext.R[i] < 0x100);
			}
		}

		// calculates the address of the next breakpoint when stepping over a CALL
		public static int Next()
		{
			var opCode = AtmelContext.Flash[AtmelContext.PC];
			if (OpCodeMap[opCode] == OpCode_CALL)
				return AtmelContext.PC + OpCodeSizes[opCode];
			return -1;
		}

		// returns all the info for displaying the op-code at a given address in the integrated debugger
		public static void GetOpCodeDetails(int addr, out int size, out string code, out string parameters)
		{
			var opCode = AtmelContext.Flash[addr];
			if (OpCodeMap[opCode] == null)
			{
				size = 2;
				code = "???";
				parameters = "";
				return;
			}
			var handler = OpCodeMap[opCode];
			var attrib = OpCodeAttribs[opCode];
			size = attrib.OpCodeSize;
			code = attrib.Code;
			parameters = attrib.ParamHandler.GetParameters(addr, OpCodeBits[opCode]);
		}

		#region OpCode Handlers

		[OpCode(0, "adc", typeof(RDParameterHandler), "0001 11rd dddd rrrr")]		
		static int OpCode_ADC(int opcode)
		{
			var d = (opcode >> 4) & 0x1f;
			var r = (opcode & 0x0f) + ((opcode >> 5) & 0x10);
			var Rd = AtmelContext.R[d];
			var Rr = AtmelContext.R[r];
			var R = Rd + Rr + AtmelContext.SREG.C;
			var Rd3 = (Rd >> 3) & 1;
			var Rd7 = (Rd >> 7) & 1;
			var not_Rd7 = 1 - Rd7;
			var Rr3 = (Rr >> 3) & 1;
			var Rr7 = (Rr >> 7) & 1;
			var not_Rr7 = 1 - Rr7;
			var R3 = (R >> 3) & 1;
			var not_R3 = 1 - R3;
			var R7 = (R >> 7) & 1;
			var not_R7 = 1 - R7;
			AtmelContext.SREG.H = (Rd3 & Rr3) | (Rr3 & not_R3) | (not_R3 & Rd3);
			AtmelContext.SREG.N = (R >> 7) & 1;
			AtmelContext.SREG.V = (Rd7 & Rr7 & not_R7) | (not_Rd7 & not_Rr7 & R7);
			AtmelContext.SREG.S = AtmelContext.SREG.N ^ AtmelContext.SREG.V;
			AtmelContext.SREG.Z = ((R & 0xff) == 0) ? 1 : 0;
			AtmelContext.SREG.C = (Rd7 & Rr7) | (Rr7 & not_R7) | (not_R7 & Rd7);
			AtmelContext.R[d] = R & 0xff;

			return 1;
		}

		[OpCode(0, "add", typeof(RDParameterHandler), "0000 11rd dddd rrrr")]
		static int OpCode_ADD(int opcode)
		{			
			var d = (opcode >> 4) & 0x1f;
			var r = (opcode & 0x0f) + ((opcode >> 5) & 0x10);
			var R = AtmelContext.R[d] + AtmelContext.R[r];
			var Rd3 = (AtmelContext.R[d] >> 3) & 1;
			var not_Rd3 = 1 - Rd3;
			var Rd7 = (AtmelContext.R[d] >> 7) & 1;
			var not_Rd7 = 1 - Rd7;
			var Rr3 = (AtmelContext.R[r] >> 3) & 1;
			var not_Rr3 = 1 - Rr3;
			var Rr7 = (AtmelContext.R[r] >> 7) & 1;
			var not_Rr7 = 1 - Rr7;
			var R3 = (R >> 3) & 1;
			var not_R3 = 1 - R3;
			var R7 = (R >> 7) & 1;
			var not_R7 = 1 - R7;
			AtmelContext.SREG.H = (Rd3 & Rr3) | (Rr3 & not_R3) | (not_R3 & Rd3);
			AtmelContext.SREG.N = (R >> 7) & 1;
			AtmelContext.SREG.V = (Rd7 & Rr7 & not_R7) | (not_Rd7 & not_Rr7 & R7);
			AtmelContext.SREG.S = AtmelContext.SREG.N ^ AtmelContext.SREG.V;
			AtmelContext.SREG.Z = ((R & 0xff) == 0) ? 1 : 0;
			AtmelContext.SREG.C = (Rd7 & Rr7) | (Rr7 & not_R7) | (not_R7 & Rd7);
			AtmelContext.R[d] = R & 0xff;
			return 1;
		}

		[OpCode(0, "adiw", typeof(KDParameterHandler), "1001 0110 KKdd KKKK")]
		static int OpCode_ADIW(int opcode)
		{
			var d = ((opcode >> 4) & 0x03);
			var K = (opcode & 0x0f) + ((opcode >> 2) & 0x30);
			var lo = 24 + (d << 1);
			var hi = lo + 1;
			var Rd = (AtmelContext.R[hi] << 8) | AtmelContext.R[lo];
			var R = Rd + K;
			var Rdh7 = (Rd >> 15) & 1;
			var not_Rdh7 = 1 - Rdh7;
			var R15 = (R >> 15) & 1;
			var not_R15 = 1 - R15;
			AtmelContext.SREG.N = R15;
			AtmelContext.SREG.V = not_Rdh7 & R15;
			AtmelContext.SREG.S = AtmelContext.SREG.N ^ AtmelContext.SREG.V;
			AtmelContext.SREG.Z = ((R & 0xffff) == 0) ? 1 : 0;
			AtmelContext.SREG.C = not_R15 & Rdh7;
			AtmelContext.R[lo] = R & 0xff;
			AtmelContext.R[hi] = (R >> 8) & 0xff;
			return 2;
		}

		[OpCode(0, "and", typeof(RDParameterHandler), "0010 00rd dddd rrrr")]
		static int OpCode_AND(int opcode)
		{
			var d = (opcode >> 4) & 0x1f;
			var r = (opcode & 0x0f) + ((opcode >> 5) & 0x10);
			var R = AtmelContext.R[d] & AtmelContext.R[r];
			AtmelContext.SREG.Z = ((R & 0xff) == 0) ? 1 : 0;
			AtmelContext.SREG.N = (R >> 7) & 1;
			AtmelContext.SREG.V = 0;
			AtmelContext.SREG.S = AtmelContext.SREG.N ^ AtmelContext.SREG.V;
			AtmelContext.R[d] = R & 0xff;
			return 1;
		}

		[OpCode(0, "andi", typeof(ImmParameterHandler), "0111 KKKK dddd KKKK")]
		static int OpCode_ANDI(int opcode)
		{
			var d = 16 + ((opcode >> 4) & 0x0f);
			var K = (opcode & 0x0f) + ((opcode >> 4) & 0xf0);
			var R = AtmelContext.R[d] & K;
			AtmelContext.SREG.Z = ((R & 0xff) == 0) ? 1 : 0;
			AtmelContext.SREG.N = (R >> 7) & 1;
			AtmelContext.SREG.V = 0;
			AtmelContext.SREG.S = AtmelContext.SREG.N ^ AtmelContext.SREG.V;
			AtmelContext.R[d] = R & 0xff;
			return 1;
		}

		[OpCode(0, "asr", typeof(DParameterHandler), "1001 010d dddd 0101")]
		static int OpCode_ASR(int opcode)
		{
			var d = ((opcode >> 4) & 0x1f);
			var Rd = AtmelContext.R[d];
			var R = (sbyte)Rd >> 1;
			AtmelContext.SREG.Z = ((R & 0xff) == 0) ? 1 : 0;
			AtmelContext.SREG.N = (R >> 7) & 1;
			AtmelContext.SREG.C = Rd & 1;
			AtmelContext.SREG.S = AtmelContext.SREG.N ^ AtmelContext.SREG.V;
			AtmelContext.SREG.V = AtmelContext.SREG.N ^ AtmelContext.SREG.C;
			AtmelContext.R[d] = R & 0xff;
			return 1;
		}

		[OpCode(0, "bclr", typeof(FlagBitParameterHandler), "1001 0100 1sss 1000")]
		[OpCode(1, "clc", typeof(NoParametersHandler), "1001 0100 1000 1000")]
		[OpCode(1, "clh", typeof(NoParametersHandler), "1001 0100 1101 1000")]
		[OpCode(1, "cli", typeof(NoParametersHandler), "1001 0100 1111 1000")]
		[OpCode(1, "cln", typeof(NoParametersHandler), "1001 0100 1010 1000")]
		[OpCode(1, "clr", typeof(NoParametersHandler), "1001 0100 1010 1000")]
		[OpCode(1, "cls", typeof(NoParametersHandler), "1001 0100 1100 1000")]
		[OpCode(1, "clt", typeof(NoParametersHandler), "1001 0100 1110 1000")]
		[OpCode(1, "clv", typeof(NoParametersHandler), "1001 0100 1011 1000")]
		[OpCode(1, "clz", typeof(NoParametersHandler), "1001 0100 1001 1000")]
		static int OpCode_BCLR(int opcode)
		{
			var s = (opcode >> 4) & 0x07;
			AtmelContext.SREG.Value = (byte)(AtmelContext.SREG.Value & ~(1 << s));
			return 1;
		}

		[OpCode(0, "bld", typeof(DBParameterHandler), "1111 100d dddd 0bbb")]
		static int OpCode_BLD(int opcode)
		{
			var d = (opcode >> 4) & 0x1f;
			int b = opcode & 0x07;
			if (AtmelContext.SREG.T == 0)
				AtmelContext.R[d] &= ~(1 << b);
			else
				AtmelContext.R[d] |= (1 << b);
			return 1;
		}

		[OpCode(0, "brbc", typeof(KSParameterHandler), true, "1111 01kk kkkk ksss")]
		[OpCode(1, "brcc", typeof(KParameterHandler), true, "1111 01kk kkkk k000")]
		[OpCode(1, "brge", typeof(KParameterHandler), true, "1111 01kk kkkk k100")]
		[OpCode(1, "brhc", typeof(KParameterHandler), true, "1111 01kk kkkk k101")]
		[OpCode(1, "brid", typeof(KParameterHandler), true, "1111 01kk kkkk k111")]
		[OpCode(1, "brne", typeof(KParameterHandler), true, "1111 01kk kkkk k001")]
		[OpCode(1, "brpl", typeof(KParameterHandler), true, "1111 01kk kkkk k010")]
		//[OpCode(0, "brsh", typeof(KParameterHandler), true, "1111 01kk kkkk k000")]
		[OpCode(1, "brtc", typeof(KParameterHandler), true, "1111 01kk kkkk k110")]
		[OpCode(1, "brvc", typeof(KParameterHandler), true, "1111 01kk kkkk k011")]
		static int OpCode_BRBC(int opcode)
		{
			// no flags set
			var s = opcode & 7;
			var k = (opcode << 22) >> 25;
			if (AtmelContext.SREG[s] == 0)
			{
				AtmelContext.PC += k + 1;
				return 2;
			}
			else
			{
				AtmelContext.PC++;
				return 1;
			}
		}

		[OpCode(0, "brbs", typeof(KSParameterHandler), true, "1111 00kk kkkk ksss")]
		[OpCode(1, "brcs", typeof(KParameterHandler), true, "1111 00kk kkkk k000")]
		[OpCode(1, "breq", typeof(KParameterHandler), true, "1111 00kk kkkk k001")]
		[OpCode(1, "brhs", typeof(KParameterHandler), true, "1111 00kk kkkk k101")]
		[OpCode(1, "brie", typeof(KParameterHandler), true, "1111 00kk kkkk k111")]
		//[OpCode(0, "brlo", typeof(KParameterHandler), true, "1111 00kk kkkk k000")]
		[OpCode(1, "brlt", typeof(KParameterHandler), true, "1111 00kk kkkk k100")]
		[OpCode(1, "brmi", typeof(KParameterHandler), true, "1111 00kk kkkk k010")]
		[OpCode(1, "brts", typeof(KParameterHandler), true, "1111 00kk kkkk k110")]
		[OpCode(1, "brvs", typeof(KParameterHandler), true, "1111 00kk kkkk k011")]
		static int OpCode_BRBS(int opcode)
		{
			// no flags set
			var s = opcode & 7;
			var k = (opcode << 22) >> 25;
			if (AtmelContext.SREG[s] != 0)
			{
				AtmelContext.PC += k + 1;
				return 2;
			}
			else
			{
				AtmelContext.PC++;
				return 1;
			}
		}

		[OpCode(0, "break", typeof(NoParametersHandler), "1001 0101 1001 1000")]
		static int OpCode_BREAK(int opcode)
		{
			throw new NotImplementedException();
		}

		[OpCode(0, "bset", typeof(FlagBitParameterHandler), "1001 0100 0sss 1000")]
		[OpCode(1, "sec", typeof(NoParametersHandler), "1001 0100 0000 1000")]
		[OpCode(1, "seh", typeof(NoParametersHandler), "1001 0100 0101 1000")]
		[OpCode(1, "sei", typeof(NoParametersHandler), "1001 0100 0111 1000")]
		[OpCode(1, "sen", typeof(NoParametersHandler), "1001 0100 0010 1000")]
		[OpCode(1, "ses", typeof(NoParametersHandler), "1001 0100 0100 1000")]
		[OpCode(1, "set", typeof(NoParametersHandler), "1001 0100 0110 1000")]
		[OpCode(1, "sev", typeof(NoParametersHandler), "1001 0100 0011 1000")]
		[OpCode(1, "sez", typeof(NoParametersHandler), "1001 0100 0001 1000")]
		static int OpCode_BSET(int opcode)
		{
			var s = (opcode >> 4) & 0x07;
			AtmelContext.SREG.Value = (byte)(AtmelContext.SREG.Value | (1 << s));
			return 1;
		}

		[OpCode(0, "bst", typeof(DBParameterHandler), "1111 101d dddd 0bbb")]
		static int OpCode_BST(int opcode)
		{
			var d = (opcode >> 4) & 0x1f;
			int b = opcode & 0x07;
			AtmelContext.SREG.T = (AtmelContext.R[d] >> b) & 1;
			return 1;
		}

		[OpCode(0, "call", typeof(CallParameterHandler), true, 2, "1001 010k kkkk 111k")]
		static int OpCode_CALL(int opcode)
		{
			// calculate the address we're jumping to
			int addr = AtmelContext.Flash[AtmelContext.PC + 1];
			addr += (opcode & 1) << 8;
			addr += ((opcode >> 4) & 0x1f) << 9;

			// push next PC onto the stack
			var nextPC = AtmelContext.PC + 2;
			AtmelContext.RAM[AtmelContext.SP.Value].Value = (byte)(nextPC >> 8);
			AtmelContext.RAM[AtmelContext.SP.Value - 1].Value = nextPC;
			AtmelContext.SP.Value = (ushort)(AtmelContext.SP.Value - 2);

			// no flags set
			AtmelContext.PC = addr;
			return 4;
		}

		[OpCode(0, "cbi", typeof(ABParameterHandler), "1001 1000 AAAA Abbb")]
		static int OpCode_CBI(int opcode)
		{
			var b = opcode & 0x07;
			var A = (opcode >> 3) & 0x1f;
			AtmelContext.IO[A].Value &= ~(1 << b);
			return 1;
		}

		[OpCode(0, "com", typeof(DParameterHandler), "1001 010d dddd 0000")]
		static int OpCode_COM(int opcode)
		{
			var d = (opcode >> 4) & 0x1f;
			int R = 0xff - AtmelContext.R[d];
			AtmelContext.SREG.N = (R >> 7) & 1;
			AtmelContext.SREG.V = 0;
			AtmelContext.SREG.S = AtmelContext.SREG.N ^ AtmelContext.SREG.V;
			AtmelContext.SREG.Z = ((R & 0xff) == 0) ? 1 : 0;
			AtmelContext.SREG.C = 1;
			AtmelContext.R[d] = R & 0xff;
			return 1;
		}

		[OpCode(0, "cp", typeof(RDParameterHandler), "0001 01rd dddd rrrr")]
		static int OpCode_CP(int opcode)
		{
			var d = (opcode >> 4) & 0x1f;
			var r = (opcode & 0x0f) + ((opcode >> 5) & 0x10);
			int R = AtmelContext.R[d] - AtmelContext.R[r];
			var Rd3 = (AtmelContext.R[d] >> 3) & 1;
			var not_Rd3 = 1 - Rd3;
			var Rd7 = (AtmelContext.R[d] >> 7) & 1;
			var not_Rd7 = 1 - Rd7;
			var Rr3 = (AtmelContext.R[r] >> 3) & 1;
			var not_Rr3 = 1 - Rr3;
			var Rr7 = (AtmelContext.R[r] >> 7) & 1;
			var not_Rr7 = 1 - Rr7;
			var R3 = (R >> 3) & 1;
			var not_R3 = 1 - R3;
			var R7 = (R >> 7) & 1;
			var not_R7 = 1 - R7;
			AtmelContext.SREG.H = (not_Rd3 & Rr3) | (Rr3 & R3) | (R3 & not_Rr3);
			AtmelContext.SREG.N = (R >> 7) & 1;
			AtmelContext.SREG.V = (Rd7 & not_Rr7 & not_R7) | (not_Rd7 & Rr7 & R7);
			AtmelContext.SREG.S = AtmelContext.SREG.N ^ AtmelContext.SREG.V;
			AtmelContext.SREG.Z = ((R & 0xff) == 0) ? 1 : 0;
			AtmelContext.SREG.C = (not_Rd7 & Rr7) | (Rr7 & R7) | (R7 & not_Rd7);
			return 1;
		}

		[OpCode(0, "cpc", typeof(RDParameterHandler), "0000 01rd dddd rrrr")]
		static int OpCode_CPC(int opcode)
		{
			var d = (opcode >> 4) & 0x1f;
			var r = (opcode & 0x0f) + ((opcode >> 5) & 0x10);
			int R = AtmelContext.R[d] - AtmelContext.R[r] - AtmelContext.SREG.C;
			var Rd3 = (AtmelContext.R[d] >> 3) & 1;
			var not_Rd3 = 1 - Rd3;
			var Rd7 = (AtmelContext.R[d] >> 7) & 1;
			var not_Rd7 = 1 - Rd7;
			var Rr3 = (AtmelContext.R[r] >> 3) & 1;
			var not_Rr3 = 1 - Rr3;
			var Rr7 = (AtmelContext.R[r] >> 7) & 1;
			var not_Rr7 = 1 - Rr7;
			var R3 = (R >> 3) & 1;
			var not_R3 = 1 - R3;
			var R7 = (R >> 7) & 1;
			var not_R7 = 1 - R7;
			AtmelContext.SREG.H = (not_Rd3 & Rr3) | (Rr3 & R3) | (R3 & not_Rr3);
			AtmelContext.SREG.N = (R >> 7) & 1;
			AtmelContext.SREG.V = (Rd7 & not_Rr7 & not_R7) | (not_Rd7 & Rr7 & R7);
			AtmelContext.SREG.S = AtmelContext.SREG.N ^ AtmelContext.SREG.V;
			AtmelContext.SREG.Z = (R == 0) ? AtmelContext.SREG.Z : 0;
			AtmelContext.SREG.C = (not_Rd7 & Rr7) | (Rr7 & R7) | (R7 & not_Rd7);
			return 1;
		}

		[OpCode(0, "cpi", typeof(ImmParameterHandler), "0011 KKKK dddd KKKK")]
		static int OpCode_CPI(int opcode)
		{
			var d = 16 + ((opcode >> 4) & 0x0f);
			var K = (opcode & 0x0f) + ((opcode >> 4) & 0xf0);
			int R = AtmelContext.R[d] - K;
			var Rd3 = (AtmelContext.R[d] >> 3) & 1;
			var not_Rd3 = 1 - Rd3;
			var Rd7 = (AtmelContext.R[d] >> 7) & 1;
			var not_Rd7 = 1 - Rd7;
			var K3 = (K >> 3) & 1;
			var not_K3 = 1 - K3;
			var K7 = (K >> 7) & 1;
			var not_K7 = 1 - K7;
			var R3 = (R >> 3) & 1;
			var not_R3 = 1 - R3;
			var R7 = (R >> 7) & 1;
			var not_R7 = 1 - R7;
			AtmelContext.SREG.H = (not_Rd3 & K3) | (K3 & R3) | (R3 & not_Rd3);
			AtmelContext.SREG.N = (R >> 7) & 1;
			AtmelContext.SREG.V = (Rd7 & not_K7 & not_R7) | (not_Rd7 & K7 & R7);
			AtmelContext.SREG.S = AtmelContext.SREG.N ^ AtmelContext.SREG.V;
			AtmelContext.SREG.Z = ((R & 0xff) == 0) ? 1 : 0;
			AtmelContext.SREG.C = (not_Rd7 & K7) | (K7 & R7) | (R7 & not_Rd7);
			return 1;
		}

		[OpCode(0, "cpse", typeof(RDParameterHandler), true, "0001 00rd dddd rrrr")]
		static int OpCode_CPSE(int opcode)
		{
			// no flags set
			var d = (opcode >> 4) & 0x1f;
			var r = (opcode & 0x0f) + ((opcode >> 5) & 0x10);
			AtmelContext.PC++;
			if (AtmelContext.R[d] == AtmelContext.R[r])
			{
				// skip
				int nextOpCode = AtmelContext.Flash[AtmelContext.PC];
				int nextOpCodeSize = OpCodeSizes[nextOpCode];
				AtmelContext.PC += nextOpCodeSize;
				return 1 + nextOpCodeSize;
			}
			else
			{
				// no skip
				return 1;				
			}
		}

		[OpCode(0, "dec", typeof(DParameterHandler), "1001 010d dddd 1010")]
		static int OpCode_DEC(int opcode)
		{
			var d = (opcode >> 4) & 0x1f;
			int R = AtmelContext.R[d] - 1;
			AtmelContext.SREG.V = (AtmelContext.R[d] == 0x80) ? 1 : 0;
			AtmelContext.SREG.N = (R >> 7) & 1;
			AtmelContext.SREG.S = AtmelContext.SREG.N ^ AtmelContext.SREG.V;
			AtmelContext.SREG.Z = ((R & 0xff) == 0) ? 1 : 0;
			AtmelContext.R[d] = R & 0xff;
			return 1;
		}

		[OpCode(0, "des", typeof(ParameterHandler), "1001 0100 KKKK 1011")]
		static int OpCode_DES(int opcode)
		{
			throw new NotImplementedException();
		}

		[OpCode(0, "eicall", typeof(ParameterHandler), "1001 0101 0001 1001")]
		static int OpCode_EICALL(int opcode)
		{
			throw new NotImplementedException();
		}

		[OpCode(0, "eijmp", typeof(ParameterHandler), "1001 0100 0001 1001")]
		static int OpCode_EIJMP(int opcode)
		{
			throw new NotImplementedException();
		}

		[OpCode(0, "elpm1", typeof(ParameterHandler), "1001 0101 1101 1000")]
		static int OpCode_ELPM1(int opcode)
		{
			throw new NotImplementedException();
		}

		[OpCode(0, "elpm2", typeof(DParameterHandler), "1001 000d dddd 0110")]
		static int OpCode_ELPM2(int opcode)
		{
			throw new NotImplementedException();
		}

		[OpCode(0, "elpm3", typeof(DParameterHandler), "1001 000d dddd 0111")]
		static int OpCode_ELPM3(int opcode)
		{
			throw new NotImplementedException();
		}

		[OpCode(0, "eor", typeof(RDParameterHandler), "0010 01rd dddd rrrr")]
		static int OpCode_EOR(int opcode)
		{
			var d = (opcode >> 4) & 0x1f;
			var r = (opcode & 0x0f) + ((opcode >> 5) & 0x10);
			var R = AtmelContext.R[d] ^ AtmelContext.R[r];
			AtmelContext.SREG.Z = ((R & 0xff) == 0) ? 1 : 0;
			AtmelContext.SREG.N = (R >> 7) & 1;
			AtmelContext.SREG.V = 0;
			AtmelContext.SREG.S = AtmelContext.SREG.N ^ AtmelContext.SREG.V;
			AtmelContext.R[d] = R & 0xff;
			return 1;
		}

		[OpCode(0, "fmul", typeof(ParameterHandler), "0000 0011 0ddd 1rrr")]
		static int OpCode_FMUL(int opcode)
		{
			throw new NotImplementedException();
		}

		[OpCode(0, "fmuls", typeof(ParameterHandler), "0000 0011 1ddd 0rrr")]
		static int OpCode_FMULS(int opcode)
		{
			throw new NotImplementedException();
		}

		[OpCode(0, "icall", typeof(NoParametersHandler), true, "1001 0101 0000 1001")]
		static int OpCode_ICALL(int opcode)
		{
			// calculate the address we're jumping to
			int addr = AtmelContext.Z.Value;
			
			// push next PC onto the stack
			var nextPC = AtmelContext.PC + 1;
			AtmelContext.RAM[AtmelContext.SP.Value].Value = (byte)(nextPC >> 8);
			AtmelContext.RAM[AtmelContext.SP.Value - 1].Value = nextPC;
			AtmelContext.SP.Value = (ushort)(AtmelContext.SP.Value - 2);

			// no flags set
			AtmelContext.PC = addr;
			return 3;
		}

		[OpCode(0, "ijmp", typeof(NoParametersHandler), true, "1001 0100 0000 1001")]
		static int OpCode_IJMP(int opcode)
		{
			AtmelContext.PC = AtmelContext.Z.Value;
			return 2;

		}

		[OpCode(0, "in", typeof(InParameterHandler), "1011 0AAd dddd AAAA")]
		static int OpCode_IN(int opcode)
		{
			// no flags set
			int A = (opcode & 0x0f) + ((opcode & 0x600) >> 5);
			int r = ((opcode >> 4) & 0x01f);
			AtmelContext.R[r] = AtmelContext.IO[A].Value;
			return 1;
		}

		[OpCode(0, "inc", typeof(DParameterHandler), "1001 010d dddd 0011")]
		static int OpCode_INC(int opcode)
		{
			var d = (opcode >> 4) & 0x1f;
			int R = AtmelContext.R[d] + 1;
			AtmelContext.SREG.V = (AtmelContext.R[d] == 0x7f) ? 1 : 0;
			AtmelContext.SREG.N = (R >> 7) & 1;
			AtmelContext.SREG.S = AtmelContext.SREG.N ^ AtmelContext.SREG.V;
			AtmelContext.SREG.Z = ((R & 0xff) == 0) ? 1 : 0;
			AtmelContext.R[d] = R & 0xff;
			return 1;
		}

		[OpCode(0, "jmp", typeof(JmpParameterHandler), true, 2, "1001 010k kkkk 110k")]
		static int OpCode_JMP(int opcode)
		{
			// no flags set
			int addr = AtmelContext.Flash[AtmelContext.PC + 1];
			addr += (opcode & 1) << 8;
			addr += ((opcode >> 4) & 0x1f) << 9;
			AtmelContext.PC = addr;
			return 3;
		}

		[OpCode(0, "lac", typeof(ParameterHandler), "1001 001r rrrr 0110")]
		static int OpCode_LAC(int opcode)
		{
			throw new NotImplementedException();
		}

		[OpCode(0, "las", typeof(ParameterHandler), "1001 001r rrrr 0101")]
		static int OpCode_LAS(int opcode)
		{
			throw new NotImplementedException();
		}

		[OpCode(0, "lat", typeof(ParameterHandler), "1001 001r rrrr 0111")]
		static int OpCode_LAT(int opcode)
		{
			throw new NotImplementedException();
		}

		[OpCode(0, "ld", typeof(DXParameterHandler), 
			"1001 000d dddd 1100",
			"1001 000d dddd 1101",
			"1001 000d dddd 1110"
		)]
		static int OpCode_LD_X(int opcode)
		{
			// todo: timings in this function need to be fixed
			var d = (opcode >> 4) & 0x1f;
			switch (opcode & 0x0f)
			{
				case 0x0c:			// (i) unchanged
					AtmelContext.R[d] = AtmelContext.RAM[AtmelContext.X.Value].Value;
					return 1;

				case 0x0d:			// (ii) post-increment
					AtmelContext.R[d] = AtmelContext.RAM[AtmelContext.X.Value].Value;
					AtmelContext.X.Value = (ushort)(AtmelContext.X.Value + 1);
					return 2;

				case 0x0e:			// (iii) pre-decrement
					AtmelContext.X.Value = (ushort)(AtmelContext.X.Value - 1);
					AtmelContext.R[d] = AtmelContext.RAM[AtmelContext.X.Value].Value;
					return 3;

				default:
					throw new InvalidOperationException();
			}
		}

		[OpCode(0, "ld", typeof(DYParameterHandler),
			"1000 000d dddd 1000",
			"1001 000d dddd 1001",
			"1001 000d dddd 1010",
			"10q0 qq0d dddd 1qqq")]
		static int OpCode_LD_Y(int opcode)
		{
			var d = (opcode >> 4) & 0x1f;
			var q = ((opcode >> 8) & 0x20) | ((opcode >> 7) & 0x18) | (opcode & 0x07);
			switch (opcode & 0x1007)
			{
				case 0x1001:			// (ii) post-increment
					AtmelContext.R[d] = AtmelContext.RAM[AtmelContext.Y.Value].Value;
					AtmelContext.Y.Value = (ushort)(AtmelContext.Y.Value + 1);
					break;

				case 0x1002:			// (iii) pre-decrement
					AtmelContext.Y.Value = (ushort)(AtmelContext.Y.Value - 1);
					AtmelContext.R[d] = AtmelContext.RAM[AtmelContext.Y.Value].Value;
					break;

				default:			// (i) unchanged and (iv) unchanged with q displacement
					AtmelContext.R[d] = AtmelContext.RAM[AtmelContext.Y.Value + q].Value;
					break;
			}
			return 2;
		}

		[OpCode(0,
			"ld", typeof(DZParameterHandler), 
			"1000 000d dddd 0000",
			"1001 000d dddd 0001",
			"1001 000d dddd 0010",
			"10q0 qq0d dddd 0qqq")]
		static int OpCode_LD_Z(int opcode)
		{
			// todo: timings in this function need to be fixed
			var d = (opcode >> 4) & 0x1f;
			var q = ((opcode >> 8) & 0x20) | ((opcode >> 7) & 0x18) | (opcode & 0x07);
			switch (opcode & 0x1007)
			{
				case 0x1001:			// (ii) post-increment
					AtmelContext.R[d] = AtmelContext.RAM[AtmelContext.Z.Value].Value;
					AtmelContext.Z.Value = (ushort)(AtmelContext.Z.Value + 1);
					break;

				case 0x1002:			// (iii) pre-decrement
					AtmelContext.Z.Value = (ushort)(AtmelContext.Z.Value - 1);
					AtmelContext.R[d] = AtmelContext.RAM[AtmelContext.Z.Value].Value;
					break;

				default:			// // (i) unchanged and (iv) unchanged with q displacement					
					AtmelContext.R[d] = AtmelContext.RAM[AtmelContext.Z.Value + q].Value;
					break;
			}
			return 2;
		}

		[OpCode(0, "ldi", typeof(ImmParameterHandler), "1110 KKKK dddd KKKK")]
		static int OpCode_LDI(int opcode)
		{
			// no flags set
			var d = 16 + ((opcode >> 4) & 0x0f);
			var K = (opcode & 0x0f) + ((opcode >> 4) & 0xf0);
			AtmelContext.R[d] = (byte)K;
			return 1;
		}

		[OpCode(0, "lds", typeof(DataSpaceParameterHandler), 2, "1001 000d dddd 0000")]
		static int OpCode_LDS(int opcode)
		{
			var d = (opcode >> 4) & 0x1f;
			var k = AtmelContext.Flash[AtmelContext.PC + 1];
			AtmelContext.R[d] = AtmelContext.RAM[k].Value;
			return 2;
		}

		/*
		[OpCode(0, "1010 0kkk dddd kkkk")]
		static int OpCode_LDS16(int opcode)
		{
			throw new NotImplementedException();
		}
		 * */

		[OpCode(0, "lpm", typeof(LPMParameterHandler), 
			"1001 0101 1100 1000",
			"1001 000d dddd 0100",
			"1001 000d dddd 0101")]
		static int OpCode_LPM(int opcode)
		{
			var d = (opcode >> 4) & 0x1f;
			var b = AtmelContext.Flash[AtmelContext.Z.Value >> 1];
			if ((AtmelContext.Z.Value & 1) == 1)
				b = (byte)(b >> 8);
			else
				b = (byte)b;
			switch (opcode & 0x0f)
			{
				case 0x08:			// (i)
					AtmelContext.R[0] = (byte)b;
					break;

				case 0x04:			// (ii)
					AtmelContext.R[d] = (byte)b;
					break;

				case 0x05:			// (iii)
					AtmelContext.R[d] = (byte)b;
					AtmelContext.Z.Value = (ushort)(AtmelContext.Z.Value + 1);
					break;

				default:
					throw new InvalidOperationException();
			}
			return 3;
		}

		[OpCode(0, "lsr", typeof(DParameterHandler), "1001 010d dddd 0110")]
		static int OpCode_LSR(int opcode)
		{
			var d = ((opcode >> 4) & 0x1f);
			var Rd = AtmelContext.R[d];
			var R = Rd >> 1;
			AtmelContext.SREG.Z = ((R & 0xff) == 0) ? 1 : 0;
			AtmelContext.SREG.N = 0;
			AtmelContext.SREG.C = Rd & 1;
			AtmelContext.SREG.S = AtmelContext.SREG.N ^ AtmelContext.SREG.V;
			AtmelContext.SREG.V = AtmelContext.SREG.N ^ AtmelContext.SREG.C;
			AtmelContext.R[d] = R & 0xff;
			return 1;
		}

		[OpCode(0, "mov", typeof(RDParameterHandler), "0010 11rd dddd rrrr")]
		static int OpCode_MOV(int opcode)
		{
			// no flags set
			var d = ((opcode >> 4) & 0x1f);
			int r = (opcode & 0x0f) + ((opcode >> 5) & 0x10);
			AtmelContext.R[d] = AtmelContext.R[r];
			return 1;
		}

		[OpCode(0, "movw", typeof(RDPairParameterHandler), "0000 0001 dddd rrrr")]
		static int OpCode_MOVW(int opcode)
		{
			// no flags set
			var d = 2 * ((opcode >> 4) & 0x0f);
			var r = 2 * (opcode & 0x0f);
			AtmelContext.R[d] = AtmelContext.R[r];
			AtmelContext.R[d + 1] = AtmelContext.R[r + 1];
			return 1;
		}

		[OpCode(0, "mul", typeof(RDParameterHandler), "1001 11rd dddd rrrr")]
		static int OpCode_MUL(int opcode)
		{
			var d = (opcode >> 4) & 0x1f;
			var r = (opcode & 0x0f) + ((opcode >> 5) & 0x10);
			var Rd = (int)(byte)AtmelContext.R[d];
			var Rr = (int)(byte)AtmelContext.R[r];
			int R = Rd * Rr;
			AtmelContext.R[0] = R & 0xff;
			AtmelContext.R[1] = (R >> 8) & 0xff;
			AtmelContext.SREG.C = (R >> 15) & 1;
			AtmelContext.SREG.Z = (R == 0) ? 1 : 0;
			return 2;
		}

		[OpCode(0, "muls", typeof(ReducedRDParameterHandler), "0000 0010 dddd rrrr")]
		static int OpCode_MULS(int opcode)
		{
			var d = 16 + ((opcode >> 4) & 0xf);
			var r = 16 + (opcode & 0x0f);
			var Rd = (int)(sbyte)AtmelContext.R[d];
			var Rr = (int)(sbyte)AtmelContext.R[r];
			int R = Rd * Rr;
			AtmelContext.R[0] = R & 0xff;
			AtmelContext.R[1] = (R >> 8) & 0xff;
			AtmelContext.SREG.C = (R >> 15) & 1;
			AtmelContext.SREG.Z = (R == 0) ? 1 : 0;
			return 2;
		}

		[OpCode(0, "mulsu", typeof(TinyRDParameterHandler), "0000 0011 0ddd 0rrr")]
		static int OpCode_MULSU(int opcode)
		{
			var d = 16 + ((opcode >> 4) & 0x07);
			var r = 16 + ((opcode & 0x07));
			var Rd = (int)(sbyte)AtmelContext.R[d];
			var Rr = (int)(byte)AtmelContext.R[r];
			int R = Rd * Rr;
			AtmelContext.R[0] = R & 0xff;
			AtmelContext.R[1] = (R >> 8) & 0xff;
			AtmelContext.SREG.C = (R >> 15) & 1;
			AtmelContext.SREG.Z = (R == 0) ? 1 : 0;
			return 2;
		}

		[OpCode(0, "neg", typeof(DParameterHandler), "1001 010d dddd 0001")]
		static int OpCode_NEG(int opcode)
		{
			var d = ((opcode >> 4) & 0x1f);
			var Rd = AtmelContext.R[d];
			var R = (-Rd) & 0xff;
			AtmelContext.SREG.H = ((Rd & 0x08) | (R & 0x08)) == 0 ? 0 : 1;
			AtmelContext.SREG.V = (R == 0x80) ? 1 : 0;
			AtmelContext.SREG.Z = (R == 0) ? 1 : 0;
			AtmelContext.SREG.N = (R >> 7) & 1;
			AtmelContext.SREG.C = (R == 0) ? 0 : 1;
			AtmelContext.SREG.S = AtmelContext.SREG.N ^ AtmelContext.SREG.V;			
			AtmelContext.R[d] = R;
			return 1;
		}

		[OpCode(0, "nop", typeof(NoParametersHandler), "0000 0000 0000 0000")]
		static int OpCode_NOP(int opcode)
		{
			// no flags
			return 1;
		}

		[OpCode(0, "or", typeof(RDParameterHandler), "0010 10rd dddd rrrr")]
		static int OpCode_OR(int opcode)
		{
			var d = (opcode >> 4) & 0x1f;
			var r = (opcode & 0x0f) + ((opcode >> 5) & 0x10);
			var R = AtmelContext.R[d] | AtmelContext.R[r];
			AtmelContext.SREG.Z = ((R & 0xff) == 0) ? 1 : 0;
			AtmelContext.SREG.N = (R >> 7) & 1;
			AtmelContext.SREG.V = 0;
			AtmelContext.SREG.S = AtmelContext.SREG.N ^ AtmelContext.SREG.V;
			AtmelContext.R[d] = R & 0xff;
			return 1;
		}

		[OpCode(0, "ori", typeof(ImmParameterHandler), "0110 KKKK dddd KKKK")]
		static int OpCode_ORI(int opcode)
		{
			var d = 16 + ((opcode >> 4) & 0x0f);
			var K = (opcode & 0x0f) + ((opcode >> 4) & 0xf0);
			var R = AtmelContext.R[d] | (byte)K;
			AtmelContext.SREG.Z = ((R & 0xff) == 0) ? 1 : 0;
			AtmelContext.SREG.N = (R >> 7) & 1;
			AtmelContext.SREG.V = 0;
			AtmelContext.SREG.S = AtmelContext.SREG.N ^ AtmelContext.SREG.V;
			AtmelContext.R[d] = R & 0xff;
			return 1;
		}

		[OpCode(0, "out", typeof(OutParameterHandler), "1011 1AAr rrrr AAAA")]
		static int OpCode_OUT(int opcode)
		{
			// no flags set
			int A = (opcode & 0x0f) + ((opcode & 0x600) >> 5);
			int r = ((opcode >> 4) & 0x01f);
			AtmelContext.IO[A].Value = AtmelContext.R[r];
			return 1;
		}

		[OpCode(0, "pop", typeof(DParameterHandler), "1001 000d dddd 1111")]
		static int OpCode_POP(int opcode)
		{
			var d = (opcode >> 4) & 0x1f;
			AtmelContext.R[d] = AtmelContext.RAM[AtmelContext.SP.Value + 1].Value;
			AtmelContext.SP.Value = (ushort)(AtmelContext.SP.Value + 1);
			return 2;
		}

		[OpCode(0, "push", typeof(DParameterHandler), "1001 001d dddd 1111")]
		static int OpCode_PUSH(int opcode)
		{
			// no flags set
			var d = (opcode >> 4) & 0x1f;
			AtmelContext.RAM[AtmelContext.SP.Value].Value = AtmelContext.R[d];
			AtmelContext.SP.Value = (ushort)(AtmelContext.SP.Value - 1);
			return 2;
		}

		[OpCode(0, "rcall", typeof(RelativeParameterHandler), true, "1101 kkkk kkkk kkkk")]
		static int OpCode_RCALL(int opcode)
		{
			// calculate the address we're jumping to
			var k = (opcode << 20) >> 20;
			var addr = AtmelContext.PC + k + 1;

			// push next PC onto the stack
			var nextPC = AtmelContext.PC + 1;
			AtmelContext.RAM[AtmelContext.SP.Value].Value = (byte)(nextPC >> 8);
			AtmelContext.RAM[AtmelContext.SP.Value - 1].Value = nextPC;
			AtmelContext.SP.Value = (ushort)(AtmelContext.SP.Value - 2);

			// no flags set
			AtmelContext.PC = addr;
			return 3;
		}

		[OpCode(0, "ret", typeof(NoParametersHandler), true, "1001 0101 0000 1000")]
		static int OpCode_RET(int opcode)
		{
			// no flags set
			AtmelContext.PC = ReadRamWord(AtmelContext.SP.Value + 1);
			AtmelContext.SP.Value = (ushort)(AtmelContext.SP.Value + 2);
			return 4;
		}

		[OpCode(0, "reti", typeof(NoParametersHandler), true, "1001 0101 0001 1000")]
		static int OpCode_RETI(int opcode)
		{
			int hi = AtmelContext.RAM[++AtmelContext.SP.Value].Value;
			int lo = AtmelContext.RAM[++AtmelContext.SP.Value].Value;
			AtmelContext.PC = lo + (hi << 8);
			AtmelContext.SREG.I = 1;
			return 4;
		}

		[OpCode(0, "rjmp", typeof(RelativeParameterHandler), true, "1100 kkkk kkkk kkkk")]
		static int OpCode_RJMP(int opcode)
		{
			// no flags set
			var k = (opcode << 20) >> 20;
			AtmelContext.PC = AtmelContext.PC + k + 1;
			return 2;
		}

		[OpCode(0, "ror", typeof(DParameterHandler), "1001 010d dddd 0111")]
		static int OpCode_ROR(int opcode)
		{
			var d = ((opcode >> 4) & 0x1f);
			var Rd = AtmelContext.R[d];
			var R = (AtmelContext.SREG.C << 7) | (Rd >> 1);
			AtmelContext.SREG.Z = ((R & 0xff) == 0) ? 1 : 0;
			AtmelContext.SREG.N = (R >> 7) & 1;
			AtmelContext.SREG.C = Rd & 1;
			AtmelContext.SREG.S = AtmelContext.SREG.N ^ AtmelContext.SREG.V;
			AtmelContext.SREG.V = AtmelContext.SREG.N ^ AtmelContext.SREG.C;
			AtmelContext.R[d] = R & 0xff;
			return 1;
		}

		[OpCode(0, "sbc", typeof(RDParameterHandler), "0000 10rd dddd rrrr")]
		static int OpCode_SBC(int opcode)
		{
			var d = (opcode >> 4) & 0x1f;
			var r = (opcode & 0x0f) + ((opcode >> 5) & 0x10);
			int R = AtmelContext.R[d] - AtmelContext.R[r] - AtmelContext.SREG.C;
			var Rd3 = (AtmelContext.R[d] >> 3) & 1;
			var not_Rd3 = 1 - Rd3;
			var Rd7 = (AtmelContext.R[d] >> 7) & 1;
			var not_Rd7 = 1 - Rd7;
			var Rr3 = (AtmelContext.R[r] >> 3) & 1;
			var not_Rr3 = 1 - Rr3;
			var Rr7 = (AtmelContext.R[r] >> 7) & 1;
			var not_Rr7 = 1 - Rr7;
			var R3 = (R >> 3) & 1;
			var not_R3 = 1 - R3;
			var R7 = (R >> 7) & 1;
			var not_R7 = 1 - R7;
			AtmelContext.SREG.H = (not_Rd3 & Rr3) | (Rr3 & R3) | (R3 & not_Rr3);
			AtmelContext.SREG.N = (R >> 7) & 1;
			AtmelContext.SREG.V = (Rd7 & not_Rr7 & not_R7) | (not_Rd7 & Rr7 & R7);
			AtmelContext.SREG.S = AtmelContext.SREG.N ^ AtmelContext.SREG.V;
			AtmelContext.SREG.Z = (R == 0) ? AtmelContext.SREG.Z : 0;
			AtmelContext.SREG.C = (not_Rd7 & Rr7) | (Rr7 & R7) | (R7 & not_Rd7);
			AtmelContext.R[d] = R & 0xff;
			return 1;
		}

		[OpCode(0, "sbci", typeof(ImmParameterHandler), "0100 KKKK dddd KKKK")]
		static int OpCode_SBCI(int opcode)
		{
			var d = 16 + ((opcode >> 4) & 0x0f);
			var K = (opcode & 0x0f) + ((opcode >> 4) & 0xf0);
			int R = AtmelContext.R[d] - K - AtmelContext.SREG.C;
			var Rd3 = (AtmelContext.R[d] >> 3) & 1;
			var not_Rd3 = 1 - Rd3;
			var Rd7 = (AtmelContext.R[d] >> 7) & 1;
			var not_Rd7 = 1 - Rd7;
			var K3 = (K >> 3) & 1;
			var not_K3 = 1 - K3;
			var K7 = (K >> 7) & 1;
			var not_K7 = 1 - K7;
			var R3 = (R >> 3) & 1;
			var not_R3 = 1 - R3;
			var R7 = (R >> 7) & 1;
			var not_R7 = 1 - R7;
			AtmelContext.SREG.H = (not_Rd3 & K3) | (K3 & R3) | (R3 & not_Rd3);
			AtmelContext.SREG.V = (Rd7 & not_K7 & not_R7) | (not_Rd7 & K7 & R7);
			AtmelContext.SREG.N = (R >> 7) & 1;
			AtmelContext.SREG.S = AtmelContext.SREG.N ^ AtmelContext.SREG.V;
			AtmelContext.SREG.Z = (R == 0) ? AtmelContext.SREG.Z : 0;
			AtmelContext.SREG.C = (not_Rd7 & K7) | (K7 & R7) | (R7 & not_Rd7);
			AtmelContext.R[d] = R & 0xff;
			return 1;
		}

		[OpCode(0, "sbi", typeof(ABParameterHandler), "1001 1010 AAAA Abbb")]
		static int OpCode_SBI(int opcode)
		{
			var b = opcode & 0x07;
			var A = (opcode >> 3) & 0x1f;
			AtmelContext.IO[A].Value |= (1 << b);
			return 1;
		}

		[OpCode(0, "sbic", typeof(ABParameterHandler), true, "1001 1001 AAAA Abbb")]
		static int OpCode_SBIC(int opcode)
		{
			var b = opcode & 0x07;
			var A = (opcode >> 3) & 0x1f;
			AtmelContext.PC++;
			if ((AtmelContext.IO[A].Value & (1 << b)) == 0)
			{
				// skip
				var nextOpCode = AtmelContext.Flash[AtmelContext.PC];
				AtmelContext.PC += OpCodeSizes[nextOpCode];
				return 2;
			}
			else
			{
				// no skip
				return 1;
			}
		}

		[OpCode(0, "sbis", typeof(ABParameterHandler), true, "1001 1011 AAAA Abbb")]
		static int OpCode_SBIS(int opcode)
		{
			var b = opcode & 0x07;
			var A = (opcode >> 3) & 0x1f;
			AtmelContext.PC++;
			if ((AtmelContext.IO[A].Value & (1 << b)) != 0)
			{
				// skip
				var nextOpCode = AtmelContext.Flash[AtmelContext.PC];
				AtmelContext.PC += OpCodeSizes[nextOpCode];
				return 2;
			}
			else
			{
				// no skip
				return 1;
			}
		}

		[OpCode(0, "sbiw", typeof(KDParameterHandler), "1001 0111 KKdd KKKK")]
		static int OpCode_SBIW(int opcode)
		{
			var d = ((opcode >> 4) & 0x03);
			var K = (opcode & 0x0f) + ((opcode >> 2) & 0x30);
			var lo = 24 + (d << 1);
			var hi = lo + 1;
			var Rd = (AtmelContext.R[hi] << 8) | AtmelContext.R[lo];
			var R = Rd - K;
			var Rdh7 = (Rd >> 15) & 1;
			var not_Rdh7 = 1 - Rdh7;
			var R15 = (R >> 15) & 1;
			var not_R15 = 1 - R15;
			AtmelContext.SREG.N = R15;
			AtmelContext.SREG.V = Rdh7 & not_R15;
			AtmelContext.SREG.S = AtmelContext.SREG.N ^ AtmelContext.SREG.V;
			AtmelContext.SREG.Z = ((R & 0xffff) == 0) ? 1 : 0;
			AtmelContext.SREG.C = R15 & not_Rdh7;
			AtmelContext.R[lo] = (byte)(R);
			AtmelContext.R[hi] = (byte)(R >> 8);
			return 2;
		}

		[OpCode(0, "sbrc", typeof(RBParameterHandler), true, "1111 110r rrrr 0bbb")]
		static int OpCode_SBRC(int opcode)
		{
			var b = opcode & 0x07;
			var r = (opcode >> 4) & 0x1f;
			AtmelContext.PC++;
			if ((AtmelContext.R[r] & (1 << b)) == 0)
			{
				// skip
				var nextOpCode = AtmelContext.Flash[AtmelContext.PC];
				AtmelContext.PC += OpCodeSizes[nextOpCode];
				return 2;
			}
			else
			{
				// no skip
				return 1;
			}
		}

		[OpCode(0, "sbrs", typeof(RBParameterHandler), true, "1111 111r rrrr 0bbb")]
		static int OpCode_SBRS(int opcode)
		{
			var b = opcode & 0x07;
			var r = (opcode >> 4) & 0x1f;
			AtmelContext.PC++;
			if ((AtmelContext.R[r] & (1 << b)) != 0)
			{
				// set
				var nextOpCode = AtmelContext.Flash[AtmelContext.PC];
				AtmelContext.PC += OpCodeSizes[nextOpCode];
				return 2;
			}
			else
			{
				// clear
				return 1;
			}
		}

		[OpCode(0, "sleep", typeof(ParameterHandler), "1001 0101 1000 1000")]
		static int OpCode_SLEEP(int opcode)
		{
			throw new NotImplementedException();
		}

		[OpCode(0, "spm", typeof(ParameterHandler), "1001 0101 111x 1000")]
		static int OpCode_SPM(int opcode)
		{
			throw new NotImplementedException();
		}

		[OpCode(0, "st", typeof(XRParameterHandler), 
			"1001 001r rrrr 1100",
			"1001 001r rrrr 1101",
			"1001 001r rrrr 1110"
			)]
		static int OpCode_ST(int opcode)
		{
			var r = (opcode >> 4) & 0x1f;
			switch (opcode & 0x03)
			{
				case 0x00:			// (i) unchanged
					AtmelContext.RAM[AtmelContext.X.Value].Value = AtmelContext.R[r];
					break;

				case 0x01:			// (ii) post-increment
					AtmelContext.RAM[AtmelContext.X.Value].Value = AtmelContext.R[r];
					AtmelContext.X.Value = (ushort)(AtmelContext.X.Value + 1);
					break;

				case 0x02:			// (iii) pre-decrement
					AtmelContext.X.Value = (ushort)(AtmelContext.X.Value - 1);
					AtmelContext.RAM[AtmelContext.X.Value].Value = AtmelContext.R[r];
					break;

				default:
					throw new InvalidOperationException();
			}
			return 2;
		}

		[OpCode(0, "st", typeof(YRParameterHandler), 
			"1000 001r rrrr 1000",
			"1001 001r rrrr 1001",
			"1001 001r rrrr 1010",
			"10q0 qq1r rrrr 1qqq"
			)]
		static int OpCode_ST_Y(int opcode)
		{
			var r = (opcode >> 4) & 0x1f;
			var q = ((opcode >> 8) & 0x20) | ((opcode >> 7) & 0x18) | (opcode & 0x07);
			switch (opcode & 0x1007)
			{
				case 0x1001:			// (ii) post-increment
					AtmelContext.RAM[AtmelContext.Y.Value].Value = AtmelContext.R[r];
					AtmelContext.Y.Value = (ushort)(AtmelContext.Y.Value + 1);
					break;

				case 0x1002:			// (iii) pre-decrement
					AtmelContext.Y.Value = (ushort)(AtmelContext.Y.Value - 1);
					AtmelContext.RAM[AtmelContext.Y.Value].Value = AtmelContext.R[r];
					break;

				default:			// (i) unchanged and (iv) unchanged with q displacement
					AtmelContext.RAM[AtmelContext.Y.Value + q].Value = AtmelContext.R[r];
					break;
			}
			return 2;
		}

		[OpCode(0, "st", typeof(ZRParameterHandler), 
			"1000 001r rrrr 0000",
			"1001 001r rrrr 0001",
			"1001 001r rrrr 0010",
			"10q0 qq1r rrrr 0qqq"
			)]
		static int OpCode_ST_Z(int opcode)
		{
			var r = (opcode >> 4) & 0x1f;
			var q = ((opcode >> 8) & 0x20) | ((opcode >> 7) & 0x18) | (opcode & 0x07);
			switch (opcode & 0x1007)
			{
				case 0x1001:			// (ii) post-increment
					AtmelContext.RAM[AtmelContext.Z.Value].Value = AtmelContext.R[r];
					AtmelContext.Z.Value = (ushort)(AtmelContext.Z.Value + 1);
					break;

				case 0x1002:			// (iii) pre-decrement
					AtmelContext.Z.Value = (ushort)(AtmelContext.Z.Value - 1);
					AtmelContext.RAM[AtmelContext.Z.Value].Value = AtmelContext.R[r];
					break;

				default:			// (i) unchanged and (iv) unchanged with q displacement
					AtmelContext.RAM[AtmelContext.Z.Value + q].Value = AtmelContext.R[r];
					break;
			}
			return 2;
		}

		[OpCode(0, "sts", typeof(DParameterHandler), 2, "1001 001d dddd 0000")]
		static int OpCode_STS(int opcode)
		{
			// no flags
			var r = (opcode >> 4) & 0x1f;
			var k = AtmelContext.Flash[AtmelContext.PC + 1];
			AtmelContext.RAM[k].Value = AtmelContext.R[r];
			return 2;
		}

		/*
		[OpCode(0, "1010 1kkk dddd kkkk")]
		static int OpCode_STS16(int opcode)
		{
			throw new NotImplementedException();
		}
		 * */

		[OpCode(0, "sub", typeof(RDParameterHandler), "0001 10rd dddd rrrr")]
		static int OpCode_SUB(int opcode)
		{
			var d = (opcode >> 4) & 0x1f;
			var r = (opcode & 0x0f) + ((opcode >> 5) & 0x10);
			int R = AtmelContext.R[d] - AtmelContext.R[r];
			var Rd3 = (AtmelContext.R[d] >> 3) & 1;
			var not_Rd3 = 1 - Rd3;
			var Rd7 = (AtmelContext.R[d] >> 7) & 1;
			var not_Rd7 = 1 - Rd7;
			var Rr3 = (AtmelContext.R[r] >> 3) & 1;
			var not_Rr3 = 1 - Rr3;
			var Rr7 = (AtmelContext.R[r] >> 7) & 1;
			var not_Rr7 = 1 - Rr7;
			var R3 = (R >> 3) & 1;
			var not_R3 = 1 - R3;
			var R7 = (R >> 7) & 1;
			var not_R7 = 1 - R7;
			AtmelContext.SREG.H = (not_Rd3 & Rr3) | (Rr3 & R3) | (R3 & not_Rr3);
			AtmelContext.SREG.N = (R >> 7) & 1;
			AtmelContext.SREG.V = (Rd7 & not_Rr7 & not_R7) | (not_Rd7 & Rr7 & R7);
			AtmelContext.SREG.S = AtmelContext.SREG.N ^ AtmelContext.SREG.V;
			AtmelContext.SREG.Z = ((R & 0xff) == 0) ? 1 : 0;
			AtmelContext.SREG.C = (not_Rd7 & Rr7) | (Rr7 & R7) | (R7 & not_Rd7);
			AtmelContext.R[d] = R & 0xff;
			return 1;
		}

		[OpCode(0, "subi", typeof(ImmParameterHandler), "0101 KKKK dddd KKKK")]
		static int OpCode_SUBI(int opcode)
		{
			var d = 16 + ((opcode >> 4) & 0x0f);
			var K = (opcode & 0x0f) + ((opcode >> 4) & 0xf0);
			int R = AtmelContext.R[d] - K;
			var Rd3 = (AtmelContext.R[d] >> 3) & 1;
			var not_Rd3 = 1 - Rd3;
			var Rd7 = (AtmelContext.R[d] >> 7) & 1;
			var not_Rd7 = 1 - Rd7;
			var K3 = (K >> 3) & 1;
			var not_K3 = 1 - K3;
			var K7 = (K >> 7) & 1;
			var not_K7 = 1 - K7;
			var R3 = (R >> 3) & 1;
			var not_R3 = 1 - R3;
			var R7 = (R >> 7) & 1;
			var not_R7 = 1 - R7;
			AtmelContext.SREG.H = (not_Rd3 & K3) | (K3 & R3) | (R3 & not_Rd3);
			AtmelContext.SREG.V = (Rd7 & not_K7 & not_R7) | (not_Rd7 & K7 & R7);
			AtmelContext.SREG.N = (R >> 7) & 1;
			AtmelContext.SREG.S = AtmelContext.SREG.N ^ AtmelContext.SREG.V;
			AtmelContext.SREG.Z = ((R & 0xff) == 0) ? 1 : 0;
			AtmelContext.SREG.C = (not_Rd7 & K7) | (K7 & R7) | (R7 & not_Rd7);
			AtmelContext.R[d] = R & 0xff;
			return 1;
		}

		[OpCode(0, "swap", typeof(DParameterHandler), "1001 010d dddd 0010")]
		static int OpCode_SWAP(int opcode)
		{
			// no flags
			var d = (opcode >> 4) & 0x1f;
			var hi = AtmelContext.R[d] & 0xf0;
			var lo = AtmelContext.R[d] & 0x0f;
			AtmelContext.R[d] = (byte)((hi >> 4) | (lo << 4));
			return 1;
		}

		[OpCode(0, "wdr", typeof(NoParametersHandler), "1001 0101 1010 1000")]
		static int OpCode_WDR(int opcode)
		{
			//throw new NotImplementedException();
			return 1;
		}

		[OpCode(0, "xch", typeof(RParameterHandler), "1001 001r rrrr 0100")]
		static int OpCode_XCH(int opcode)
		{
			var r = (opcode >> 4) & 0x1f;
			var addr = AtmelContext.Z.Value;
			var R = AtmelContext.R[r];
			AtmelContext.R[r] = (byte)AtmelContext.RAM[addr].Value;
			AtmelContext.RAM[addr].Value = (byte)R;
			return 1;
		}

		#endregion OpCode Handlers		

		private static bool CheckInterrupts()
		{
			// make sure interrupts are enabled
			if (AtmelContext.SREG.I == 0)
				return false;

			// is timer 1 compare signalling an interrupt?
			if (AtmelContext.Timer1OutputCompareFlag)
			{
				// yes, make sure the timer0 interupt flag has been set
				if ((AtmelContext.RAM[AtmelIO.TIFR1][AtmelIO.TOV1]) != 0)
				{
					// generate an interrupt.
					AtmelContext.RAM[AtmelContext.SP.Value--].Value = (byte)AtmelContext.PC;
					AtmelContext.RAM[AtmelContext.SP.Value--].Value = (byte)(AtmelContext.PC >> 8);
					AtmelContext.PC = AtmelInterrupt.TIM1_COMPA;
					AtmelContext.SREG.I = 0;
					AtmelContext.RAM[AtmelIO.TIFR1][AtmelIO.TOV1] = 0;
					AtmelContext.Timer1OutputCompareFlag = false;
					AtmelContext.Clock += 4;
					return true;
				}
			}

			// is timer 1 overflow signalling an interrupt?
			else if (AtmelContext.Timer1OverflowFlag)
			{
				// yes, make sure the timer0 interupt flag has been set
				if ((AtmelContext.RAM[AtmelIO.TIFR1][AtmelIO.TOV1]) != 0)
				{
					// generate an interrupt.
					AtmelContext.RAM[AtmelContext.SP.Value--].Value = (byte)AtmelContext.PC;
					AtmelContext.RAM[AtmelContext.SP.Value--].Value = (byte)(AtmelContext.PC >> 8);
					AtmelContext.PC = AtmelInterrupt.TIM1_OVF;
					AtmelContext.SREG.I = 0;
					AtmelContext.RAM[AtmelIO.TIFR1][AtmelIO.TOV1] = 0;
					AtmelContext.Timer1OverflowFlag = false;
					AtmelContext.Clock += 4;
					return true;
				}
			}

			// is timer 0 signalling an interrupt?
			if (AtmelContext.Timer0OutputCompareFlag)
			{
				// yes, make sure the timer0 interupt flag has been set
				if ((AtmelContext.RAM[AtmelIO.TIFR0][AtmelIO.TOV0]) != 0)
				{
					// generate an interrupt.
					AtmelContext.RAM[AtmelContext.SP.Value--].Value = (byte)AtmelContext.PC;
					AtmelContext.RAM[AtmelContext.SP.Value--].Value = (byte)(AtmelContext.PC >> 8);
					AtmelContext.PC = AtmelInterrupt.TIM0_OVF;
					AtmelContext.SREG.I = 0;
					AtmelContext.RAM[AtmelIO.TIFR0][AtmelIO.TOV0] = 0;
					AtmelContext.Timer0OutputCompareFlag = false;
					AtmelContext.Clock += 4;
					return true;
				}
			}

			// is there a UART interrupt pending?
			if (AtmelContext.UDRE_InterruptPending)
			{
				// generate an interrupt
				AtmelContext.RAM[AtmelContext.SP.Value--].Value = (byte)AtmelContext.PC;
				AtmelContext.RAM[AtmelContext.SP.Value--].Value = (byte)(AtmelContext.PC >> 8);
				AtmelContext.PC = AtmelInterrupt.USART_UDRE;
				AtmelContext.SREG.I = 0;
				USART.UCSR0A[AtmelIO.TXC0] = 0;
				AtmelContext.Clock += 4;				
				return true;
			}

			// is there an SPI interrupt pending?
			if (AtmelContext.SPI_InterruptPending)
			{
				// yes, so generate an interrupt
				AtmelContext.RAM[AtmelContext.SP.Value--].Value = (byte)AtmelContext.PC;
				AtmelContext.RAM[AtmelContext.SP.Value--].Value = (byte)(AtmelContext.PC >> 8);
				AtmelContext.PC = AtmelInterrupt.SPI_STC;
				AtmelContext.SREG.I = 0;
				SPI.SPCR[AtmelIO.SPIF] = 0;
				AtmelContext.Clock += 4;
				return true;
			}

			return false;
		}

		private static void UpdateTimers()
		{
			int cycles = (int)(AtmelContext.Clock - AtmelContext.LastTimerUpdate);
			if (cycles < 0)
				cycles = 0;
			AtmelContext.NextTimerEvent = AtmelContext.Clock + AtmelProcessor.ClockSpeed; // minimum 1 second update for timers
			var clock = ClockTable[AtmelContext.RAM[AtmelIO.CLKPR].Value];

			// timer 0
			var clockSelect0 = ClockSelectTable[AtmelContext.RAM[AtmelIO.TCCR0B].Value & 7];
			var div0 = clock * clockSelect0;
			if (div0 > 0)
			{
				AtmelContext.Timer0 += cycles;
				var wgm0 = (AtmelContext.RAM[AtmelIO.TCCR0A].Value & 0x03) | ((AtmelContext.RAM[AtmelIO.TCCR0B].Value >> 1) & 0x04);
				int tcnt0 = AtmelContext.RAM[AtmelIO.TCNT0].Value;
				tcnt0 += (AtmelContext.Timer0 / div0);
				AtmelContext.Timer0 %= div0;
				if (tcnt0 >= 0x100)
				{
					AtmelContext.RAM[AtmelIO.TIFR0][AtmelIO.TOV0] = 1;
					AtmelContext.Timer0OutputCompareFlag = true;
					AtmelContext.InterruptPending = true;
					tcnt0 &= 0xff;
				}
				AtmelContext.RAM[AtmelIO.TCNT0].Value = tcnt0;
				AtmelContext.NextTimerEvent = Math.Min(AtmelContext.NextTimerEvent, div0 * (0x100 - tcnt0));
			}

			// timer 1
			var clockSelect1 = ClockSelectTable[AtmelContext.RAM[AtmelIO.TCCR1B].Value & 7];
			var div1 = clock * clockSelect1;
			if (div1 > 0)
			{
				// ctc
				AtmelContext.Timer1 += cycles;
				var wgm = (AtmelContext.RAM[AtmelIO.TCCR1A].Value & 0x03) | ((AtmelContext.RAM[AtmelIO.TCCR1B].Value >> 1) & 0x0C);
				switch (wgm)
				{
					// normal mode
					case 0:
					case 8: // todo: this is wrong, give mode 8 its own handler and implement it properly
					{
						int tcnt1h = AtmelContext.RAM[AtmelIO.TCNT1H].Value;
						int tcnt1l = AtmelContext.RAM[AtmelIO.TCNT1L].Value;
						int tcnt1 = tcnt1h * 256 + tcnt1l;
						tcnt1 += (AtmelContext.Timer1 / div1);
						AtmelContext.Timer1 %= div1;
						if (tcnt1 >= 0x10000)
						{
							AtmelContext.RAM[AtmelIO.TIFR1][AtmelIO.TOV1] = 1;
							AtmelContext.Timer1OverflowFlag = true;
							AtmelContext.InterruptPending = true;
							tcnt1 &= 0xffff;
						}
						AtmelContext.RAM[AtmelIO.TCNT1H].Value = tcnt1 / 256;
						AtmelContext.RAM[AtmelIO.TCNT1L].Value = tcnt1 % 256;
						AtmelContext.NextTimerEvent = Math.Min(AtmelContext.NextTimerEvent, div1 * (0x10000 - tcnt1));
					}
					break;

					// Phase correct PWM mode
					case 1:
					case 2:
					case 3:
					case 10:
					case 11:
					{
						// todo: implement this
					}
					break;

					// Fast PWM Mode
					case 5:
					case 6:
					case 7:
					case 14:
					case 15:
					{
						// todo: implement this
					}
					break;

					/*
					// Phase and frequency correct PWM
					case 8:
					{
						// todo: implement this
					}
					break;
						 * */

					// Clear Timer on Compare Match (CTC) Mode
					case 4:					
					case 12:
					{
						int tcnt1h = AtmelContext.RAM[AtmelIO.TCNT1H].Value;
						int tcnt1l = AtmelContext.RAM[AtmelIO.TCNT1L].Value;
						int tcnt1 = tcnt1h * 256 + tcnt1l;
						tcnt1 += (AtmelContext.Timer1 / div1);
						AtmelContext.Timer1 %= div1;
						var OCR1A = AtmelContext.RAM[AtmelIO.OCR1AH].Value * 256 + AtmelContext.RAM[AtmelIO.OCR1AL].Value;
						if (tcnt1 >= OCR1A)
						{
							AtmelContext.RAM[AtmelIO.TIFR1][AtmelIO.TOV1] = 1;
							if (OCR1A == 280)
							{
								AtmelContext.Timer1OutputCompareFlag = true;
								AtmelContext.InterruptPending = true;
							}
							if (OCR1A > 0)
								tcnt1 %= OCR1A;
						}
						AtmelContext.RAM[AtmelIO.TCNT1H].Value = tcnt1 / 256;
						AtmelContext.RAM[AtmelIO.TCNT1L].Value = tcnt1 % 256;
						if (OCR1A > 0)
							AtmelContext.NextTimerEvent = Math.Min(AtmelContext.NextTimerEvent, div1 * (OCR1A - tcnt1));
					}
					break;

				}				
			}

			Debug.Assert(AtmelContext.NextTimerEvent >= 0);
			AtmelContext.NextTimerEvent += AtmelContext.Clock;
			AtmelContext.LastTimerUpdate = AtmelContext.Clock;
		}
		
	}

}
