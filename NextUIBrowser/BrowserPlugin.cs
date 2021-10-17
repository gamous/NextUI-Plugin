using System;
using System.IO;
using System.Threading;
using Dalamud.Logging;
using NextUIBrowser.Cef;
using NextUIBrowser.OverlayWindow;
using NextUIShared;

namespace NextUIBrowser {
	// ReSharper disable once UnusedType.Global
	public class BrowserPlugin : INuPlugin {
		public string GetName() => "NextUIBrowser";

		public static IGuiManager? guiManager;
		public static OverlayWindowManager? windowManager;

		public void Initialize(
			string pluginDir, 
			string cefDir, 
			IGuiManager manager
		) {
			PluginLog.Log("Initializing Browser");
			guiManager = manager;

			string cacheDir = Path.Combine(
				Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
				"NUCefSharp\\Cache"
			);
			CefHandler.Initialize(cacheDir, cefDir, pluginDir);

			PluginLog.Log("Initialized Cef");
			windowManager = new OverlayWindowManager();
			windowManager.Initialize(guiManager);
			PluginLog.Log("Initialized Browser");

			// Notify gui manager that micro plugin is ready to go
			guiManager.MicroPluginLoaded();
		}

		public void Shutdown() {
			windowManager?.Dispose();
			CefHandler.Shutdown();
			PluginLog.Log("Cef was shut down");
			guiManager = null;
			windowManager = null;
			// TODO: FIX THIS
		}
	}
}