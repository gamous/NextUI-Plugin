using System;
using System.IO;
using System.Reflection;
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
			AppDomain currentDomain = AppDomain.CurrentDomain;
			currentDomain.AssemblyResolve += CurrentDomainOnAssemblyResolve;
			PluginLog.Log("Initializing Browser");
			
			GuiManager = guiManager;

			string cacheDir = Path.Combine(
				Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
				"NUCefSharp\\Cache"
			);
			CefHandler.Initialize(cacheDir);

			windowManager = new OverlayWindowManager();
			windowManager.Initialize(guiManager);
		}

		protected Assembly? CurrentDomainOnAssemblyResolve(object? sender, ResolveEventArgs args) {
			
			string folderPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
			string assemblyPath = Path.Combine(folderPath, new AssemblyName(args.Name).Name + ".dll");
			if (!File.Exists(assemblyPath)) return null;
			Assembly assembly = Assembly.LoadFrom(assemblyPath);
			return assembly;
		}


		public void Shutdown() {
			// Cef.Shutdown();
			CefHandler.Shutdown();
		}
	}
}