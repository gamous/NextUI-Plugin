using System;
using System.IO;
using System.Threading;
using NextUIPlugin.Cef.App;
using Xilium.CefGlue;

#if DEBUG
using Dalamud.Logging;
using System.Diagnostics;
#endif

namespace NextUIPlugin.Cef {
	internal static class CefHandler {
		public static void Initialize(
			string cacheDir,
			string cefDir,
			string pluginDir
		) {
			var settings = new CefSettings() {
				CachePath = cacheDir,
				MultiThreadedMessageLoop = true,
				UncaughtExceptionStackSize = 5,
				WindowlessRenderingEnabled = true,
				// BrowserSubprocessPath = Path.Combine(cefDir, "CefSharp.BrowserSubprocess.exe"),
				BrowserSubprocessPath = Path.Combine(cefDir, "CustomSubProcess.exe"),
#if DEBUG
				LogFile = Path.Combine(pluginDir, "cef-debug.log"),
#else
				LogSeverity = CefLogSeverity.Fatal,
#endif
			};

			var mainArgs = new CefMainArgs(Array.Empty<string>());
			var cefApp = new NUCefApp();

			CefRuntime.Load(cefDir);
			CefRuntime.EnableHighDpiSupport();
			CefRuntime.Initialize(mainArgs, settings, cefApp, IntPtr.Zero);

			// Maybe we dont need this anymore
			// Thread.Sleep(100);
#if DEBUG
			PluginLog.Log("Cef initialized " + " " + CefRuntime.ChromeVersion);
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
			foreach (var process in Process.GetProcessesByName("CustomSubProcess")) {
				PluginLog.Log("KILLED CEF " + process.Id);
				process.Kill();
			}

			PluginLog.Log("CEF SHUT? ");
			//CefRuntime.Shutdown();
#else
			// However in prod we gotta shutdown in order to save user data to cache.
			CefRuntime.Shutdown();
#endif
		}
	}
}