using System;
using System.Collections.Generic;
using System.Linq;
using AppKit;
using Foundation;

namespace ScreenShare
{
	public static class Helper
	{
		/// <summary>
		/// Sends a mac notification
		/// </summary>
		/// <returns>A notification</returns>
		/// <param name="title">Title.</param>
		/// <param name="message">Message.</param>
		public static void NotifyMe (string title, string message)
		{
			// First we create our notification and customize as needed
			NSUserNotification not = null;

			not = new NSUserNotification ();

			not.Title = title;
			not.InformativeText = message;
			not.DeliveryDate = (NSDate)DateTime.Now;
			not.SoundName = NSUserNotification.NSUserNotificationDefaultSoundName;

			// We get the Default notification Center
			NSUserNotificationCenter center = NSUserNotificationCenter.DefaultUserNotificationCenter;

			// If we return true here, Notification will show up even if your app is TopMost.
			center.ShouldPresentNotification = (c, n) => { return true; };

			center.ScheduleNotification (not);
		}

		/// <summary>
		/// Sets the clipboard.
		/// </summary>
		/// <returns>The clipboard.</returns>
		/// <param name="str">String.</param>
		public static void SetClipboard (string str)
		{
			NSString stri = (NSString)str;
			NSPasteboard pasteBoard = NSPasteboard.GeneralPasteboard;
			pasteBoard.ClearContents ();
			pasteBoard.WriteObjects (new INSPasteboardWriting [] { stri });
		}

		/// <summary>
		/// Gets the clipboard.
		/// </summary>
		/// <returns>The clipboard.</returns>
		public static string GetClipboard ()
		{
			NSPasteboard pasteBoard = NSPasteboard.GeneralPasteboard;
			return pasteBoard.PasteboardItems [0].GetStringForType ("public.utf8-plain-text");
		}

		/// <summary>
		/// Shows the input dialog.
		/// </summary>
		/// <returns>The input</returns>
		/// <param name="name">Name.</param>
		/// <param name="providerName">Provider name.</param>
		public static string ShowInputDialog (string name, string providerName, bool multiline = false)
		{
			NSAlert alert = new NSAlert ();
			alert.MessageText = name.UppercaseFirst() + " is required by " + providerName;
			alert.AddButton ("Ok");
			alert.AddButton ("Cancel");

			CoreGraphics.CGRect textFieldRect = new CoreGraphics.CGRect (0, 0, 300, 24);
			if (multiline) {
				textFieldRect = new CoreGraphics.CGRect (0, 0, 300, 24*3);
			}

			SSTextField input = new SSTextField (textFieldRect);

			input.UsesSingleLineMode = !multiline;
			var clip = GetClipboard ();
			input.StringValue = (clip != null) ? clip : "";
			alert.AccessoryView = input;
			nint button = alert.RunModal ();

			if (button == (int)NSAlertButtonReturn.First) {
				return input.StringValue;
			}
			return "";
		}

		/// <summary>
		/// Uppercases the first letter in string.
		/// </summary>
		/// <returns>String with first uppercase</returns>
		/// <param name="s">string</param>
		public static string UppercaseFirst (this string s)
		{
			// Check for empty string.
			if (string.IsNullOrEmpty (s)) {
				return string.Empty;
			}
			// Return char and concat substring.
			return char.ToUpper (s [0]) + s.Substring (1);
		}

		/// <summary>
		/// Basic post request.
		/// </summary>
		/// <returns>The post response.</returns>
		/// <param name="url">URL.</param>
		/// <param name="args">Arguments.</param>
		/// <param name="headers">Headers.</param>
		public static string BasicPostRequest (string url, Dictionary<string, string> args, Dictionary<string, string> headers = null)
		{
			NSString urlString = (NSString)url;

			NSMutableUrlRequest request = new NSMutableUrlRequest ();
			request.Url = new NSUrl (urlString);
			request.HttpMethod = "POST";
			string boundary = "---------------------------14737809831466499882746641449";
			NSString contentType = new NSString (string.Format ("multipart/form-data; boundary={0}", boundary));

			var keys = new List<object> { "Content-Type" };
			var objects = new List<object> { contentType };
			if (headers != null) {
				foreach (KeyValuePair<string, string> kvp in headers) {
					keys.Add (kvp.Key);
					objects.Add (kvp.Value);
				}
			}
			var dictionary = NSDictionary.FromObjectsAndKeys (objects.ToArray (), keys.ToArray ());
			request.Headers = dictionary;

			NSMutableData body = new NSMutableData ();
			foreach (KeyValuePair<string, string> kvp in args) {
				body.AppendData (NSData.FromString (new NSString (string.Format ("\r\n--{0}\r\n", boundary))));
				body.AppendData (NSData.FromString (new NSString (string.Format ("Content-Disposition: form-data; name=\"{0}\"\r\n\r\n", kvp.Key))));
				body.AppendData (NSData.FromString (new NSString (kvp.Value)));
				body.AppendData (NSData.FromString (new NSString (string.Format ("\r\n--{0}--\r\n", boundary))));
			}

			request.Body = body;

			NSUrlResponse resp;
			NSError err;
			NSData returnData = NSUrlConnection.SendSynchronousRequest (request, out resp, out err);
			return NSString.FromData (returnData, NSStringEncoding.UTF8).ToString ();
		}

		/// <summary>
		/// Basic get request.
		/// </summary>
		/// <returns>The get response</returns>
		/// <param name="url">URL.</param>
		/// <param name="args">Arguments.</param>
		/// <param name="headers">Headers.</param>
		public static string BasicGetRequest (string url, Dictionary<string, string> args, Dictionary<string, string> headers = null)
		{
			NSMutableUrlRequest request = new NSMutableUrlRequest ();
			request.HttpMethod = "GET";

			var keys = new List<object>();
			var objects = new List<object>();
			if (headers != null) {
				foreach (KeyValuePair<string, string> kvp in headers) {
					keys.Add (kvp.Key);
					objects.Add (kvp.Value);
				}
			}
			var dictionary = NSDictionary.FromObjectsAndKeys (objects.ToArray (), keys.ToArray ());
			request.Headers = dictionary;

			NSMutableData body = new NSMutableData ();
			if (args.Count > 0) {
				url = url + "?";
				int i = 0;
				foreach (KeyValuePair<string, string> kvp in args) {
					i++;
					url = url + kvp.Key + "=" + System.Net.WebUtility.HtmlEncode(kvp.Value);
					if (i < args.Count) { url = url + "&"; }
				}
			}
			request.Url = new NSUrl (url);

			request.Body = body;

			NSUrlResponse resp;
			NSError err;
			NSData returnData = NSUrlConnection.SendSynchronousRequest (request, out resp, out err);
			return NSString.FromData (returnData, NSStringEncoding.UTF8).ToString ();
		}

		/// <summary>
		/// Randoms the string.
		/// </summary>
		/// <returns>The string.</returns>
		/// <param name="length">Length.</param>
		public static string RandomString (int length)
		{
			const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
			var random = new Random ();
			return new string (Enumerable.Repeat (chars, length)
				.Select (s => s [random.Next (s.Length)]).ToArray ());
		}
	}
}

