$(function () {

	// basic SD card emulation class
	SdDevice =
	{
		Init: function ()
		{
			var self = this;

			this.CS_PORT = AtmelContext.B; // PB2
			this.CS_BIT = 2;
			this.Index = 0;
			this.Received = [];
			for (var i = 0; i < 10; i++)
				this.Received[i] = 0;
			this.Status = 0;
			this.Buffer = [];
			for (var i = 0; i < 550; i++)
				this.Buffer[i] = 0;
			this.BytesToSend = 0;
			this.SendIndex = 0;
			this.AppCommand = false;
			SPI.OnReceivedByte.push(function(data) {self.spi_OnReceivedByte(data);});
		},

		Reset: function()
		{
			this.Status = 0;
			this.Index = 0;
			this.BytesToSend = 0;
			this.SendIndex = 0;
		},

		spi_OnReceivedByte: function(data)
		{
			// make sure the SD card is currently enabled
			if (this.CS_PORT.WriteRegister.get().get_bit(this.CS_BIT) != 0)
				return;

			if (this.BytesToSend > 0)
			{
				this.BytesToSend--;
				SPI.ReceiveByte(this.Buffer[this.SendIndex++]);
				return;
			}

			this.Received[this.Index++] = data;
			if (!this.AppCommand)
			{
				switch (this.Received[0])
				{
					// CMD0 - RESET
					case 0x40:
						if (this.Index == 6)
						{
							this.Status = 0xff;
							this.Index = 0;
							this.SendIndex = 0;
							this.BytesToSend = 0;
							this.Buffer[this.BytesToSend++] = 0x01;
						}
						break;

						// CMD8 - SEND_IF_COND
					case 0x40 + 8:
						if (this.Index == 6)
						{
							this.Status = 0xff;
							this.Index = 0;
							this.SendIndex = 0;
							this.BytesToSend = 0;
							this.Buffer[this.BytesToSend++] = 0x04;	// invalid
							//this.Buffer[this.BytesToSend++] = 0x00;	// R1
							//this.Buffer[this.BytesToSend++] = 0xaa;
							//this.Buffer[this.BytesToSend++] = 0xaa;
							//this.Buffer[this.BytesToSend++] = 0xaa;
							//this.Buffer[this.BytesToSend++] = 0xaa;
						}
						break;

						// CMD16 - SET_BLOCKLEN
					case 0x40 + 16:
						if (this.Index == 6)
						{
							this.Status = 0x01;
							this.Index = 0;
							this.SendIndex = 0;
							this.BytesToSend = 0;
							this.Buffer[this.BytesToSend++] = 0x00;
						}
						break;

						// CMD17 - READ_SINGLE_BLOCK
					case 0x40 + 17:
						if (this.Index == 6)
						{
							this.Status = 0x01;
							this.Index = 0;
							this.SendIndex = 0;
							this.BytesToSend = 0;
							this.Buffer[this.BytesToSend++] = 0x00;
							this.Buffer[this.BytesToSend++] = 0xFE;
							try
							{
								var offset = (this.Received[1] << 24) + (this.Received[2] << 16) + (this.Received[3] << 8) + this.Received[4];
								for (var i = 0; i < 512; i++)
									this.Buffer[this.BytesToSend + i] = this.ReadBuffer[offset + i];
							}
							catch (e)
							{
								for (var i = 0; i < 512; i++)
									this.Buffer[this.BytesToSend + i] = 0;
							}
							this.BytesToSend += 512;
							this.Buffer[this.BytesToSend++] = 0x00;
							this.Buffer[this.BytesToSend++] = 0x00;
						}
						break;

						// CMD23 - Number of blocks
					case 0x40 + 23:
						if (this.Index == 6)
						{
							this.Status = 0x01;
							this.Index = 0;
						}
						break;

						// CMD55 - ACMD
					case 0x40 + 55:
						if (this.Index == 6)
						{
							this.Status = 0xff;
							this.Index = 0;
							this.SendIndex = 0;
							this.BytesToSend = 0;
							this.Buffer[this.BytesToSend++] = 0;
							this.AppCommand = true;
						}
						break;

						// CMD58 - READ_OCR
					case 0x40 + 58:
						if (this.Index == 6)
						{
							this.Status = 0xff;
							this.Index = 0;
							this.SendIndex = 0;
							this.BytesToSend = 0;
							this.Buffer[this.BytesToSend++] = 0;
						}
						break;

					case 0xff:
						this.Index = 0;
						this.Status = 0xff;
						break;

					default:
						if (this.Index == 6)
						{
							// unknown command
							this.Status = 0x04;
							this.Index = 0;
						}
						break;
				}
			}
			else
			{
				switch (this.Received[0])
				{
					// SD_SEND_OP_COND
					case 0x40 + 41:
						if (this.Index == 6)
						{
							this.Status = 0xff;
							this.Index = 0;
							this.SendIndex = 0;
							this.BytesToSend = 0;
							this.Buffer[this.BytesToSend++] = 0x00;
							this.AppCommand = false;
						}
						break;

					case 0xff:
						this.Index = 0;
						this.Status = 0xff;
						break;

					default:
						if (this.Index == 6)
						{
							// unknown acmd
							this.Status = 0x04;
							this.Index = 0;
							this.AppCommand = false;
						}
						break;
				}
			}

			SPI.ReceiveByte(this.Status);
		}
	}

});
