using GalaSoft.MvvmLight;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Simbuino.UI.Avalon
{
	/// <summary>
	/// Abstract base class for an AvalonDock pane view-model.
	/// </summary>
	public abstract class AbstractPaneViewModel : ViewModelBase
	{
		/// <summary>
		/// Set to 'true' when the pane is visible.
		/// </summary>
		private bool isVisible = true;

		/// <summary>
		/// Set to 'true' when the pane is visible.
		/// </summary>
		public bool IsVisible
		{
			get
			{
				return isVisible;
			}
			set
			{
				if (isVisible == value)
				{
					return;
				}

				isVisible = value;

				RaisePropertyChanged(() => IsVisible);
			}
		}
	}
}
