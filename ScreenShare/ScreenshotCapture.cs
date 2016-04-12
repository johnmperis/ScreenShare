using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

using Foundation;

namespace ScreenShare
{
	/// <summary>
	///     ScreenshotCapture
	/// </summary>
	public static class ScreenshotCapture
	{
		#region Public static methods

		/// <summary>
		/// Takes the screenshot.
		/// </summary>
		/// <returns>The screenshot.</returns>
		/// <param name="region">Region.</param>
		/// <param name="windowCapture">Window capture.</param>
		public static byte[] TakeScreenshot(bool region = false, bool windowCapture = false)
		{
			var data = StartScreenCapture(string.Format("-T0 -tpng {0} {1} -x", region ? "-i" : "", windowCapture ? "-W" : "-S"));
			return data;
		}

		#endregion

		#region  Private static methods
		/// <summary>
		/// Starts the screen capture.
		/// </summary>
		/// <returns>The screen capture.</returns>
		/// <param name="parameters">Parameters.</param>
		private static byte[] StartScreenCapture(string parameters)
		{
			string appName = "screencapture";
			var imageFileName = Path.Combine(Path.GetTempPath(), string.Format("screenshot_{0}.jpg", Guid.NewGuid()));

			var process = Process.Start(appName, string.Format("{0} {1}", parameters, imageFileName));
			if (process == null)
			{
				throw new InvalidOperationException(string.Format("Executable of '{0}' was not found", appName));
			}
			process.WaitForExit();

			if (!File.Exists(imageFileName))
			{
				throw new InvalidOperationException(string.Format("Failed to capture screenshot using {0}", appName));
			}

			try
			{
				return File.ReadAllBytes (imageFileName);
			}
			finally
			{
				File.Delete(imageFileName);
			}
		}
		#endregion

		/// <summary>
		/// Upload the specified paramFileBytes, url, fileFormName and args.
		/// </summary>
		/// <returns>Response string</returns>
		/// <param name="paramFileBytes">Parameter file bytes.</param>
		/// <param name="url">URL.</param>
		/// <param name="fileFormName">File form name.</param>
		/// <param name="args">Arguments.</param>
		public static string Upload(byte [] paramFileBytes, string url, string fileFormName, Dictionary<string, string> args = null, Dictionary<string, string> headers = null, bool onlyData = false)
		{
			return UploadNative (url, paramFileBytes, fileFormName, args, headers, onlyData);
		}

		/// <summary>
		/// Upload function using the native functionality of Mac
		/// </summary>
		/// <returns>Response string</returns>
		/// <param name="url">URL.</param>
		/// <param name="paramFileBytes">Parameter file bytes.</param>
		/// <param name="fileFormName">File form name.</param>
		/// <param name="args">Arguments.</param>
		private static string UploadNative (string url, byte [] paramFileBytes, string fileFormName, Dictionary<string, string> args = null, Dictionary<string, string> headers = null, bool onlyData = false)
		{
			NSData actualFile = NSData.FromArray (paramFileBytes);
			NSString urlString = (NSString)url;
			//we're gonna send byte data

			NSMutableUrlRequest request = new NSMutableUrlRequest ();
			request.Url = new NSUrl(urlString);
			request.HttpMethod = "POST";
			string boundary = "---------------------------14737809831466499882746641449";
			NSString contentType = new NSString (string.Format("multipart/form-data; boundary={0}", boundary));

			var keys = new List<object> { "Content-Type" };
			var objects = new List<object> { contentType };
			foreach (KeyValuePair<string, string> kvp in headers) {
				if (!keys.Contains (kvp.Key)) {
					keys.Add (kvp.Key);
					objects.Add (kvp.Value);
				} else {
					for (int i = 0; i < keys.Count; i++) {
						if ((string)keys [i] == kvp.Key) {
							objects [i] = kvp.Value;
						}
					}
				}
			}
			var dictionary = NSDictionary.FromObjectsAndKeys (objects.ToArray (), keys.ToArray ());
			request.Headers = dictionary;

			NSMutableData body = new NSMutableData ();
			if (!onlyData) {
				body.AppendData (NSData.FromString (new NSString (string.Format ("\r\n--{0}\r\n", boundary))));
				body.AppendData (NSData.FromString (new NSString (string.Format ("Content-Disposition: form-data; name=\"{0}\"; filename=\"{0}.png\"\r\n", fileFormName))));
				body.AppendData (NSData.FromString (new NSString ("Content-Type: application/octet-stream\r\n\r\n")));
			}
			body.AppendData (actualFile);
			if (!onlyData) {
				body.AppendData (NSData.FromString (new NSString (string.Format ("\r\n--{0}--\r\n", boundary))));

				foreach (KeyValuePair<string, string> kvp in args) {
					body.AppendData (NSData.FromString (new NSString (string.Format ("\r\n--{0}\r\n", boundary))));
					body.AppendData (NSData.FromString (new NSString (string.Format ("Content-Disposition: form-data; name=\"{0}\"\r\n\r\n", kvp.Key))));
					body.AppendData (NSData.FromString (new NSString (kvp.Value)));
					body.AppendData (NSData.FromString (new NSString (string.Format ("\r\n--{0}--\r\n", boundary))));
				}
			}
			//Attach body
			request.Body = body;

			NSUrlResponse resp;
			NSError err;
			NSData returnData = NSUrlConnection.SendSynchronousRequest (request, out resp, out err);
			return NSString.FromData (returnData, NSStringEncoding.UTF8).ToString();
		}


	}
}