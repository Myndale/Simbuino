using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Simbuino.UI.Helpers
{
	public static class FileHelpers
	{
		public static IEnumerable<string> ReadAllLines(this Stream stream)
		{
			using (StreamReader reader = new StreamReader(stream))
			{
				string line;
				while ((line = reader.ReadLine()) != null)
				{
					yield return line;
				}
			}
		}
	}
}
