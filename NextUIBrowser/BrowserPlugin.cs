using System;
using System.IO;
using System.Reflection;
using System.Threading;
using Dalamud.Logging;
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
			PluginLog.Log("Initializing Browser");

			GuiManager = guiManager;

			string cacheDir = Path.Combine(
				Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
				"NUCefSharp\\Cache"
			);
			CefHandler.Initialize(cacheDir);

			windowManager = new OverlayWindowManager();
			windowManager.Initialize(guiManager);
			PluginLog.Log("Initialized Browser");

			// Notify gui manager that micro plugin is ready to go
			guiManager.MicroPluginLoaded();
		}

		public void Shutdown() {
			windowManager.Dispose();
			CefHandler.Shutdown();
			// TODO: FIX THIS
		}
	}
}