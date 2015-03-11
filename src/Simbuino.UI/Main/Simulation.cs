using GalaSoft.MvvmLight;
using Simbuino.Emulator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Simbuino.UI.Helpers;
using Simbuino.UI.Disassembly;
using System.Windows.Data;
using System.Collections.ObjectModel;
using Simbuino.UI.Audio;
using System.Timers;
using System.Threading;
using System.Diagnostics;
using System.IO;
using System.Windows.Input;

namespace Simbuino.UI.Main
{
	public class Simulation : ViewModelBase, IDisposable
	{
		public static string Error = "";

		public IList<InstructionViewModel> Disassembly { get; private set; }
		public bool[] Breakpoints;
		public IDictionary<int, InstructionViewModel> Instructions { get; private set; }

		private Task SimulationTask = null;
		private CancellationTokenSource BreakToken = new CancellationTokenSource();

		public event EventHandler ImageChanged;
		public int MinAddr = -1;
		public int MaxAddr = -1;

		private bool _Loaded = false;
		public bool Loaded
		{
			get { return _Loaded; }
			set { _Loaded = value; RaisePropertyChanged(() => this.Loaded); }
		}
		
		private ObservableCollection<InstructionViewModel> _VisibleInstructions;
		public ObservableCollection<InstructionViewModel> VisibleInstructions
		{
			get { return this._VisibleInstructions; }
			set
			{
				if (this._VisibleInstructions != value)
				{
					this._VisibleInstructions = value;
					RaisePropertyChanged(() => VisibleInstructions);
				}
			}
		}

		private InstructionViewModel _SelectedInstruction;
		public InstructionViewModel SelectedInstruction
		{
			get { return this._SelectedInstruction; }
			set
			{
				if (this._SelectedInstruction != value)
				{
					this._SelectedInstruction = value;
					RaisePropertyChanged(() => SelectedInstruction);
				}
			}
		}

		private InstructionViewModel _CurrentInstruction;
		public InstructionViewModel CurrentInstruction
		{
			get { return this._CurrentInstruction; }
			set
			{
				if (this._CurrentInstruction != value)
				{
					this._CurrentInstruction = value;
					RaisePropertyChanged(() => CurrentInstruction);
				}
			}
		}

		// messy! all display regs should be moved to the view model proper

		private DisplayRegister _DisplayPC = new DisplayRegister();
		public DisplayRegister DisplayPC
		{
			get { return this._DisplayPC; }
			set
			{
				if (this._DisplayPC != value)
				{
					this._DisplayPC = value;
					RaisePropertyChanged(() => DisplayPC);
				}
			}
		}

		private long _DisplayClock = 0;
		public long DisplayClock
		{
			get { return this._DisplayClock; }
			set
			{
				if (this._DisplayClock != value)
				{
					this._DisplayClock = value;
					RaisePropertyChanged(() => DisplayClock);
				}
			}
		}

		private long TotalCycles = 0;

		private int _CyclesPerSecond = 0;
		public int CyclesPerSecond
		{
			get { return this._CyclesPerSecond; }
			set
			{
				if (this._CyclesPerSecond != value)
				{
					this._CyclesPerSecond = value;
					RaisePropertyChanged(() => CyclesPerSecond);
				}
			}
		}

		private DisplayRegister _DisplaySREG = new DisplayRegister();
		public DisplayRegister DisplaySREG
		{
			get { return this._DisplaySREG; }
			set
			{
				if (this._DisplaySREG != value)
				{
					this._DisplaySREG = value;
					RaisePropertyChanged(() => DisplaySREG);
				}
			}
		}

		private DisplayRegister _DisplayB = new DisplayRegister();
		public DisplayRegister DisplayB
		{
			get { return this._DisplayB; }
			set
			{
				if (this._DisplayB != value)
				{
					this._DisplayB = value;
					RaisePropertyChanged(() => DisplayB);
				}
			}
		}

		private DisplayRegister _DisplayC = new DisplayRegister();
		public DisplayRegister DisplayC
		{
			get { return this._DisplayC; }
			set
			{
				if (this._DisplayC != value)
				{
					this._DisplayC = value;
					RaisePropertyChanged(() => DisplayC);
				}
			}
		}

		private DisplayRegister _DisplayD = new DisplayRegister();
		public DisplayRegister DisplayD
		{
			get { return this._DisplayD; }
			set
			{
				if (this._DisplayD != value)
				{
					this._DisplayD = value;
					RaisePropertyChanged(() => DisplayD);
				}
			}
		}

