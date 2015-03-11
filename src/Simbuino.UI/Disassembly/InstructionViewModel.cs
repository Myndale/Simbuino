using GalaSoft.MvvmLight;
using Simbuino.Emulator;
using Simbuino.UI.Main;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Simbuino.UI.Disassembly
{
	public class InstructionViewModel : ViewModelBase
	{
		public Simulation Simulation { get; private set; }
		public int Address { get; private set; }
		public int Size { get; private set; }
		public string Code { get; private set; }
		public string Parameters { get; private set; }

		private bool _Hidden;
		public bool Hidden
		{
			get {return this._Hidden;}
			set
			{
				if (this._Hidden != value)
				{
					this._Hidden = value;
					RaisePropertyChanged(() => Hidden);
				}
			}
		}

		private bool _Breakpoint;
		public bool Breakpoint
		{
			get { return this._Breakpoint; }
			set
			{
				if (this._Breakpoint != value)
				{
					this._Breakpoint = value;
					this.Simulation.Breakpoints[this.Address] = value;
					RaisePropertyChanged(() => Breakpoint);
				}
			}
		}

		public InstructionViewModel(Simulation simulation, int addr)
		{
			this.Simulation = simulation;
			this.Address = addr;
			this.Hidden = false;
			this.Breakpoint = false;

			int size;
			string code, parameters;
			AtmelProcessor.GetOpCodeDetails(addr, out size, out code, out parameters);
			this.Size = size;
			this.Code = code;
			this.Parameters = parameters;
		}

		public override string ToString()
		{
			return String.Format("{0:X4}:    {1,-6}   {2}", 2*this.Address, this.Code, this.Parameters);
		}
	}
}
