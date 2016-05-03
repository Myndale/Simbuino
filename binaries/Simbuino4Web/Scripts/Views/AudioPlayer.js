$(function () {
	AudioPlayer = Class.create({

		ctor: function () {
			var self = this;
			this.BufferSize = 4096;
			this.StartCycle = AtmelContext.Clock;
			this.CurrentSample = 0;
			this.StartCycle = 10;
			this.LastSample = 0;
			var AudioContext = window.AudioContext || window.webkitAudioContext;
			if (AudioContext)
			{
				this.Context = new AudioContext();
				this.SampleRate = this.Context.sampleRate;				
				
				this.CyclesPerSample = Math.floor(AtmelProcessor.ClockSpeed / this.SampleRate);
				this.MixBuffer = [];
				for (var i = 0; i < this.BufferSize; i++)
					this.MixBuffer[i] = 0;
				this.Processor = this.Context.createScriptProcessor(this.BufferSize, 1, 1);
				this.Processor.onaudioprocess = function (e) {

					var src = self.MixBuffer;
					var dst = e.outputBuffer.getChannelData(0);
					var count = Math.min(self.CurrentSample, self.BufferSize);

					// copy over samples we've done for this buffer
					for (var i = 0; i < count; i++)
						dst[i] = src[i];

					// fill any remaining samples with the last sample value
					for (var i = count; i < self.BufferSize; i++)
						dst[i] = self.LastSample;

					self.StartCycle = AtmelContext.Clock;
					self.CurrentSample = 0;
				};

				// intercept writes to the OCR2B sound register and start playing
				AtmelContext.RAM[AtmelIO.OCR2B].OnRegisterChanged.push(function (oldVal, newVal) { self.reg_OnRegisterChanged(oldVal, newVal) });
				this.Processor.connect(this.Context.destination);
				this.Active = true;
			}
		},

		reg_OnRegisterChanged: function(oldVal, newVal)
		{
			newVal = (newVal - 128) / 512;

			// which sample are we on?
			var sampleNum = Math.floor((AtmelContext.Clock - this.StartCycle) / this.CyclesPerSample);

			// make sure we don't get too far ahead				
			sampleNum = Math.min(sampleNum, this.BufferSize-1);

			// fill the buffer up to this point with the old value
			var buffer = this.MixBuffer;
			while (this.CurrentSample < sampleNum)
				buffer[this.CurrentSample++] = newVal;

			// make note of our new value
			this.LastSample = newVal;
		}

	});

});
