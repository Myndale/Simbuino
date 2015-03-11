using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Simbuino.Emulator
{
	// helper class for decoding Intel .HEX classes
	public class HexDecoder
	{
		public bool Decode(IEnumerable<string> code, out int minAddr, out int maxAddr)
		{
			minAddr = -1;
			maxAddr = -1;
			if (code == null)
				throw new ArgumentException("Null code array");
			foreach (var line in code)
				if (Decode(line, ref minAddr, ref maxAddr))
					return true;
			return false;
		}

		public bool Decode(string code, ref int minAddr, ref int maxAddr)
		{
			// a null or empty string is an error
			if (String.IsNullOrEmpty(code))
				throw new ArgumentException("Invalid code string");

			// first character must be a colon
			if (code[0] != ':')
				throw new ArgumentException("First character must be a colon");

			// parse the line
			var bytes = code.Substring(1).ToHexBytes();
			if (bytes.Length < 5)
				throw new ArgumentException("Line is too short");
			byte byteCount = bytes[0];
			ushort address = (ushort)(bytes[1] * 256 + bytes[2]);
			RecordType recordType = (RecordType)Enum.ToObject(typeof(RecordType), bytes[3]);
			byte crc = bytes.Last();
			var data = bytes.Skip(4).Take(bytes.Length - 5).ToArray();

			// make sure the count matches
			if (byteCount != data.Count())
				throw new ArgumentException("Byte count doesn't match data length");

			// make sure the checksum checks out
			if (crc != bytes.Take(bytes.Length - 1).Checksum())
				throw new ArgumentException("Bad crc");

			// set the data
			switch (recordType)
			{
				case RecordType.Data:
					for (int i = 0; i < data.Count(); i++)
					{
						int addr = address + i;
						if ((addr & 1) == 0)
							AtmelContext.Flash[addr >> 1] = (AtmelContext.Flash[addr >> 1] & 0xff00) + data[i];
						else
							AtmelContext.Flash[addr >> 1] = (AtmelContext.Flash[addr >> 1] & 0x00ff) + ((int)data[i] << 8);
						minAddr = (minAddr == -1) ? addr : Math.Min(minAddr, addr);
						maxAddr = (maxAddr == -1) ? addr : Math.Max(maxAddr, addr);
					}
					break;

				case RecordType.EndOfFile:
					return true;

				case RecordType.ExtendedSegmentAddress:
					throw new NotSupportedException();

				case RecordType.StartSegmentAddress:
					throw new NotSupportedException();

				case RecordType.ExtendedLinearAddress:
					throw new NotSupportedException();

				case RecordType.StartLinearAddress:
					throw new NotSupportedException();

				default:
					throw new ArgumentException("Unrecognized record type");
			}

			// not end of file
			return false;
		}

		public string Packet(RecordType recordType, int address, IEnumerable<byte> data)
		{
			var bytes = new byte[] { (byte)(data.Count())}
				.Concat(new byte[] { (byte)(address >> 8), (byte)(address & 0xff) })
				.Concat(new byte[] { (byte)recordType })
				.Concat(data.ToArray())
				.ToArray();
			var crc = bytes.Checksum();
			var line = ":" + bytes.ToHexString() + crc.ToHexString();
			return line;
		}

	}
}
