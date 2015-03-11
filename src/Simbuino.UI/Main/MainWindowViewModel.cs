using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using MvvmDialogs.ViewModels;
using Simbuino.Emulator;
using Simbuino.Hardware;
using Simbuino.UI.Audio;
using Simbuino.UI.Avalon;
using Simbuino.UI.Capture;
using Simbuino.UI.Options;
using Simbuino.UI.Serialization;
using Simbuino.UI.Uploading;
using Simbuino.UI.ViewModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace Simbuino.UI.Main
{
	/// <summary>
	/// Class for the main window's view-model.
	/// </summary>
	public sealed class MainWindowViewModel : ViewModelBase, IDisposable
	{
		private static readonly string defaultTitle = "Simbuino";

		private ObservableCollection<IDialogViewModel> _Dialogs = new ObservableCollection<IDialogViewModel>();
		public ObservableCollection<IDialogViewModel> Dialogs { get { return _Dialogs; } }

		public Key UpKey = Key.W;
		public Key DownKey = Key.S;
		public Key LeftKey = Key.A;
		public Key RightKey = Key.D;
		public Key AKey = Key.K;
		public Key BKey = Key.L;
		public Key CKey = Key.R;

		private const string QUICKSAVE_FILENAME = "simbuino.sav";

		public Simulation Simulation {get; private set;}

		/// <summary>
		/// View-model for the active document.
		/// </summary>
		private TextFileDocumentViewModel activeDocument = null;

		/// <summary>
		/// View-model for the active pane.
		/// </summary>
		private AbstractPaneViewModel activePane = null;

		private List<CaptureFrame> CaptureFrames = new List<CaptureFrame>();

		private bool _Exporting = false;
		public bool Exporting
		{
			get { return _Exporting; }
			set { _Exporting = value; RaisePropertyChanged(() => this.Exporting); }
		}

		private Timer CaptureTimer = new Timer();
		private DispatcherTimer UpdateTimer = new DispatcherTimer();

		private bool _Capturing = false;
		public bool Capturing
		{
			get { return _Capturing; }
			set
			{
				_Capturing = value;
				if (value)
				{
					this.RecordingBlink = true;
					this.CaptureTimer.Start();
				}
				else
				{
					this.CaptureTimer.Stop();
					this.RecordingBlink = false;
				}
				RaisePropertyChanged(() => this.Capturing);
			}
		}

		private bool _RecordingBlink = false;
		public bool RecordingBlink
		{
			get { return _RecordingBlink; }
			set { _RecordingBlink = value; RaisePropertyChanged(() => this.RecordingBlink); }
		}

		private ObservableCollection<string> _SerialPorts;
		public ObservableCollection<string> SerialPorts
		{
			get { return _SerialPorts; }
			set { _SerialPorts = value; RaisePropertyChanged(() => this.SerialPorts); }
		}

		private string _SelectedPort = "";
		public string SelectedPort
		{
			get { return _SelectedPort; }
			set
			{
				_SelectedPort = value;
				RaisePropertyChanged(() => this.SelectedPort);
				this.Device.PortName = value;
			}
		}

		private Device Device;

		public MainWindowViewModel()
		{
			//
			// Initialize the 'Document Overview' pane view-model.
			//
			this.DocumentOverviewPaneViewModel = new DocumentOverviewPaneViewModel(this);

			//
			// Initialize the 'Open Documents' pane view-model.
			//
			this.OpenDocumentsPaneViewModel = new OpenDocumentsPaneViewModel(this);

			//
			// Add view-models for panes to the 'Panes' collection.
			//
			this.Panes = new ObservableCollection<AbstractPaneViewModel>();
			this.Panes.Add(this.DocumentOverviewPaneViewModel);
			this.Panes.Add(this.OpenDocumentsPaneViewModel);

			//
			// Add an example/test document view-model.
			//
			this.Documents = new ObservableCollection<TextFileDocumentViewModel>();
			this.Documents.Add(new TextFileDocumentViewModel(string.Empty, "test data!", true));

			this.Simulation = new Simulation();
			this.Simulation.ImageChanged += Lcd_ImageChanged;

			this.Device = new Device();

			LoadUserSettings();
			RefreshPorts();            

#if DEBUG
			// saves time during development when we're always testing the same app
			// Load(@"c:\temp\GamebuinoGames\test.hex");
#endif

			this.CaptureTimer.Interval = 1000;
			this.CaptureTimer.Tick += CaptureTimer_Tick;

			this.UpdateTimer.Interval = TimeSpan.FromMilliseconds(1000 / 20);
			this.UpdateTimer.Tick += (s, e) => Simulation.UpdateDisplayRegisters();
			this.UpdateTimer.Start();
		}

		public ICommand LoadedCommand { get { return new RelayCommand(OnLoaded); } }
		private async void OnLoaded()
		{
			if (Environment.GetCommandLineArgs().Length == 2)
			{
				var filename = Environment.GetCommandLineArgs().ElementAt(1);
				if (File.Exists(filename))
				{
					Load(filename);
					await this.Simulation.Run();
				}
			}
		}

		public ICommand MenuItemOpenedCommand { get { return new RelayCommand(OnMenuItemOpened); } }
		private void OnMenuItemOpened()
		{
			RefreshPorts();
		}

		private void RefreshPorts()
		{
			var ports = this.Device.AvailablePorts;
			this.SerialPorts = new ObservableCollection<string>(ports);
			if (!this.SerialPorts.Contains(this.SelectedPort))
				this.SelectedPort = (this.SerialPorts.Count == 1) ? this.SerialPorts.First() : "";
		}

		public ICommand SelectPortCommand { get { return new RelayCommand<string>(OnSelectPort); } }
		private void OnSelectPort(string port)
		{
			this.SelectedPort = port;
		}

		public void Dispose()
		{
			this.Device.Dispose();
			SaveUserSettings();
			this.Simulation.Dispose();
		}

		private void LoadUserSettings()
		{
			if (!Key.TryParse(Simbuino.UI.Properties.Settings.Default.UpKey, out this.UpKey))
				this.UpKey = Key.W;
			if (!Key.TryParse(Simbuino.UI.Properties.Settings.Default.DownKey, out this.DownKey))
				this.DownKey = Key.S;
			if (!Key.TryParse(Simbuino.UI.Properties.Settings.Default.LeftKey, out this.LeftKey))
				this.LeftKey = Key.A;
			if (!Key.TryParse(Simbuino.UI.Properties.Settings.Default.RightKey, out this.RightKey))
				this.RightKey = Key.D;
			if (!Key.TryParse(Simbuino.UI.Properties.Settings.Default.AKey, out this.AKey))
				this.AKey = Key.K;
			if (!Key.TryParse(Simbuino.UI.Properties.Settings.Default.BKey, out this.BKey))
				this.BKey = Key.L;
			if (!Key.TryParse(Simbuino.UI.Properties.Settings.Default.CKey, out this.CKey))
				this.CKey = Key.R;

			this.Simulation.Lcd.Persistence = Simbuino.UI.Properties.Settings.Default.Persistence;
			this.Simulation.AudioEnabled = Simbuino.UI.Properties.Settings.Default.AudioEnabled;
			AudioPlayer.Filtered = Simbuino.UI.Properties.Settings.Default.AudioFiltered;

			this.Simulation.Lcd.LcdForeground = Simbuino.UI.Properties.Settings.Default.LcdForeground;
			this.Simulation.Lcd.LcdBackground = Simbuino.UI.Properties.Settings.Default.LcdBackground;
			this.Simulation.Lcd.LcdBacklight = Simbuino.UI.Properties.Settings.Default.LcdBacklight;
			this.Simulation.Lcd.LcdAngle = Simbuino.UI.Properties.Settings.Default.LcdAngle;

			this.Simulation.SdCard.ImgFile = Simbuino.UI.Properties.Settings.Default.ImgFile;
			
			this.SelectedPort = Simbuino.UI.Properties.Settings.Default.HardwarePort;

			this.Simulation.LoadEEPROM();
		}

		private void SaveUserSettings()
		{
			Simbuino.UI.Properties.Settings.Default.UpKey = this.UpKey.ToString();
			Simbuino.UI.Properties.Settings.Default.DownKey = this.DownKey.ToString();
			Simbuino.UI.Properties.Settings.Default.LeftKey = this.LeftKey.ToString();
			Simbuino.UI.Properties.Settings.Default.RightKey = this.RightKey.ToString();
			Simbuino.UI.Properties.Settings.Default.AKey = this.AKey.ToString();
			Simbuino.UI.Properties.Settings.Default.BKey = this.BKey.ToString();
			Simbuino.UI.Properties.Settings.Default.CKey = this.CKey.ToString();

			Simbuino.UI.Properties.Settings.Default.Persistence = this.Simulation.Lcd.Persistence;
			Simbuino.UI.Properties.Settings.Default.AudioEnabled = this.Simulation.AudioEnabled;
			Simbuino.UI.Properties.Settings.Default.AudioFiltered = AudioPlayer.Filtered;

			Simbuino.UI.Properties.Settings.Default.LcdForeground = this.Simulation.Lcd.LcdForeground;
			Simbuino.UI.Properties.Settings.Default.LcdBackground = this.Simulation.Lcd.LcdBackground;
			Simbuino.UI.Properties.Settings.Default.LcdBacklight = this.Simulation.Lcd.LcdBacklight;
			Simbuino.UI.Properties.Settings.Default.LcdAngle = this.Simulation.Lcd.LcdAngle;

			Simbuino.UI.Properties.Settings.Default.ImgFile = this.Simulation.SdCard.ImgFile;

			Simbuino.UI.Properties.Settings.Default.HardwarePort = this.SelectedPort;

			this.Simulation.SaveEEPROM();

			Simbuino.UI.Properties.Settings.Default.Save();
		}

		/// <summary>
		/// The current title of the main window.
		/// </summary>
		public string Title
		{
			get
			{
				var title = new StringBuilder();
				title.Append(defaultTitle);

				if (this.ActiveDocument != null)
				{
					title.Append(" - ");
					title.Append(this.ActiveDocument.Title);
				}

				return title.ToString();
			}
		}

		/// <summary>
		/// View-models for panes.
		/// </summary>
		public ObservableCollection<AbstractPaneViewModel> Panes
		{
			get;
			private set;
		}

		/// <summary>
		/// View-models for documents.
		/// </summary>
		public ObservableCollection<TextFileDocumentViewModel> Documents
		{
			get;
			private set;
		}

		/// <summary>
		/// Returns 'true' if any of the open documents are modified.
		/// </summary>
		public bool AnyDocumentIsModified
		{
			get
			{
				foreach (var document in this.Documents)
				{
					if (document.IsModified)
					{
						return true;
					}
				}

				return false;
			}
		}

		/// <summary>
		/// View-model for the active document.
		/// </summary>
		public TextFileDocumentViewModel ActiveDocument
		{
			get
			{
				return activeDocument;
			}
			set
			{
				if (activeDocument == value)
				{
					return;
				}

				if (activeDocument != null)
				{
					activeDocument.IsModifiedChanged -= new EventHandler<EventArgs>(activeDocument_IsModifiedChanged);
				}

				activeDocument = value;

				if (activeDocument != null)
				{
					activeDocument.IsModifiedChanged += new EventHandler<EventArgs>(activeDocument_IsModifiedChanged);
				}

				RaisePropertyChanged(() => ActiveDocument);
				RaisePropertyChanged(() => Title);

				if (ActiveDocumentChanged != null)
				{
					ActiveDocumentChanged(this, EventArgs.Empty);
				}
			}
		}

		/// <summary>
		/// Event raised when the ActiveDocument property has changed.
		/// </summary>
		public event EventHandler<EventArgs> ActiveDocumentChanged;

		/// <summary>
		/// View-model for the active pane.
		/// </summary>
		public AbstractPaneViewModel ActivePane
		{
			get
			{
				return activePane;
			}
			set
			{
				if (activePane == value)
				{
					return;
				}

				activePane = value;

				RaisePropertyChanged(() => ActivePane);
			}
		}

		/// <summary>
		/// View-model for the 'Document Overview' pane.
		/// </summary>
		public DocumentOverviewPaneViewModel DocumentOverviewPaneViewModel
		{
			get;
			private set;
		}

		/// <summary>
		/// View-model for the 'Open Documents' pane.
		/// </summary>
		public OpenDocumentsPaneViewModel OpenDocumentsPaneViewModel
		{
			get;
			private set;
		}

		/// <summary>
		/// Create a new file and add it to the view-model.
		/// </summary>
		public void NewFile()
		{
			var newDocument = new TextFileDocumentViewModel(string.Empty, string.Empty, false);
			this.Documents.Add(newDocument);
		}

		public void ShowAllPanes()
		{
			foreach (var pane in this.Panes)
			{
				pane.IsVisible = true;
			}
		}

		/// <summary>
		/// Hide all panes.
		/// </summary>
		public void HideAllPanes()
		{
			foreach (var pane in this.Panes)
			{
				pane.IsVisible = false;
			}
		}

		/// <summary>
		/// Called when the application is closing.
		/// Return 'true' to allow application to exit.
		/// </summary>
		public bool OnApplicationClosing()
		{
			if (this.AnyDocumentIsModified)
			{
				/*
				if (!this.DialogProvider.QueryCloseApplicationWhenDocumentsModified())
				{
					//
					// User has cancelled application exit.
					//
					return false;
				}
				 * */
			}

			//
			// Allow application exit to proceed.
			//
			return true;
		}

		/// <summary>
		/// Event raised when the active document's IsModified property has changed.
		/// </summary>
		private void activeDocument_IsModifiedChanged(object sender, EventArgs e)
		{
			//
			// Update the main window's title when the active document has been modified.
			//
			RaisePropertyChanged(() => Title);
		}

		IEnumerable<string> CurrentFirmware;
		string LastLoaded = null;

		public ICommand LoadCommand { get { return new RelayCommand(LoadCommand_Execute); } }
		private void LoadCommand_Execute()
		{
			var dlg = new OpenFileDialogViewModel
			{
				Title = "Select game file to load",
				Filter = "HEX files (*.hex)|*.hex|All files (*.*)|*.*",
				FileName = "*.HEX",
				Multiselect = false
			};

			if (dlg.Show(this.Dialogs))
				Load(dlg.FileName);
		}

		public void Load(string filename)
		{
			try
			{
				Simulation.Error = "Startup error";
				var newFirmware = this.LastLoaded != filename;
				this.LastLoaded = filename;
				Simulation.Error = "Error reading file " + filename;
				var firmware = File.ReadAllLines(filename);
				this.CurrentFirmware = firmware;
				this.Simulation.Flash(firmware, newFirmware);
				this.Simulation.USART.TransmitLog = "";
			}
			catch (Exception e)
			{
				var str = "Failed! Reason: " + Simulation.Error + ". Exception details: " + e.Message;
				if (e.InnerException != null)
					str += " Inner Exception details: " + e.InnerException.Message;
				MessageBox.Show(str);
			}
		}

		public ICommand RunCommand { get { return new RelayCommand(RunCommand_Execute); } }
		private async void RunCommand_Execute()
		{
			await this.Simulation.Run();
		}

		public ICommand ReloadAndRunCommand { get { return new RelayCommand(ReloadAndRunCommand_Execute); } }
		private async void ReloadAndRunCommand_Execute()
		{
			if (this.LastLoaded == null)
				return;
			Load(this.LastLoaded);
			await this.Simulation.Run();
		}

		public ICommand StopDebuggingCommand { get { return new RelayCommand(StopDebuggingCommand_Execute); } }
		private void StopDebuggingCommand_Execute()
		{
			if (this.CurrentFirmware != null)
				this.Simulation.Flash(this.CurrentFirmware, false);
		}

		public ICommand BreakAllCommand { get { return new RelayCommand(BreakAllCommand_Execute); } }
		private void BreakAllCommand_Execute()
		{
			this.Simulation.BreakAll();
		}

		public ICommand StepIntoCommand { get { return new RelayCommand(StepIntoCommand_Execute); } }
		private void StepIntoCommand_Execute()
		{
			this.Simulation.StepInto();
		}

		public ICommand StepOverCommand { get { return new RelayCommand(StepOverCommand_Execute); } }
		private async void StepOverCommand_Execute()
		{
			await this.Simulation.StepOver();
		}

		public ICommand ToggleBreakpointCommand { get { return new RelayCommand(ToggleBreakpointCommand_Execute); } }
		private void ToggleBreakpointCommand_Execute()
		{
			this.Simulation.ToggleBreakpoint();
		}

		public ICommand RemoveAllBreakpointsCommand { get { return new RelayCommand(RemoveAllBreakpointsCommand_Execute); } }
		private void RemoveAllBreakpointsCommand_Execute()
		{
			this.Simulation.RemoveAllBreakpoints();
		}

		public ICommand OptionsCommand { get { return new RelayCommand(OptionsCommand_Execute); } }
		private void OptionsCommand_Execute()
		{
			this.Dialogs.Add(new OptionsDialogViewModel(this));
		}

		public ICommand ClearEepromCommand { get { return new RelayCommand(ClearEepromCommand_Execute); } }
		private void ClearEepromCommand_Execute()
		{
			this.Simulation.ClearEEPROM();
		}

		public ICommand SaveEepromCommand { get { return new RelayCommand(SaveEepromCommand_Execute); } }
		private void SaveEepromCommand_Execute()
		{
			try
			{
				var dlg = new SaveFileDialogViewModel
				{
					Title = "Save EEPROM",
					Filter = "EEPROM files (*.eep)|*.eep|All files (*.*)|*.*",
					FileName = "*.eep",
				};

				if (dlg.Show(this.Dialogs) == DialogResult.OK)
				{
					BreakAllCommand_Execute();
					var eeprom = AtmelContext.EEPROM
						.Select((x, i) => new { Index = i, Value = x })
						.GroupBy(x => x.Index / 32)
						.Select(x => String.Join(" ", 
								x .Select(v => v.Value).Select(val => String.Format("{0:X2}", val))
						))
						.ToList();
					File.WriteAllLines(dlg.FileName, eeprom);
				}
			}
			catch (Exception e)
			{
				new MessageBoxViewModel("Error saving eeprom: " + e.Message, "Error").Show(this.Dialogs);
			}
		}

		public ICommand LoadEepromCommand { get { return new RelayCommand(LoadEepromCommand_Execute); } }
		private void LoadEepromCommand_Execute()
		{
			try
			{
				var dlg = new OpenFileDialogViewModel
				{
					Title = "Load EEPROM",
					Filter = "EEPROM files (*.eep)|*.eep|All files (*.*)|*.*",
					FileName = "*.eep",
				};

				if (dlg.Show(this.Dialogs))
				{
					BreakAllCommand_Execute();

					var bytes = String.Join(" ", File.ReadAllLines(dlg.FileName))
						.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
						.Select(x => Convert.ToInt32(x, 16))
						.ToArray();

					for (int i=0; i<Math.Min(bytes.Length, 1024); i++)
						AtmelContext.EEPROM[i] = bytes[i] & 0xff;
				}
			}
			catch (Exception e)
			{
				new MessageBoxViewModel("Error loading eeprom: " + e.Message, "Error").Show(this.Dialogs);
			}
		}

		public ICommand QuickSaveCommand { get { return new RelayCommand(QuickSaveCommand_Execute); } }
		private void QuickSaveCommand_Execute()
		{
			ContextSerializer.Save(QUICKSAVE_FILENAME);
		}

		public ICommand QuickRestoreCommand { get { return new RelayCommand(QuickRestoreCommand_Execute); } }
		private void QuickRestoreCommand_Execute()
		{
			ContextSerializer.Restore(QUICKSAVE_FILENAME);
		}

		public ICommand ExportImageCommand { get { return new RelayCommand(ExportImageCommand_Execute); } }
		private void ExportImageCommand_Execute()
		{
			var dlg = new SaveFileDialogViewModel
			{
				Title = "Save Capture",
				Filter = "GIF files (*.gif)|*.gif|All files (*.*)|*.*",
				FileName = "*.gif",
			};

			if (dlg.Show(this.Dialogs) == DialogResult.OK)
			{
				BreakAllCommand_Execute();
				var frames = new List<CaptureFrame> {
					new CaptureFrame
					{
						Backlight = this.Simulation.Lcd.LcdCurrentBacklight,
						Pixels = (byte [])this.Simulation.Lcd.Pixels.Clone()
					}
				};
				var exportDlg = new CaptureExportViewModel(dlg.FileName, frames, this.Simulation.Lcd.LcdAngle);
				this.Dialogs.Add(exportDlg);
			}
		}
		
		public ICommand StartStopCaptureCommand { get { return new RelayCommand(StartStopCaptureCommand_Execute); } }
		private void StartStopCaptureCommand_Execute()
		{
			this.Capturing = !this.Capturing;
			if (!this.Capturing)
			{
				if (this.CaptureFrames.Count() == 0)
				{
					new MessageBoxViewModel("No frames captured, export cancelled!", "Capture Error").Show(this.Dialogs);
					return;
				}
				BreakAllCommand_Execute();
			
				var dlg = new SaveFileDialogViewModel
				{
					Title = "Save Capture",
					Filter = "GIF files (*.gif)|*.hex|All files (*.*)|*.*",
					FileName = "*.gif",
				};

				if (dlg.Show(this.Dialogs) == DialogResult.OK)
				{					
					this.Exporting = true;				
					var exportDlg = new CaptureExportViewModel(dlg.FileName, this.CaptureFrames, this.Simulation.Lcd.LcdAngle);
					this.Dialogs.Add(exportDlg);
					if (exportDlg.Success)
						new MessageBoxViewModel("Export finished!").Show(this.Dialogs);
					this.Exporting = false;
				}

				this.CaptureFrames.Clear();
			}
		}

		

		public ICommand AboutCommand { get { return new RelayCommand(AboutCommand_Execute); } }
		private void AboutCommand_Execute()
		{
			Assembly assembly = Assembly.GetEntryAssembly();
			FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
			var version = fvi.FileVersion;
			var copyright = fvi.LegalCopyright;
			new MessageBoxViewModel("Simbuino v" + version + " - Gamebuino Emulator" + Environment.NewLine + Environment.NewLine + copyright, "About Simbuino").Show(this.Dialogs);
		}

		void Lcd_ImageChanged(object sender, EventArgs e)
		{
			if (!this.Capturing)
				return;
			this.CaptureFrames.Add(new CaptureFrame
			{
				Backlight = this.Simulation.Lcd.LcdCurrentBacklight,
				Pixels = (byte [])this.Simulation.Lcd.Pixels.Clone()
			});
		}

		void CaptureTimer_Tick(object sender, EventArgs e)
		{
			this.RecordingBlink = !this.RecordingBlink;
		}

		public ICommand UploadCommand { get { return new RelayCommand(OnUpload); } }
		private void OnUpload()
		{
			try
			{
				this.Simulation.BreakAll();
				var cancel = new System.Threading.CancellationTokenSource();
				var dlg = new UploadingDialogViewModel(cancel);
				Exception error = null;
				dlg.UploadTask = Task.Run(async () => {
					try
					{
						RefreshPorts();

						await this.Device.Flash(AtmelContext.Flash, this.Simulation.MinAddr, this.Simulation.MaxAddr,
							(progress) => dlg.Progress = progress, cancel);
					}
					catch (Exception ex)
					{
						error = ex;
					}
                });
                this.Dialogs.Add(dlg);
				if (error != null)
					throw error;
			}
			catch (Exception ex)
			{
				new MessageBoxViewModel("Error uploading game: " + ex.Message, "Upload Error").Show(this.Dialogs);
			}
		}
		
		
	}
}
