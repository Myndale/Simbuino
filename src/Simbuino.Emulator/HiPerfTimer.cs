using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Simbuino.Emulator
{
	// high performance timer used for profiling, code courtesy Daniel Strigl http://www.codeproject.com/Articles/2635/High-Performance-Timer-in-C
	public class HiPerfTimer
	{
		[DllImport("Kernel32.dll")]
		private static extern bool QueryPerformanceCounter(
			out long lpPerformanceCount);

		[DllImport("Kernel32.dll")]
		private static extern bool QueryPerformanceFrequency(
			out long lpFrequency);

		private long startTime, stopTime;
		private long freq;
		public double CurrentTime = 0;

		// Constructor
		public HiPerfTimer()
		{
			startTime = 0;
			stopTime = 0;

			if (QueryPerformanceFrequency(out freq) == false)
			{
				// high-performance counter not supported
				throw new Win32Exception();
			}
		}

		// Start the timer
		public void Start()
		{
			// lets do the waiting threads there work
			//Thread.Sleep(0);

			QueryPerformanceCounter(out startTime);
			stopTime = startTime;
			this.CurrentTime = stopTime / (double)freq;
		}

		// Stop the timer
		public void Stop()
		{
			QueryPerformanceCounter(out stopTime);
			this.CurrentTime = stopTime / (double)freq;
		}

		public void Restart()
		{
			this.startTime = this.stopTime;
		}

		// Returns the duration of the timer (in seconds)
		public double Duration
		{
			get
			{
				return (double)(stopTime - startTime) / (double)freq;
			}
		}
	}
}