		private DisplayRegister _DisplayX = new DisplayRegister();
		public DisplayRegister DisplayX
		{
			get { return this._DisplayX; }
			set
			{
				if (this._DisplayX != value)
				{
					this._DisplayX = value;
					RaisePropertyChanged(() => DisplayX);
				}
			}
		}

		private DisplayRegister _DisplayY = new DisplayRegister();
		public DisplayRegister DisplayY
		{
			get { return this._DisplayY; }
			set
			{
				if (this._DisplayY != value)
				{
					this._DisplayY = value;
					RaisePropertyChanged(() => DisplayY);
				}
			}
		}

		private DisplayRegister _DisplayZ = new DisplayRegister();
		public DisplayRegister DisplayZ
		{
			get { return this._DisplayZ; }
			set
			{
				if (this._DisplayZ != value)
				{
					this._DisplayZ = value;
					RaisePropertyChanged(() => DisplayZ);
				}
			}
		}

		private DisplayRegister _DisplaySP = new DisplayRegister();
		public DisplayRegister DisplaySP
		{
			get { return this._DisplaySP; }
			set
			{
				if (this._DisplaySP != value)
				{
					this._DisplaySP = value;
					RaisePropertyChanged(() => DisplaySP);
				}
			}
		}

		private ObservableCollection<DisplayRegister> _DisplayR;
		public ObservableCollection<DisplayRegister> DisplayR
		{
			get { return this._DisplayR; }
			set
			{
				if (this._DisplayR != value)
				{
					this._DisplayR = value;
					RaisePropertyChanged(() => DisplayR);
				}
			}
		}

		private ObservableCollection<DisplayIORegister> _DisplayIO;
		public ObservableCollection<DisplayIORegister> DisplayIO
		{
			get { return this._DisplayIO; }
			set
			{
				if (this._DisplayIO != value)
				{
					this._DisplayIO = value;
					this.ReversedDisplayIO = new ObservableCollection<DisplayIORegister>(value.Reverse().ToList());
					RaisePropertyChanged(() => DisplayIO);
				}
			}
		}

		private ObservableCollection<DisplayIORegister> _ReversedDisplayIO;
		public ObservableCollection<DisplayIORegister> ReversedDisplayIO
		{
			get { return this._ReversedDisplayIO; }
			set
			{
				if (this._ReversedDisplayIO != value)
				{
					this._ReversedDisplayIO = value;
					RaisePropertyChanged(() => ReversedDisplayIO);
				}
			}
		}

		private ObservableCollection<DisplayRegisterLine> _DisplayFlash;
		public ObservableCollection<DisplayRegisterLine> DisplayFlash
		{
			get { return this._DisplayFlash; }
			set
			{
				if (this._DisplayFlash != value)
				{
					this._DisplayFlash = value;
					RaisePropertyChanged(() => DisplayFlash);
				}
			}
		}

		private ObservableCollection<DisplayRegisterLine> _DisplayRAM;
		public ObservableCollection<DisplayRegisterLine> DisplayRAM
		{
			get { return this._DisplayRAM; }
			set
			{
				if (this._DisplayRAM != value)
				{
					this._DisplayRAM = value;
					RaisePropertyChanged(() => DisplayRAM);
				}
			}
		}

		private ObservableCollection<DisplayRegisterLine> _DisplayEEPROM;
		public ObservableCollection<DisplayRegisterLine> DisplayEEPROM
		{
			get { return this._DisplayEEPROM; }
			set
			{
				if (this._DisplayEEPROM != value)
				{
					this._DisplayEEPROM = value;
					RaisePropertyChanged(() => DisplayEEPROM);
				}
			}
		}

		private SPI _SPI;
		public SPI SPI
		{
			get { return this._SPI; }
			set
			{
				if (this._SPI != value)
				{
					this._SPI = value;
					RaisePropertyChanged(() => SPI);
				}
			}
		}

		private ADC _ADC;
		public ADC ADC
		{
			get { return this._ADC; }
			set
			{
				if (this._ADC != value)
				{
					this._ADC = value;
					RaisePropertyChanged(() => ADC);
				}
			}
		}

		private EEPROM _EEPROM;
		public EEPROM EEPROM
		{
			get { return this._EEPROM; }
			set
			{
				if (this._EEPROM != value)
				{
					this._EEPROM = value;
					RaisePropertyChanged(() => EEPROM);
				}
			}
		}

