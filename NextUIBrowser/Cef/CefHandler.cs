using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using CefSharp.OffScreen;
using Dalamud.Logging;

namespace NextUIBrowser.Cef {
	internal static class CefHandler {
		public static void Initialize(
			string cacheDir,
			string cefDir,
			string pluginDir
		) {
			foreach (var mod in Process.GetCurrentProcess().Modules) {
				PluginLog.Log("Process Loaded modules " + mod.GetType().Name);
			}

			
			var settings = new CefSettings() {
				CachePath = cacheDir,
				// LogFile = Path.Combine(pluginDir, "cef-debug.log"),
				UncaughtExceptionStackSize = 5,
				WindowlessRenderingEnabled = true,
				BrowserSubprocessPath = Path.Combine(cefDir, "CefSharp.BrowserSubprocess.exe"),
				CefCommandLineArgs = {
					["autoplay-policy"] = "no-user-gesture-required",
				},
// #if !DEBUG
				// LogSeverity = LogSeverity.Fatal,
// #endif
			};
			settings.CefCommandLineArgs["allow-no-sandbox-job"] = "1";

			settings.EnableAudio();
			settings.SetOffScreenRenderingBestPerformanceArgs();
			PluginLog.Log("Settings OK");

			CefSharp.Cef.Initialize(settings, performDependencyCheck: false, browserProcessHandler: null);
			// Otherwise it's a rare chance that making new browser window hangs up
			Thread.Sleep(1000);
			PluginLog.Log("Cef initialized " + CefSharp.Cef.IsInitialized + " " + CefSharp.Cef.CefVersion);
	
	
			foreach (var mod in Process.GetCurrentProcess().Modules) {
				PluginLog.Log("Process Loaded modules " + mod.GetType().Name);
			}
		}

		public static void Shutdown() {
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
		}
	}
}