using System.IO;
using CefSharp;
using CefSharp.OffScreen;
using Dalamud.Logging;
using NextUIPlugin;

namespace NextUIBrowser {
	public class BrowserPlugin : INuPlugin {
		public string GetName() => "NextUIBrowser";

		public void Initialize(string dir) {
			var settings = new CefSettings();

			var browser = Path.Combine(dir, @"CefSharp.BrowserSubprocess.exe");
			var locales = Path.Combine(dir, @"locales\");
			var res = Path.Combine(dir);

			settings.BrowserSubprocessPath = browser;
			settings.LocalesDirPath = locales;
			settings.ResourcesDirPath = res;

			// Make sure you set performDependencyCheck false
			settings.CefCommandLineArgs["autoplay-policy"] = "no-user-gesture-required";
			PluginLog.Log("CEF 4?");
			settings.EnableAudio();
			settings.SetOffScreenRenderingBestPerformanceArgs();
			Cef.Initialize(settings, performDependencyCheck: true, browserProcessHandler: null);
			
			PluginLog.Log("CEF WORKING?");
		}
	}
}