		private LcdDevice _Lcd;
		public LcdDevice Lcd
		{
			get { return this._Lcd; }
			set
			{
				if (this._Lcd != null)
					this._Lcd.ImageChanged -= _Lcd_ImageChanged;
				if (this._Lcd != value)
				{
					this._Lcd = value;
					RaisePropertyChanged(() => Lcd);
					if (this._Lcd != null)
						this._Lcd.ImageChanged += _Lcd_ImageChanged;
				}
			}
		}

		void _Lcd_ImageChanged(object sender, EventArgs e)
		{
			if (this.ImageChanged != null)
				this.ImageChanged(sender, e);
		}

		private SdDevice _SdCard;
		public SdDevice SdCard
		{
			get { return this._SdCard; }
			set
			{
				if (this._SdCard != value)
				{
					this._SdCard = value;
					RaisePropertyChanged(() => SdCard);
				}
			}
		}

		private Buttons _Buttons;
		public Buttons Buttons
		{
			get { return this._Buttons; }
			set
			{
				if (this._Buttons != value)
				{
					this._Buttons = value;
					RaisePropertyChanged(() => Buttons);
				}
			}
		}

		private bool _AudioEnabled = true;
		public bool AudioEnabled
		{
			get { return this._AudioEnabled; }
			set
			{
				if (this._AudioEnabled != value)
				{
					this._AudioEnabled = value;
					RaisePropertyChanged(() => AudioEnabled);
				}
			}
		}

		private USART _USART;
		public USART USART
		{
			get { return this._USART; }
			set
			{
				if (this._USART != value)
				{
					this._USART = value;
					RaisePropertyChanged(() => USART);
				}
			}
		}


		public Simulation()
		{			
			this.EEPROM = new EEPROM();			
			this.ADC = new ADC();
			this.SPI = new SPI();
			this.Lcd = new LcdDevice(this.SPI);
			this.SdCard = new SdDevice(this.SPI);
			this.Buttons = new Buttons();
			this.USART = new USART();
			this.DisplayR = new ObservableCollection<DisplayRegister>(Enumerable.Range(0, AtmelContext.R.Length).Select(i => new DisplayRegister()).ToArray());
			this.DisplayIO = new ObservableCollection<DisplayIORegister>(Enumerable.Range(0, AtmelContext.NumIO).Select(i => new DisplayIORegister(i)).ToArray());

			var registersPerLine = 16;

			int numFlashLines = (2*AtmelContext.Flash.Length + registersPerLine - 1) / registersPerLine;
			this.DisplayFlash = new ObservableCollection<DisplayRegisterLine>(
				Enumerable.Range(0, numFlashLines).Select(index => new DisplayRegisterLine(index * registersPerLine, registersPerLine)).ToArray());
			
			int numRamLines = (AtmelContext.RAMSize + registersPerLine - 1) / registersPerLine;
			this.DisplayRAM = new ObservableCollection<DisplayRegisterLine>(
				Enumerable.Range(0, numRamLines).Select(index => new DisplayRegisterLine(AtmelContext.FirstSRAM + index * registersPerLine, registersPerLine)).ToArray());

			int numEEpromLines = (AtmelContext.EEPROM.Length + registersPerLine - 1) / registersPerLine;
			this.DisplayEEPROM = new ObservableCollection<DisplayRegisterLine>(
				Enumerable.Range(0, numEEpromLines).Select(index => new DisplayRegisterLine(index * registersPerLine, registersPerLine)).ToArray());
			
			Flash(new string[] {}, true);
		}

		public void Dispose()
		{
			BreakAll();
		}

