using GalaSoft.MvvmLight;
using Simbuino.UI.Avalon;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Simbuino.UI.Main
{
	/// <summary>
	/// View-model for a pane that shows a list of open documents.
	/// </summary>
	public class OpenDocumentsPaneViewModel : AbstractPaneViewModel
	{
		public OpenDocumentsPaneViewModel(MainWindowViewModel mainWindowViewModel)
		{
			if (mainWindowViewModel == null)
			{
				throw new ArgumentNullException("mainWindowViewModel");
			}

			this.MainWindowViewModel = mainWindowViewModel;
			this.MainWindowViewModel.ActiveDocumentChanged += new EventHandler<EventArgs>(MainWindowViewModel_ActiveDocumentChanged);
		}

		/// <summary>
		/// View-models for documents.
		/// </summary>
		public ObservableCollection<TextFileDocumentViewModel> Documents
		{
			get
			{
				return this.MainWindowViewModel.Documents;
			}
		}

		/// <summary>
		/// View-model for the active document.
		/// </summary>
		public TextFileDocumentViewModel ActiveDocument
		{
			get
			{
				return this.MainWindowViewModel.ActiveDocument;
			}
			set
			{
				this.MainWindowViewModel.ActiveDocument = value;
			}
		}

		/// <summary>
		/// Close the currently selected document.
		/// </summary>
		public void CloseSelected()
		{
			var activeDocument = this.MainWindowViewModel.ActiveDocument;
			if (activeDocument != null)
			{
				this.MainWindowViewModel.Documents.Remove(activeDocument);
			}
		}

		/// <summary>
		/// Reference to the main window's view model.
		/// </summary>
		private MainWindowViewModel MainWindowViewModel
		{
			get;
			set;
		}

		/// <summary>
		/// Event raised when the active document in the main window has changed.
		/// </summary>
		private void MainWindowViewModel_ActiveDocumentChanged(object sender, EventArgs e)
		{
			RaisePropertyChanged(() => ActiveDocument);
		}
	}
}
