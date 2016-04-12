using System;
using System.Collections.Generic;
using System.Collections;
using System.IO;
using System.Text.RegularExpressions;

using AppKit;
using Foundation;

namespace ScreenShare
{
	public partial class ViewController : NSViewController
	{

		private NSMenuItem fullScreenshot;
		private NSMenuItem regionScreenshot;
		private NSMenuItem windowScreenshot;
		private NSMenuItem toolsItem;
		private NSStatusItem statusItem;

		public ViewController (IntPtr handle) : base (handle)
		{
		}

		public override void ViewDidLoad ()
		{
			base.ViewDidLoad ();

			// Do any additional setup after loading the view.
		}

		/// <summary>
		/// Awakes from nib.
		/// </summary>
		public override void AwakeFromNib()
		{
			//Initialize status item
			AppDelegate mainDelegate = (AppDelegate)NSApplication.SharedApplication.Delegate;
			mainDelegate.statusItem = NSStatusBar.SystemStatusBar.CreateStatusItem(30);
			statusItem = mainDelegate.statusItem;

			BuildMenu ();
			ProcessProviders ();

			NSImage img = NSImage.ImageNamed ("clipboard");
			img.Template = true;
			statusItem.Image = img;
			statusItem.HighlightMode = true;
		}

		/// <summary>
		/// Builds the menu.
		/// </summary>
		/// <returns>The menu.</returns>
		private void BuildMenu ()
		{
			NSMenu ms = new NSMenu ();
			fullScreenshot = new NSMenuItem ("Full Screenshot");
			regionScreenshot = new NSMenuItem ("Region Screenshot");
			windowScreenshot = new NSMenuItem ("Window Screenshot");
			toolsItem = new NSMenuItem ("Tools");
			ms.AddItem (fullScreenshot);
			ms.AddItem (regionScreenshot);
			ms.AddItem (windowScreenshot);
			ms.AddItem (NSMenuItem.SeparatorItem);
			ms.AddItem (toolsItem);
			ms.AddItem (NSMenuItem.SeparatorItem);
			NSMenuItem quitButton = new NSMenuItem ("Quit");
			quitButton.Activated += (object sender, EventArgs e) => {
				NSApplication.SharedApplication.Terminate (this);
			};
			ms.AddItem (quitButton);
			statusItem.Menu = ms;
		}

		/// <summary>
		/// Processes the providers.
		/// </summary>
		private void ProcessProviders()
		{
			string[] files = Directory.GetFiles ("./Providers");
			for (int i = 0; i < files.Length; i++) {
				//Time to read them yo
				dynamic provider = null;
				try{
					provider = JSON.Json.JsonDecode(File.ReadAllText(files[i]));
				}catch(Exception ex){
					#if DEBUG
					Console.WriteLine (ex);
					#endif
					return;
				}
				if (!provider.ContainsKey ("RequestType") || !provider.ContainsKey ("Name") || !provider.ContainsKey ("RequestURL")) {
					continue;
				}
				if (provider ["RequestType"] == "POST") {
					if (provider.ContainsKey ("FileFormName")) {
						//Fullscreen button
						NSMenuItem provFull = new NSMenuItem (provider ["Name"]);
						provFull.Activated += (object sender, EventArgs e) => {
							ProviderUpload (provider);
						};
						//Region button
						NSMenuItem provRegion = new NSMenuItem (provider ["Name"]);
						provRegion.Activated += (object sender, EventArgs e) => {
							ProviderUpload (provider, true);
						};
						NSMenuItem provWindow = new NSMenuItem (provider ["Name"]);
						provWindow.Activated += (object sender, EventArgs e) => {
							ProviderUpload (provider, false, true);
						};

						//Add buttons
						if (!fullScreenshot.HasSubmenu) {
							fullScreenshot.Submenu = new NSMenu ();
						}
						fullScreenshot.Submenu.AddItem (provFull);
						if (!regionScreenshot.HasSubmenu) {
							regionScreenshot.Submenu = new NSMenu ();
						}
						regionScreenshot.Submenu.AddItem (provRegion);
						if (!windowScreenshot.HasSubmenu) {
							windowScreenshot.Submenu = new NSMenu ();
						}
						windowScreenshot.Submenu.AddItem (provWindow);
					} else {
						NSMenuItem tool = new NSMenuItem (provider ["Name"]);
						tool.Activated += (object sender, EventArgs e) => {
							ProviderPost (provider);
						};
						if (!toolsItem.HasSubmenu) { toolsItem.Submenu = new NSMenu (); }
						toolsItem.Submenu.AddItem (tool);
					}
				} else if (provider ["RequestType"] == "GET") {
					NSMenuItem tool = new NSMenuItem (provider["Name"]);
					tool.Activated += (object sender, EventArgs e) => {
						ProviderGet (provider);
					};
					if (!toolsItem.HasSubmenu) { toolsItem.Submenu = new NSMenu (); }
					toolsItem.Submenu.AddItem (tool);
				}
			}
		}

		/// <summary>
		/// Upload function for providers
		/// </summary>
		/// <param name="provider">Provider</param>
		/// <param name="region">Get Region</param>
		/// <param name="window">Get Window</param>
		private void ProviderUpload (dynamic provider, bool region = false, bool window = false)
		{
			byte [] fullScreen = null;
			bool onlyData = false;
			try {
				fullScreen = ScreenshotCapture.TakeScreenshot (region, window);
			} catch(Exception ex) {
#if DEBUG
				Console.WriteLine (ex);
#endif
				Helper.NotifyMe ("Error", "There was an error taking a screenshot");
				return;
			}

			Dictionary<string, string> args = ProcessArgs (provider);
			Dictionary<string, string> headers = ProcessHeaders (provider);

			if (provider.ContainsKey ("DataOnly")) {
				onlyData = (bool)provider ["DataOnly"];
			}
			string data = UploadScreenshot (fullScreen, provider ["RequestURL"], provider ["FileFormName"], args, headers, onlyData);
			if (string.IsNullOrEmpty (data)) {
				return;
			}
			#if DEBUG
			Console.WriteLine ("Data: {0}", data);
			#endif

			ProcessFinalUrl (provider, data);
		}