		public void Flash(IEnumerable<string> firmware, bool newFirmware)
		{
			Simulation.Error = "Error stopping audio player";
			if (this.Loaded)
				Reset();

			Simulation.Error = "Error decoding hex file";
			var decoder = new HexDecoder();
			int minAddr, maxAddr;
			decoder.Decode(firmware, out minAddr, out maxAddr);
			this.MinAddr = minAddr;
			this.MaxAddr = maxAddr;

			// add the disassembled instructions
			Simulation.Error = "Error dissassembling hex file";
			this.Disassembly = new List<InstructionViewModel>();
			this.Instructions = new Dictionary<int, InstructionViewModel>();
			int hide = 0;
			for (int i = 0; i < AtmelContext.Flash.Length; i++)
			{
				var vm = new InstructionViewModel(this, i);
				if (hide > 0)
				{
					vm.Hidden = true;
					hide--;
				}
				else
					hide += vm.Size - 1;
				this.Disassembly.Add(vm);
				this.Instructions[i] = vm;
			}

			Simulation.Error = "Error initializing breakpoints";
			if ((this.Breakpoints == null) || newFirmware)
				this.Breakpoints = new Boolean[this.Disassembly.Count()];
			else
			{
				Array.Resize(ref this.Breakpoints, this.Disassembly.Count());
				for (int i = 0; i < this.Disassembly.Count(); i++)
					this.Disassembly[i].Breakpoint = this.Breakpoints[i];
			}

			this.Loaded = (this.MinAddr >= 0) && (this.MaxAddr < 2*AtmelContext.Flash.Length) && (this.MaxAddr > this.MinAddr);	
			Simulation.Error = "Error performing initial flash update";
			Update();
		}

		public async Task Run() 
		{
			await RunTo(-1);
		}

		public async Task RunTo(int stopPosition)
		{
			lock (this)
			{
				if ((this.SimulationTask != null) && !this.SimulationTask.IsCompleted)
					return;

				this.BreakToken = new CancellationTokenSource();
				this.SimulationTask = Task.Run(() =>
				{
					this.CurrentInstruction = null;
					AtmelProcessor.StartProfiling();
					var timer = new HiPerfTimer();
					Thread.CurrentThread.Priority = ThreadPriority.AboveNormal;										
					var audioEnabled = this.AudioEnabled;
					const double sliceTime = 1.0 / AudioPlayer.AudioSlices;
					double fpsDuration = 0;
					double simDuration = 0;
					timer.Start();					
					if (audioEnabled)
						AudioPlayer.Start();
					while (!this.BreakToken.IsCancellationRequested)
					{
						// simulate cycles for one slice												
						var startCycles = AtmelContext.Clock;
						var endCycles = startCycles + AtmelProcessor.ClockSpeed / AudioPlayer.AudioSlices;
						
						while (AtmelContext.Clock < endCycles)
						{
							AtmelProcessor.Step();
							if (this.Breakpoints[AtmelContext.PC] || (AtmelContext.PC == stopPosition))
							{
								this.BreakToken.Cancel();
								break;
							}

#if PROFILE
							// only do this once-per-instruction in profile build, it's really slow
							if (this.BreakToken.IsCancellationRequested)
							{
								this.BreakToken.Cancel();
								break;
							}
#endif
						}
						timer.Stop();
						var duration = timer.Duration;
						timer.Restart();
						fpsDuration += duration;
						simDuration += duration;
						this.TotalCycles += (endCycles - startCycles);
						if (fpsDuration >= 1.0)
						{
							this.CyclesPerSecond = (int)(this.TotalCycles / fpsDuration);
							this.TotalCycles = 0;
							fpsDuration = 0;
						}

						// if audio is enabled then submit the buffer and wait until it starts playing
						if (audioEnabled)
							AudioPlayer.NextBuffer();

						// otherwise sleep off any left-over time
						else
						{
							var remaining = sliceTime - simDuration;
							if (remaining >= 0)
								Thread.Sleep((int)(1000 * remaining));
							simDuration -= sliceTime;
						}
					}
					if (audioEnabled)
						AudioPlayer.Stop();
					UpdateCurrentInstruction();
					AtmelProcessor.ReportProfiling();
				});
			}

			await this.SimulationTask;
		}

		public void Reset()
		{
			BreakAll();
			Simulation.Error = "Error resetting context in simulation reset";
			AtmelContext.Reset();
			this.Buttons.Reset();
			Simulation.Error = "Error resetting LCD in simulation reset";
			this.Lcd.Reset();
			Simulation.Error = "Error resetting SD card in simulation reset";
			this.SdCard.Reset();
			Simulation.Error = "Error in initial update in simulation reset";
			Update();
			this.MinAddr = this.MaxAddr = -1;
		}

		public void BreakAll()
		{
			// cancel any current simulation task
			lock (this)
			{
				if (this.SimulationTask != null)
				{
					this.BreakToken.Cancel();
					this.SimulationTask.Wait();
				}
			}
		}

		public void StepInto()
		{
			lock (this)
			{
				if ((this.SimulationTask != null) && !this.SimulationTask.IsCompleted)
					return;
				this.BreakToken = new CancellationTokenSource();
				AtmelProcessor.Step();
				UpdateCurrentInstruction();
			}
		}

