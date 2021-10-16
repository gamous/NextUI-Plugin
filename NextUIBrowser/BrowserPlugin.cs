using System;
using System.IO;
using CefSharp.OffScreen;
using NextUIBrowser.Cef;
using NextUIBrowser.OverlayWindow;
using NextUIShared;

namespace NextUIBrowser {
	public class BrowserPlugin : INuPlugin {
		public string GetName() => "NextUIBrowser";

		public static IGuiManager GuiManager = null!;
		public static OverlayWindowManager windowManager = null!;

		public void Initialize(
			string dir,
			IGuiManager guiManager
		) {
			GuiManager = guiManager;

			string cacheDir = Path.Combine(
				Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
				"NUCefSharp\\Cache"
			);
			CefHandler.Initialize(cacheDir);

			windowManager = new OverlayWindowManager();
			windowManager.Initialize(guiManager);
		}

		public void Shutdown() {
			// Cef.Shutdown();
			CefHandler.Shutdown();
		}
	}
}