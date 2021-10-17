using System.IO;
using System.Threading;
using CefSharp;
using CefSharp.OffScreen;
#if DEBUG
using Dalamud.Logging;
using System.Diagnostics;
#endif

namespace NextUIBrowser.Cef {
	internal static class CefHandler {
		public static void Initialize(
			string cacheDir,
			string cefDir,
			string pluginDir
		) {
			var settings = new CefSettings() {
				CachePath = cacheDir,
				UncaughtExceptionStackSize = 5,
				WindowlessRenderingEnabled = true,
				BrowserSubprocessPath = Path.Combine(cefDir, "CefSharp.BrowserSubprocess.exe"),
				CefCommandLineArgs = {
					["autoplay-policy"] = "no-user-gesture-required",
				},
#if DEBUG
				LogFile = Path.Combine(pluginDir, "cef-debug.log"),
#else
				// Don't log useless stuff for release
				LogSeverity = LogSeverity.Fatal,
#endif
			};
			settings.CefCommandLineArgs["allow-no-sandbox-job"] = "1";

			settings.EnableAudio();
			settings.SetOffScreenRenderingBestPerformanceArgs();

			CefSharp.Cef.Initialize(settings, performDependencyCheck: false, browserProcessHandler: null);
			// Otherwise it's a rare chance that making new browser window hangs up
			Thread.Sleep(100);
#if DEBUG
			PluginLog.Log("Cef initialized " + CefSharp.Cef.IsInitialized + " " + CefSharp.Cef.CefVersion);
#endif
		}

		public static void Shutdown() {
#if DEBUG
			// There is literally no point in shutting down CEF as it will never boot up again within same process
			// In order to boot it again, entire cef & plugin needs to be copied somewhere else
			// Definitely don't want to copy 100MB files each time game is executed
			PluginLog.Log("CEF SHUTTING DOWN");

			/*
			 * Nasty trash garbonzo, don't question, Cef.Shutdown does NOT work properly.
			 * If you have better solution, hit me up
			 */
			foreach (var process in Process.GetProcessesByName("CefSharp.BrowserSubprocess")) {
				PluginLog.Log("KILLED CEF " + process.Id);
				process.Kill();
			}

			PluginLog.Log("CEF SHUT? " + CefSharp.Cef.IsShutdown);
#else
			// However in prod we gotta shutdown in order to save user data to cache.
			CefSharp.Cef.Shutdown();
#endif
		}
	}
}