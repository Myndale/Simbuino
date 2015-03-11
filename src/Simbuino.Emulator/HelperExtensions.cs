using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Simbuino.Emulator
{
	public static class HelperExtensions
	{
		public static byte[] ToHexBytes(this string str)
		{
			if (str.Length % 2 != 0)
				return null;
			return Enumerable.Range(0, str.Length)
					 .Where(x => x % 2 == 0)
					 .Select(x => Convert.ToByte(str.Substring(x, 2), 16))
					 .ToArray();
		}

		public static byte Checksum(this IEnumerable<byte> bytes)
		{
			return (byte)(0x100 - (bytes.Select(x => (int)x).Sum() & 0xff));
		}

		public static string ToHexString(this byte val)
		{
			return String.Format("{0:X2}", val);
		}

		public static string ToHexString(this byte[] vals)
		{
			return String.Join("", vals.Select(val => String.Format("{0:X2}", val)));
		}
	}
}
