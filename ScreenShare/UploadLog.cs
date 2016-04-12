using System;
using System.IO;

namespace ScreenShare
{
	public static class UploadLog
	{
		private static string filename = "upload.txt";

		/// <summary>
		/// Writes to log file.
		/// </summary>
		/// <param name="link">Link.</param>
		public static void WriteToLog (string link) {
			using (StreamWriter sw = new StreamWriter (filename, true)) {
				sw.WriteLine ("[{0} {1}] {2}", DateTime.Now.ToLongDateString (), DateTime.Now.ToLongTimeString (), link);
			}
		}

		/// <summary>
		/// Writes to log file.
		/// </summary>
		/// <param name="link">Link.</param>
		/// <param name="deleteLink">Delete link.</param>
		public static void WriteToLog (string link, string deleteLink)
		{
			using (StreamWriter sw = new StreamWriter (filename, true)) {
				sw.WriteLine ("[{0} {1}] {2} delete: {3}", DateTime.Now.ToLongDateString(), DateTime.Now.ToLongTimeString(), link, deleteLink);
			}
		}

	}
}

