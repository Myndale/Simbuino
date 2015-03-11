using GalaSoft.MvvmLight;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Simbuino.UI.Main
{
	public class DisplayRegister : ViewModelBase
	{
		private int _Value = 0;
		public int Value
		{
			get
			{
				return this._Value;
			}
			set
			{
				this.Changed = (this.Value != value);
				if (this._Value != value)
				{
					this._Value = value;
					RaisePropertyChanged(() => Value);
				}
			}
		}

		private bool _Changed = false;
		public bool Changed
		{
			get
			{
				return this._Changed;
			}
			private set
			{
				if (this._Changed != value)
				{
					this._Changed = value;
					RaisePropertyChanged(() => Changed);
				}
			}
		}
		
	}
}
