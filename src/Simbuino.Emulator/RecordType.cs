using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Simbuino.Emulator
{
	// needed by the HexDecoder class
	public enum RecordType
	{
		Data = 0,
		EndOfFile = 1,
		ExtendedSegmentAddress = 2,
		StartSegmentAddress = 3,
		ExtendedLinearAddress = 4,
		StartLinearAddress = 5
	}
}
