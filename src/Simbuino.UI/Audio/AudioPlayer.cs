using SharpDX;
using SharpDX.Multimedia;
using SharpDX.XAudio2;
using Simbuino.Emulator;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Simbuino.UI.Audio
{
	public static class AudioPlayer
	{
		public const int AudioFrequency = 44100;
		public const int AudioSlices = 20; // 1 audio block per default frame 
		public const int SamplesPerSlice = AudioFrequency / AudioSlices;
		public const int CyclesPerSample = AtmelProcessor.ClockSpeed / AudioFrequency;

		private static AudioBuffer[] AudioBuffers = new AudioBuffer[2];
		private static AutoResetEvent[] BufferEvents = new AutoResetEvent[2];
		private static byte[][] SampleBuffers = new byte[2][];
		private static int CurrentBuffer = 0;
		private static int CurrentSample = 0;
		private static int LastSample = 0;
		private static int FilteredSample = 0;
		private static int FilterValue = 0;
		private static long LastCycle = 0;
		private static SourceVoice SourceVoice;
		
		private static bool _Filtered = false;
		public static bool Filtered
		{
			get { return _Filtered; }
			set
			{
				_Filtered = value;
				FilterValue = value ? 220 : 0;
			}
		}

		public volatile static bool Playing = false;


		static AudioPlayer()
		{
			SampleBuffers[0] = new byte[AudioFrequency / AudioSlices];			
			var dataStream0 = DataStream.Create(SampleBuffers[0], true, true);
			AudioBuffers[0] = new AudioBuffer
			{
				Stream = dataStream0,
				AudioBytes = (int)dataStream0.Length,
				Flags = BufferFlags.None,
				Context = new IntPtr(0)
			};
			BufferEvents[0] = new AutoResetEvent(false);

			SampleBuffers[1] = new byte[AudioFrequency / AudioSlices];
			var dataStream1 = DataStream.Create(SampleBuffers[1], true, true);
			AudioBuffers[1] = new AudioBuffer
			{
				Stream = dataStream1,
				AudioBytes = (int)dataStream1.Length,
				Flags = BufferFlags.None,
				Context = new IntPtr(1)
			};
			BufferEvents[1] = new AutoResetEvent(false);

			var waveFormat = new WaveFormat(AudioFrequency, 8, 1);
			XAudio2 xaudio = new XAudio2();
			MasteringVoice masteringVoice = new MasteringVoice(xaudio);
			SourceVoice = new SourceVoice(xaudio, waveFormat, true);
			SourceVoice.BufferStart += SourceVoice_BufferStart;
		}

		public static void Start()
		{			
			CurrentBuffer = 0;

			// queue 2 empty buffers, we'll start filling the second one
			SourceVoice.Start();
			QueueBuffer();
			Playing = true;

			var reg = AtmelContext.RAM[AtmelIO.OCR2B] as ObservableRegister;
			reg.OnRegisterChanged += reg_OnRegisterChanged;
		}

		private static void QueueBuffer()
		{
			// fill remaining buffer with the current sample
			var buffer = SampleBuffers[CurrentBuffer];
			while (CurrentSample < SamplesPerSlice)
			{
				FilteredSample = (FilteredSample * FilterValue + LastSample * (256 - FilterValue)) / 256;
				buffer[CurrentSample++] = (byte)FilteredSample;
			}

			// queue the buffer and wait for it to start playing			
			BufferEvents[CurrentBuffer].Reset();
			SourceVoice.SubmitSourceBuffer(AudioBuffers[CurrentBuffer], null);
			BufferEvents[CurrentBuffer].WaitOne();
			CurrentBuffer = 1 - CurrentBuffer;
			CurrentSample = 0;
			LastCycle = AtmelContext.Clock;
		}

		public static void Stop()
		{
			Playing = false;
			var reg = AtmelContext.RAM[AtmelIO.OCR2B] as ObservableRegister;
			reg.OnRegisterChanged -= reg_OnRegisterChanged;
			SourceVoice.Stop();
		}

		static void reg_OnRegisterChanged(int oldVal, int newVal)
		{
			if (!Playing)
				return;

			// if the buffer is full then we're going to have to skip samples
			if (CurrentSample >= SamplesPerSlice)
				return;

			// which sample are we on?
			int sampleNum = Math.Min(SamplesPerSlice, (int)((AtmelContext.Clock - LastCycle) / CyclesPerSample));

			// fill the buffer up to this point with the old value
			var buffer = SampleBuffers[CurrentBuffer];
			while (CurrentSample < sampleNum)
			{
				FilteredSample = (FilteredSample * FilterValue + LastSample * (256 - FilterValue)) / 256;
				buffer[CurrentSample++] = (byte)FilteredSample;
			}
			CurrentSample = sampleNum;

			// make note of our new value
			if (LastSample != newVal)
				LastSample = (byte)newVal;
		}

		public static void NextBuffer()
		{
			QueueBuffer();
		}

		private static void SourceVoice_BufferStart(IntPtr obj)
		{
			BufferEvents[obj.ToInt32()].Set();
		}

	}
}
