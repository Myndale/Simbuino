using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using MvvmDialogs.ViewModels;
using Simbuino.UI.Audio;
using Simbuino.UI.Main;
using Simbuino.UI.ViewModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Simbuino.UI.Options
{
	public class OptionsDialogViewModel : ViewModelBase, IUserDialogViewModel
	{
		public bool IsModal { get { return true; } }

		public void RequestClose()
		{
			if (this.DialogClosing != null)
				this.DialogClosing(this, new EventArgs());
		}

		public event EventHandler DialogClosing;

		public MainWindowViewModel MainWindowViewModel { get; private set; }
		public Simulation Simulation { get { return this.MainWindowViewModel.Simulation; } }

		private bool _Persistence;
		public bool Persistence
		{
			get { return _Persistence; }
			set { _Persistence = value; RaisePropertyChanged(() => this.Persistence); }
		}

		private Color _LcdBackground = Color.FromRgb(0x8f, 0xa7, 0x9a);
		public Color LcdBackground
		{
			get {
				return _LcdBackground; }
			set { _LcdBackground = value; RaisePropertyChanged(() => this.LcdBackground); }
		}

		private Color _LcdForeground = Color.FromRgb(0x40, 0x40, 0x40);
		public Color LcdForeground
		{
			get { return _LcdForeground; }
			set { _LcdForeground = value; RaisePropertyChanged(() => this.LcdForeground); }
		}

		private Color _LcdBacklight = Color.FromRgb(0xce, 0xdd, 0xe7);
		public Color LcdBacklight
		{
			get { return _LcdBacklight; }
			set { _LcdBacklight = value; RaisePropertyChanged(() => this.LcdBacklight); }
		}

		private double _LcdAngle = 0;
		public double LcdAngle
		{
			get { return _LcdAngle; }
			set { _LcdAngle = value; RaisePropertyChanged(() => this.LcdAngle); }
		}

		private bool _AudioEnabled;
		public bool AudioEnabled
		{
			get { return _AudioEnabled; }
			set { _AudioEnabled = value; RaisePropertyChanged(() => this.AudioEnabled); }
		}

		private bool _AudioFiltered;
		public bool AudioFiltered
		{
			get { return _AudioFiltered; }
			set { _AudioFiltered = value; RaisePropertyChanged(() => this.AudioFiltered); }
		}

		private Key _UpKey;
		public Key UpKey
		{
			get { return _UpKey; }
			set { _UpKey = value; RaisePropertyChanged(() => this.UpKey); }
		}

		private Key _DownKey;
		public Key DownKey
		{
			get { return _DownKey; }
			set { _DownKey = value; RaisePropertyChanged(() => this.DownKey); }
		}

		private Key _LeftKey;
		public Key LeftKey
		{
			get { return _LeftKey; }
			set { _LeftKey = value; RaisePropertyChanged(() => this.LeftKey); }
		}

		private Key _RightKey;
		public Key RightKey
		{
			get { return _RightKey; }
			set { _RightKey = value; RaisePropertyChanged(() => this.RightKey); }
		}

		private Key _AKey;
		public Key AKey
		{
			get { return _AKey; }
			set { _AKey = value; RaisePropertyChanged(() => this.AKey); }
		}

		private Key _BKey;
		public Key BKey
		{
			get { return _BKey; }
			set { _BKey = value; RaisePropertyChanged(() => this.BKey); }
		}

		private Key _CKey;
		public Key CKey
		{
			get { return _CKey; }
			set { _CKey = value; RaisePropertyChanged(() => this.CKey); }
		}

		private string _ImgFile = "";
		public string ImgFile
		{
			get { return _ImgFile; }
			set { _ImgFile = value; RaisePropertyChanged(() => this.ImgFile); }
		}
		

		public OptionsDialogViewModel(MainWindowViewModel mainWindowViewModel)
		{
			this.MainWindowViewModel = mainWindowViewModel;
			this.Persistence = this.Simulation.Lcd.Persistence;
			this.AudioEnabled = this.Simulation.AudioEnabled;
			this.AudioFiltered = AudioPlayer.Filtered;
			this.UpKey = this.MainWindowViewModel.UpKey;
			this.DownKey = this.MainWindowViewModel.DownKey;
			this.LeftKey = this.MainWindowViewModel.LeftKey;
			this.RightKey = this.MainWindowViewModel.RightKey;
			this.AKey = this.MainWindowViewModel.AKey;
			this.BKey = this.MainWindowViewModel.BKey;
			this.CKey = this.MainWindowViewModel.CKey;
			this.LcdBackground = this.MainWindowViewModel.Simulation.Lcd.LcdBackground;
			this.LcdBacklight = this.MainWindowViewModel.Simulation.Lcd.LcdBacklight;
			this.LcdForeground = this.MainWindowViewModel.Simulation.Lcd.LcdForeground;
			this.LcdAngle = this.MainWindowViewModel.Simulation.Lcd.LcdAngle;
			this.ImgFile = this.MainWindowViewModel.Simulation.SdCard.ImgFile;

			this.MainWindowViewModel.Simulation.BreakAll();
		}

		public ICommand OkCommand { get { return new RelayCommand(OnOk); } }
		private void OnOk()
		{
			this.Simulation.Lcd.Persistence = this.Persistence;
			this.Simulation.AudioEnabled = this.AudioEnabled;
			AudioPlayer.Filtered = this.AudioFiltered;
			this.MainWindowViewModel.UpKey = this.UpKey;
			this.MainWindowViewModel.DownKey = this.DownKey;
			this.MainWindowViewModel.LeftKey = this.LeftKey;
			this.MainWindowViewModel.RightKey = this.RightKey;
			this.MainWindowViewModel.AKey = this.AKey;
			this.MainWindowViewModel.BKey = this.BKey;
			this.MainWindowViewModel.CKey = this.CKey;
			this.MainWindowViewModel.Simulation.Lcd.LcdBackground = this.LcdBackground;
			this.MainWindowViewModel.Simulation.Lcd.LcdBacklight = this.LcdBacklight;
			this.MainWindowViewModel.Simulation.Lcd.LcdForeground = this.LcdForeground;
			this.MainWindowViewModel.Simulation.Lcd.LcdAngle = this.LcdAngle;
			this.MainWindowViewModel.Simulation.SdCard.ImgFile = this.ImgFile;
			this.RequestClose();
		}

		public ICommand CancelCommand { get { return new RelayCommand(OnCancel); } }
		private void OnCancel()
		{
			this.RequestClose();
		}

		public ICommand DefaultKeysCommand { get { return new RelayCommand(OnDefaultKeys); } }
		private void OnDefaultKeys()
		{
			this.UpKey = Key.W;
			this.DownKey = Key.S;
			this.LeftKey = Key.A;
			this.RightKey = Key.D;
			this.AKey = Key.K;
			this.BKey = Key.L;
			this.CKey = Key.R;
		}

		public ICommand DefaultColorsCommand { get { return new RelayCommand(OnDefaultColors); } }
		private void OnDefaultColors()
		{
			this.LcdBackground = Color.FromRgb(0x8f, 0xa7, 0x9a);
			this.LcdForeground = Color.FromRgb(0x40, 0x40, 0x40);
			this.LcdBacklight = Color.FromRgb(0xce, 0xdd, 0xe7);
		}

		public ICommand SelectImgFileCommand { get { return new RelayCommand(OnSelectImgFile); } }
		private void OnSelectImgFile()
		{
			var dlg = new OpenFileDialogViewModel
			{
				Title = "Select IMG file for SD Card emulation",
				Filter = "IMG files (*.img)|*.img|All files (*.*)|*.*",
				FileName = "*.IMG",
				Multiselect = false
			};

			if (dlg.Show(this.MainWindowViewModel.Dialogs))
				this.ImgFile = dlg.FileName;
		}

		private ObservableCollection<Orientation> _Orientations = new ObservableCollection<Orientation>
		{
			new Orientation{Description="Landscape", Angle=0.0},
			new Orientation{Description="Portrait 1", Angle=-90.0},
			new Orientation{Description="Portrat 2", Angle=90.0},
			new Orientation{Description="Inverted", Angle=180.0}
		};
		public ObservableCollection<Orientation> Orientations
		{
			get { return _Orientations; }
			set { _Orientations = value; RaisePropertyChanged(() => this.Orientations); }
		}
	}

	public class Orientation : ViewModelBase
	{
		private string _Description = "";
		public string Description
		{
			get { return _Description; }
			set { _Description = value; RaisePropertyChanged(() => this.Description);}
		}

		private double _Angle = 0.0;
		public double Angle
		{
			get { return _Angle; }
			set { _Angle = value; RaisePropertyChanged(() => this.Angle);}
		}

	}
}
