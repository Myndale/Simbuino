using GalaSoft.MvvmLight;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Simbuino.UI.Main
{
	/// <summary>
	/// The view-model for a text file document.
	/// </summary>
	public class TextFileDocumentViewModel : ViewModelBase
	{
		/// <summary>
		/// The default name for untitled files.
		/// </summary>
		public static readonly string UntitledFileName = "Untitled.txt";

		/// <summary>
		/// The file path of the document.
		/// </summary>
		private string filePath = string.Empty;

		/// <summary>
		/// The text of the document.
		/// </summary>
		private string text = string.Empty;

		/// <summary>
		/// Set to 'true' when the document is modified and needs to be saved.
		/// </summary>
		private bool isModified = false;

		public TextFileDocumentViewModel(string filePath, string text, bool isModified)
		{
			this.filePath = filePath;
			this.text = text;
			this.isModified = isModified;
		}

		/// <summary>
		/// The title of the document.
		/// </summary>
		public string Title
		{
			get
			{
				var title = new StringBuilder();
				title.Append(this.FileName);

				if (this.IsModified)
				{
					title.Append("*");
				}

				return title.ToString();
			}
		}

		/// <summary>
		/// Tooltip to display in the UI.
		/// </summary>
		public string ToolTip
		{
			get
			{
				var toolTip = new StringBuilder();
				if (string.IsNullOrEmpty(this.FilePath))
				{
					toolTip.Append(UntitledFileName);
				}
				else
				{
					toolTip.Append(this.FilePath);
				}

				if (this.IsModified)
				{
					toolTip.Append("*");
				}

				return toolTip.ToString();
			}
		}

		/// <summary>
		/// Name of the file (with the path stripped off).
		/// </summary>
		public string FileName
		{
			get
			{
				if (string.IsNullOrEmpty(this.FilePath))
				{
					return UntitledFileName;
				}

				return System.IO.Path.GetFileName(this.FilePath);
			}
		}

		/// <summary>
		/// The file path of the document.
		/// </summary>
		public string FilePath
		{
			get
			{
				return filePath;
			}
			set
			{
				if (filePath == value)
				{
					return;
				}

				filePath = value;

				RaisePropertyChanged(() => FilePath);
				RaisePropertyChanged(() => FileName);
				RaisePropertyChanged(() => Title);
				RaisePropertyChanged(() => ToolTip);
			}
		}

		/// <summary>
		/// The path to the directory that contains the file.
		/// </summary>
		public string DirectoryPath
		{
			get
			{
				if (string.IsNullOrEmpty(this.FilePath))
				{
					return string.Empty;
				}

				return System.IO.Path.GetDirectoryName(this.FilePath);
			}
		}

		/// <summary>
		/// The text of the document.
		/// </summary>
		public string Text
		{
			get
			{
				return text;
			}
			set
			{
				if (text == value)
				{
					return;
				}

				text = value;

				this.IsModified = true;

				RaisePropertyChanged(() => Text);
			}
		}

		/// <summary>
		/// Set to 'true' when the document is modified and needs to be saved.
		/// </summary>
		public bool IsModified
		{
			get
			{
				return isModified;
			}
			set
			{
				if (isModified == value)
				{
					return;
				}

				isModified = value;

				RaisePropertyChanged(() => IsModified);
				RaisePropertyChanged(() => Title);
				RaisePropertyChanged(() => ToolTip);

				if (this.IsModifiedChanged != null)
				{
					this.IsModifiedChanged(this, EventArgs.Empty);
				}
			}
		}

		/// <summary>
		/// Event raised when the IsModified has changed.
		/// </summary>
		public event EventHandler<EventArgs> IsModifiedChanged;
	}
}
