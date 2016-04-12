using AppKit;

namespace ScreenShare
{
	public class SSTextField : NSTextField
	{

		public SSTextField (CoreGraphics.CGRect rect) : base (rect)
		{

		}

		public override bool PerformKeyEquivalent (NSEvent theEvent)
		{
			if (theEvent.Type == NSEventType.KeyDown) {
				if (theEvent.ModifierFlags.HasFlag(NSEventModifierMask.CommandKeyMask)) {
					switch (theEvent.CharactersIgnoringModifiers) {
					case "x":
						if (NSApplication.SharedApplication.SendAction (new ObjCRuntime.Selector ("cut:"), null, this)) { return true; }
						break;
					case "c":
						if (NSApplication.SharedApplication.SendAction (new ObjCRuntime.Selector ("copy:"), null, this)) { return true; }
						break;
					case "v":
						if (NSApplication.SharedApplication.SendAction (new ObjCRuntime.Selector ("paste:"), null, this)) { return true; }
						break;
					case "z":
						if (NSApplication.SharedApplication.SendAction (new ObjCRuntime.Selector ("undo:"), null, this)) { return true; }
						break;
					case "a":
						if (NSApplication.SharedApplication.SendAction (new ObjCRuntime.Selector ("selectAll:"), null, this)) { return true; }
						break;
					}
				} else if (theEvent.ModifierFlags.HasFlag(NSEventModifierMask.CommandKeyMask) && theEvent.ModifierFlags.HasFlag(NSEventModifierMask.ShiftKeyMask)) {
					if (theEvent.CharactersIgnoringModifiers == "Z") {
						if (NSApplication.SharedApplication.SendAction (new ObjCRuntime.Selector ("redo:"), null, this)) { return true; }
					}
				}
			}
			return base.PerformKeyEquivalent (theEvent);
		}

	}

}