		/// <summary>
		/// Get request for provider
		/// </summary>
		/// <returns>URL from request</returns>
		/// <param name="provider">Provider.</param>
		private void ProviderGet (dynamic provider)
		{
			Dictionary<string, string> args = ProcessArgs (provider);
			Dictionary<string, string> headers = ProcessHeaders (provider);

			string response = Helper.BasicGetRequest (provider ["RequestURL"], args);

			ProcessFinalUrl (provider, response);
		}

		/// <summary>
		/// Post using the provider
		/// </summary>
		/// <returns>The provider url</returns>
		/// <param name="provider">Provider.</param>
		private void ProviderPost (dynamic provider)
		{
			Dictionary<string, string> args = ProcessArgs (provider);
			Dictionary<string, string> headers = ProcessHeaders (provider);

			var response = Helper.BasicPostRequest (provider["RequestURL"], args, headers);

			ProcessFinalUrl (provider, response);
		}

		public override NSObject RepresentedObject
		{
			get {
				return base.RepresentedObject;
			}
			set {
				base.RepresentedObject = value;
				// Update the view, if already loaded.
			}
		}

		/// <summary>
		/// Uploads the screenshot.
		/// </summary>
		/// <returns>The screenshot.</returns>
		/// <param name="fullScreen">Full screen.</param>
		/// <param name="url">URL.</param>
		/// <param name="fileFormName">File form name.</param>
		/// <param name="args">Arguments.</param>
		private string UploadScreenshot(byte[] fullScreen, string url, string fileFormName, Dictionary<string, string> args, Dictionary<string, string> headers, bool onlyData = false)
		{
			string data = "";
			try{
				data = ScreenshotCapture.Upload(fullScreen, url, fileFormName, args, headers, onlyData);
			}catch{
				Helper.NotifyMe("Error", "There was an error uploading your screenshot");
				return "";
			}
			return data;
		}


		/// <summary>
		/// Processes the final URL.
		/// </summary>
		/// <param name="provider">Provider.</param>
		/// <param name="response">Response.</param>
		private void ProcessFinalUrl (dynamic provider, string response)
		{
			string finalUrl = response;
			Match m = null;
			if (provider.ContainsKey ("RegexList")) {
				m = Regex.Match (response, provider ["RegexList"] [0]);
			}
			if (provider.ContainsKey ("URL")) {
				MatchCollection mc = Regex.Matches (provider ["URL"], "\\$([^\\$]+)\\$");
				finalUrl = provider ["URL"];
				foreach (Match ma in mc) {
					string [] matches = ma.Groups [1].Value.Split (new string [] { ":", "," }, StringSplitOptions.RemoveEmptyEntries);
					int n;
					if (int.TryParse (matches [0], out n)) {
						finalUrl = finalUrl.Replace (ma.Groups [0].Value, m.Groups [n].Value);
					} else if (matches [0] == "json") {
						dynamic json = JSON.Json.JsonDecode (response);
						for (int i = 1; i < matches.Length; i++) {
							if (i == matches.Length - 1) {
								finalUrl = finalUrl.Replace (ma.Groups [0].Value, json [matches [i]]);
								continue;
							}
							json = json [matches [i]];
						}
					}
				}
			}

			Helper.SetClipboard (finalUrl);
			UploadLog.WriteToLog (finalUrl);

			Helper.NotifyMe ("Response from " + provider ["Name"], finalUrl);
		}

		/// <summary>
		/// Processes the arguments.
		/// </summary>
		/// <returns>The arguments.</returns>
		/// <param name="provider">Provider.</param>
		private Dictionary<string, string> ProcessArgs (dynamic provider)
		{
			bool multiline = false;
			if (provider.ContainsKey ("Multiline")) {
				multiline = (bool)provider ["Multiline"];
			}
			Dictionary<string, string> args = new Dictionary<string, string> ();
			if (provider.ContainsKey ("Arguments")) {
				foreach (DictionaryEntry kvp in provider ["Arguments"]) {
					string value = (string)kvp.Value;
					if (value.Contains ("$input$")) {
						value = value.Replace ("$input$", Helper.ShowInputDialog ((string)kvp.Key, provider ["Name"], multiline));
					} else if (value.Contains ("$random$")) {
						value = value.Replace ("$random$", Helper.RandomString (8));
					}
					args.Add ((string)kvp.Key, value);
				}
			}
			return args;
		}

		/// <summary>
		/// Processes the headers.
		/// </summary>
		/// <returns>The headers.</returns>
		/// <param name="provider">Provider.</param>
		private Dictionary<string, string> ProcessHeaders (dynamic provider)
		{
			Dictionary<string, string> headers = new Dictionary<string, string> ();
			if (provider.ContainsKey ("Headers")) {
				foreach (DictionaryEntry kvp in provider ["Headers"]) {
					string value = (string)kvp.Value;
					if (value.Contains ("$input$")) {
						value = value.Replace ("$input$", Helper.ShowInputDialog ((string)kvp.Key, provider ["Name"]));
					} else if (value.Contains ("$random$")) {
						value = value.Replace ("$random$", Helper.RandomString (8));
					}
					headers.Add ((string)kvp.Key, value);
				}
			}
			return headers;
		}

	}
}