		public async Task StepOver()
		{
			int stopPosition = -1;
			lock (this)
			{
				if ((this.SimulationTask != null) && !this.SimulationTask.IsCompleted)
					return;

				stopPosition = AtmelProcessor.Next();
				if (stopPosition == -1)
				{
					StepInto();
					return;
				}
			}

			await RunTo(stopPosition);
		}

		public void ToggleBreakpoint()
		{
			if (this.SelectedInstruction != null)
				this.SelectedInstruction.Breakpoint = !this.SelectedInstruction.Breakpoint;
		}

		public void RemoveAllBreakpoints()
		{
			foreach (var instruction in this.Disassembly)
				instruction.Breakpoint = false;
		}

		public void Update()
		{
			this.VisibleInstructions = new ObservableCollection<InstructionViewModel>(this.Disassembly.Where(d => !d.Hidden));
			UpdateCurrentInstruction();
		}

		public void UpdateCurrentInstruction()
		{
			try
			{
				this.CurrentInstruction = this.Instructions[AtmelContext.PC];
				UpdateDisplayRegisters();
			}
			catch (Exception)
			{
				AtmelContext.Reset();
				this.Buttons.Reset();
				Update();
			}
		}

		public void UpdateDisplayRegisters()
		{
			AtmelContext.Active = false;
			this.DisplayPC.Value = 2 * AtmelContext.PC;
			this.DisplayClock = AtmelContext.Clock;
			this.DisplaySREG.Value = AtmelContext.SREG.Value;
			this.DisplayB.Value = AtmelContext.B.Value;
			this.DisplayC.Value = AtmelContext.C.Value;
			this.DisplayD.Value = AtmelContext.D.Value;
			this.DisplayX.Value = AtmelContext.X.Value;
			this.DisplayY.Value = AtmelContext.Y.Value;
			this.DisplayZ.Value = AtmelContext.Z.Value;
			this.DisplaySP.Value = AtmelContext.SP.Value;
			for (int i=0; i<this.DisplayR.Count(); i++)
				this.DisplayR[i].Value = AtmelContext.R[i];
			for (int i = 0; i < this.DisplayIO.Count(); i++)
				this.DisplayIO[i].Value = AtmelContext.IO[i].Value;
			for (int i = 0, addr = 0; i < this.DisplayFlash.Count(); i++)
			{
				for (int j = 0; j < this.DisplayFlash[i].Registers.Count(); )
				{
					this.DisplayFlash[i].Registers[j++].Value = AtmelContext.Flash[addr] & 0xff;
					this.DisplayFlash[i].Registers[j++].Value = AtmelContext.Flash[addr++] >> 8;
				}
				this.DisplayFlash[i].UpdateDisplayString();
			}
			for (int i = 0, addr = AtmelContext.FirstSRAM; i < this.DisplayRAM.Count(); i++)
			{
				for (int j = 0; j < this.DisplayRAM[i].Registers.Count(); j++, addr++)
					this.DisplayRAM[i].Registers[j].Value = AtmelContext.RAM[addr].Value;
				this.DisplayRAM[i].UpdateDisplayString();
			}
			for (int i = 0, addr = 0; i < this.DisplayEEPROM.Count(); i++)
			{
				for (int j = 0; j < this.DisplayEEPROM[i].Registers.Count(); j++, addr++)
					this.DisplayEEPROM[i].Registers[j].Value = AtmelContext.EEPROM[addr];
				this.DisplayEEPROM[i].UpdateDisplayString();
			}
			AtmelContext.Active = true;
			this.Lcd.Refresh(false);
		}

		public void LoadEEPROM()
		{
			try
			{
				var bytes = new int[] {};
				if (!String.IsNullOrEmpty(Simbuino.UI.Properties.Settings.Default.EEPROM))
					bytes = Simbuino.UI.Properties.Settings.Default.EEPROM.Split(new char[] { ',' }).Select(val => Int32.Parse(val)).ToArray();
				if (bytes.Count() == AtmelContext.EEPROM.Count())
					AtmelContext.EEPROM = bytes;
			}
			catch
			{
			}
		}

		public void SaveEEPROM()
		{
			Simbuino.UI.Properties.Settings.Default.EEPROM = String.Join(",", AtmelContext.EEPROM.Select(val => val.ToString()));
		}

		public void ClearEEPROM()
		{
			AtmelContext.ClearEEPROM();
		}

	}
}
