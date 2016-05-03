$(function () {

	RecordType =
	{
		Data: 0,
		EndOfFile: 1,
		ExtendedSegmentAddress: 2,
		StartSegmentAddress: 3,
		ExtendedLinearAddress: 4,
		StartLinearAddress: 5
	};

	// courtesy http://stackoverflow.com/questions/14603205/how-to-convert-hex-string-into-a-bytes-array-and-a-bytes-array-in-the-hex-strin
	function parseHexString(str) {
		var result = [];
		var index = 0;
		while (index < str.length) {
			result.push(parseInt(str.substring(index, index+2), 16));
			index += 2;
		}
		return result;
	}
	
	HexDecoder =
	{
		Decode: function(code)
		{
			this.minAddr = this.maxAddr = -1;
			if (!code)
				return false;
			for (var i=0; i<code.length; i++)
				if (this.DecodeLine(code[i]))
					return (this.minAddr >= 0) && (this.maxAddr < 2 * AtmelContext.Flash.length) && (this.maxAddr > this.minAddr);
			return false;
		},

		DecodeLine: function(code)
		{
			// a null or empty string is an error
			if (!code)
				return true;

			// first character must be a colon
			if (code[0] != ':')
				return true;

			// parse the line
			code = code.substring(1);
			var bytes = this.ToHexBytes(code);
			if (bytes.length < 5)
				return true;
			var byteCount = bytes[0];
			var address = bytes[1] * 256 + bytes[2];
			var recordType = bytes[3];
			var crc = bytes[bytes.length-1];
			var data = bytes.slice(4, bytes.length - 1);

			// make sure the count matches
			if (byteCount != data.length)
				return true;

			// make sure the checksum checks out
			if (crc != this.Checksum(bytes.slice(0, bytes.length - 1)))
				return true;

			// set the data
			if (recordType == RecordType.Data)
			{
				for (var i = 0; i < data.length; i++)
				{
					var addr = address + i;
					if ((addr & 1) == 0)
						AtmelContext.Flash[addr >> 1] = (AtmelContext.Flash[addr >> 1] & 0xff00) + data[i];
					else
						AtmelContext.Flash[addr >> 1] = (AtmelContext.Flash[addr >> 1] & 0x00ff) + (data[i] << 8);
					this.minAddr = (this.minAddr == -1) ? addr : Math.min(this.minAddr, addr);
					this.maxAddr = (this.maxAddr == -1) ? addr : Math.max(this.maxAddr, addr);
				}
			}
			else if (recordType == RecordType.EndOfFile)
				return true;

			// not end of file
			return false;
		},

		ToHexBytes: function(str)
		{
			var result = parseHexString(str);
			return result;
		},

		Checksum: function(bytes)
		{
			var sum = 0;
			for (var i=0; i<bytes.length; i++)
				sum = sum + bytes[i];
			var result = (0x100 - (sum & 0xff)) & 0xff;
			return result;
		}
	}

});
