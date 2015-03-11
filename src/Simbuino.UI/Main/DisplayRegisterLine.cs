using GalaSoft.MvvmLight;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Simbuino.UI.Main
{
	public class DisplayRegisterLine : ViewModelBase
	{
		private int _Address;
		public int Address
		{
			get { return _Address; }
			set { _Address = value; RaisePropertyChanged(() => this.Address); }
		}

		private ObservableCollection<DisplayRegister> _Registers;
		public ObservableCollection<DisplayRegister> Registers
		{
			get { return _Registers; }
			set { _Registers = value; RaisePropertyChanged(() => this.Registers); }
		}

		public DisplayRegisterLine(int address, int numRegisters)
		{
			this.Address = address;
			this.Registers = new ObservableCollection<DisplayRegister>(
				Enumerable.Range(0, numRegisters)
					.Select(i => new DisplayRegister())
				);
		}

		public string DisplayString
		{
			get
			{
				return String.Format("0x{0:x4}:  {1}  {2}",
					this.Address,
					String.Join(" ", this.Registers.Select(v => String.Format("{0:x2}", v.Value))),
					String.Join("", this.Registers.Select(v => 
						(v.Value < 32) || (v.Value >= 128) ? '.' : (char)v.Value))
				);
			}
		}

		public void UpdateDisplayString()
		{
			RaisePropertyChanged(() => this.DisplayString);
		}
	}
}
