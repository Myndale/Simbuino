using MvvmDialogs.Presenters;
using Simbuino.UI.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Simbuino.UI.Presenters
{
	public class MessageBoxPresenter : IDialogBoxPresenter<MessageBoxViewModel>
	{
		public void Show(MessageBoxViewModel vm)
		{
			vm.Result = MessageBox.Show(vm.Message, vm.Caption, vm.Buttons, vm.Image);
		}
	}
}
