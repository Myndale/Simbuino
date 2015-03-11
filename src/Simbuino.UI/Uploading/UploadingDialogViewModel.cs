using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using MvvmDialogs.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Simbuino.UI.Uploading
{
	public class UploadingDialogViewModel : ViewModelBase, IUserDialogViewModel
    {
		public bool IsModal { get { return true; } }		
		public event EventHandler DialogClosing;
		public Task UploadTask { get; set; }

        private int _Progress;
        public int Progress
        {
            get { return _Progress; }
            set { _Progress = value; RaisePropertyChanged(() => this.Progress); }
        }

		private CancellationTokenSource Cancel;

		public UploadingDialogViewModel(CancellationTokenSource cancel)
		{
			this.Cancel = cancel;
		}

		public void RequestClose()
		{
			this.Cancel.Cancel();
			if (this.DialogClosing != null)
				this.DialogClosing(this, new EventArgs());
		}

		public ICommand LoadedCommand { get { return new RelayCommand(OnLoaded); } }
		private async void OnLoaded()
		{
			await this.UploadTask;
			this.RequestClose();
		}

		public ICommand CancelCommand { get { return new RelayCommand(CancelCommand_Execute); } }
		private void CancelCommand_Execute()
		{
			this.Cancel.Cancel();
		}


    }
}
