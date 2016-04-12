// WARNING
//
// This file has been generated automatically by Xamarin Studio Community to store outlets and
// actions made in the UI designer. If it is removed, they will be lost.
// Manual changes to this file may not be handled correctly.
//
using Foundation;
using System.CodeDom.Compiler;

namespace ScreenShare
{
	[Register ("ViewController")]
	partial class ViewController
	{
		[Outlet]
		AppKit.NSMenu statusMenu { get; set; }

		[Action ("DragScreenshot:")]
		partial void DragScreenshot (Foundation.NSObject sender);

		[Action ("HelloWorld:")]
		partial void HelloWorld (Foundation.NSObject sender);

		[Action ("Screenshot:")]
		partial void Screenshot (Foundation.NSObject sender);

		[Action ("WindowScreenshot:")]
		partial void WindowScreenshot (Foundation.NSObject sender);
		
		void ReleaseDesignerOutlets ()
		{
			if (statusMenu != null) {
				statusMenu.Dispose ();
				statusMenu = null;
			}
		}
	}
}
