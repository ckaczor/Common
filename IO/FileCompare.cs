using System;
using System.IO;

namespace Common.IO
{
	public class FileCompare
	{
		#region Comparison

		public bool Compare(string fileName1, string fileName2)
		{
			FileStream fileStream1 = null;
			FileStream fileStream2 = null;
			bool compareStatus = true;

		    try
			{
				// Create file streams for each file
				fileStream1 = new FileStream(fileName1, FileMode.Open);
				fileStream2 = new FileStream(fileName2, FileMode.Open);

				// If the files aren't the same length then don't bother with more
				if (fileStream1.Length != fileStream2.Length)
					throw new ApplicationException();

				// Start comparing the bytes of each file
			    int fileByte1;
			    do
				{
					// Read a byte from the first file
					fileByte1 = fileStream1.ReadByte();

					// Read a byte from the second file
					int fileByte2 = fileStream2.ReadByte();

					// If the bytes don't match then stop
					if (fileByte1 != fileByte2)
						throw new ApplicationException();
				}
				while (fileByte1 != -1);
			}
			catch
			{
				// Compare failed
				compareStatus = false;
			}
			finally
			{
				// Close both of the file streams
				if (fileStream1 != null)
					fileStream1.Close();

				if (fileStream2 != null)
					fileStream2.Close();
			}

			return compareStatus;
		}

		#endregion
	}
}
