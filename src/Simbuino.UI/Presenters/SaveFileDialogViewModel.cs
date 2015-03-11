using MvvmDialogs.Presenters;
using Simbuino.UI.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Simbuino.UI.Presenters
{
	public class SaveFileDialogPresenter : IDialogBoxPresenter<SaveFileDialogViewModel>
	{
		public void Show(SaveFileDialogViewModel vm)
		{
			var dlg = new SaveFileDialog();

			dlg.FileName = vm.FileName;
			dlg.Filter = vm.Filter;
			dlg.InitialDirectory = vm.InitialDirectory ?? "";
			dlg.RestoreDirectory = vm.RestoreDirectory;
			dlg.Title = vm.Title ?? "";
			dlg.ValidateNames = vm.ValidateNames;

			vm.Result = dlg.ShowDialog();

			vm.FileName = dlg.FileName;
			vm.FileNames = dlg.FileNames;
			vm.Filter = dlg.Filter;
			vm.InitialDirectory = dlg.InitialDirectory;
			vm.RestoreDirectory = dlg.RestoreDirectory;
			vm.Title = dlg.Title;
			vm.ValidateNames = dlg.ValidateNames;
		}
	}
}
