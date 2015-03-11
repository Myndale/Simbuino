using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Simbuino.UI.Main;
using GalaSoft.MvvmLight.Command;
using System.Diagnostics;		// todo: take this out once DI has been added

namespace Simbuino.UI
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		public MainWindow()
		{
			InitializeComponent();
			this.DataContext = new MainWindowViewModel();
			CompositionTarget.Rendering += CompositionTarget_Rendering;
		}

		public ICommand ExitCommand { get { return new RelayCommand(ExitCommand_Execute); } }
		private void ExitCommand_Execute()
		{
			Close();
		}

		private void OnClosing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			(this.DataContext as MainWindowViewModel).Dispose();
		}

		void CompositionTarget_Rendering(object sender, EventArgs e)
		{
			if (!this.IsActive)
				return;

			var mainViewModel = this.DataContext as MainWindowViewModel;
			if (mainViewModel == null)
				return;
			if (mainViewModel.Simulation == null)
				return;
			if (mainViewModel.Simulation.Buttons == null)
				return;

			switch ((int)mainViewModel.Simulation.Lcd.LcdAngle)
			{
				case -90:
					mainViewModel.Simulation.Buttons.Up = Keyboard.IsKeyDown(mainViewModel.LeftKey);
					mainViewModel.Simulation.Buttons.Down = Keyboard.IsKeyDown(mainViewModel.RightKey);
					mainViewModel.Simulation.Buttons.Left = Keyboard.IsKeyDown(mainViewModel.DownKey);
					mainViewModel.Simulation.Buttons.Right = Keyboard.IsKeyDown(mainViewModel.UpKey);
					break;

				case 90:
					mainViewModel.Simulation.Buttons.Up = Keyboard.IsKeyDown(mainViewModel.RightKey);
					mainViewModel.Simulation.Buttons.Down = Keyboard.IsKeyDown(mainViewModel.LeftKey);
					mainViewModel.Simulation.Buttons.Left = Keyboard.IsKeyDown(mainViewModel.UpKey);
					mainViewModel.Simulation.Buttons.Right = Keyboard.IsKeyDown(mainViewModel.DownKey);
					break;

				case 180:
					mainViewModel.Simulation.Buttons.Up = Keyboard.IsKeyDown(mainViewModel.DownKey);
					mainViewModel.Simulation.Buttons.Down = Keyboard.IsKeyDown(mainViewModel.UpKey);
					mainViewModel.Simulation.Buttons.Left = Keyboard.IsKeyDown(mainViewModel.RightKey);
					mainViewModel.Simulation.Buttons.Right = Keyboard.IsKeyDown(mainViewModel.LeftKey);
					break;

				default:
					mainViewModel.Simulation.Buttons.Up = Keyboard.IsKeyDown(mainViewModel.UpKey);
					mainViewModel.Simulation.Buttons.Down = Keyboard.IsKeyDown(mainViewModel.DownKey);
					mainViewModel.Simulation.Buttons.Left = Keyboard.IsKeyDown(mainViewModel.LeftKey);
					mainViewModel.Simulation.Buttons.Right = Keyboard.IsKeyDown(mainViewModel.RightKey);
					break;
			}
			mainViewModel.Simulation.Buttons.A = Keyboard.IsKeyDown(mainViewModel.AKey);
			mainViewModel.Simulation.Buttons.B = Keyboard.IsKeyDown(mainViewModel.BKey);
			mainViewModel.Simulation.Buttons.C = Keyboard.IsKeyDown(mainViewModel.CKey);
		}
	}
}
