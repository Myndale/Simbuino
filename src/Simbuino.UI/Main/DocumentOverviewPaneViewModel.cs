using GalaSoft.MvvmLight;
using Simbuino.UI.Avalon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Simbuino.UI.Main
{
	/// <summary>
	/// View-model for the 'Document Overview' pane.
	/// </summary>
	public sealed class DocumentOverviewPaneViewModel : AbstractPaneViewModel
	{
		/// <summary>
		/// The number of lines in the file.
		/// </summary>
		private int lineCount = 0;

		/// <summary>
		/// The word count in the file.
		/// </summary>
		private int wordCount = 0;

		public DocumentOverviewPaneViewModel(MainWindowViewModel mainWindowViewModel)
		{
			if (mainWindowViewModel == null)
			{
				throw new ArgumentNullException("MainWindowViewModel");
			}

			this.MainWindowViewModel = mainWindowViewModel;
			this.MainWindowViewModel.ActiveDocumentChanged += new EventHandler<EventArgs>(MainWindowViewModel_ActiveDocumentChanged);
		}

		/// <summary>
		/// Get the name of the active file.
		/// </summary>
		public string FileName
		{
			get
			{
				if (this.MainWindowViewModel.ActiveDocument == null)
				{
					return string.Empty;
				}

				return this.MainWindowViewModel.ActiveDocument.FileName;
			}
		}

		/// <summary>
		/// Get the path of the directory that contains the active file.
		/// </summary>
		public string DirectoryPath
		{
			get
			{
				if (this.MainWindowViewModel.ActiveDocument == null)
				{
					return string.Empty;
				}

				return this.MainWindowViewModel.ActiveDocument.DirectoryPath;
			}
		}

		/// <summary>
		/// Gets the number of lines in the file.
		/// </summary>
		public int LineCount
		{
			get
			{
				return lineCount;
			}
		}

		/// <summary>
		/// Gets the word count in the file.
		/// </summary>
		public int WordCount
		{
			get
			{
				return wordCount;
			}
		}

		/// <summary>
		/// View-model for main window.
		/// </summary>
		private MainWindowViewModel MainWindowViewModel
		{
			get;
			set;
		}

		/// <summary>
		/// Event raised when the active document in the main window's view model has changed.
		/// </summary>
		private void MainWindowViewModel_ActiveDocumentChanged(object sender, EventArgs e)
		{
			ComputeWordAndLineCount();

			RaisePropertyChanged(() => FileName);
			RaisePropertyChanged(() => DirectoryPath);
			RaisePropertyChanged(() => LineCount);
			RaisePropertyChanged(() => WordCount);
		}

		/// <summary>
		/// Parse the active file and compute word and line counts.
		/// </summary>
		private void ComputeWordAndLineCount()
		{
			wordCount = 0;
			lineCount = 0;

			if (this.MainWindowViewModel.ActiveDocument == null)
			{
				// Nothing to do.
				return;
			}

			string fileText = this.MainWindowViewModel.ActiveDocument.Text;
			int pos = 0;
			bool word = false;

			while (pos < fileText.Length)
			{
				if (fileText[pos] == '\n')
				{
					++lineCount;
				}

				if (Char.IsWhiteSpace(fileText[pos]))
				{
					if (word)
					{
						++wordCount;
						word = false;
					}
				}
				else
				{
					if (!word)
					{
						word = true;
					}
				}

				++pos;
			}

			if (word)
			{
				++wordCount;
			}
		}
	}
}
