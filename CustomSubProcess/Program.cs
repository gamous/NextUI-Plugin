using System;
using Xilium.CefGlue;

namespace CustomSubProcess {
	internal static class Program {
		[STAThread]
		public static int Main(string[] args) {
			// Load CEF. This checks for the correct CEF version.
			CefRuntime.Load();
			CefRuntime.EnableHighDpiSupport();

			var mainArgs = new CefMainArgs(args);
			var cefApp = new DemoApp();
			Console.WriteLine($"Subprocess starting");
			Console.WriteLine($"Args {string.Join(' ', args)}");

			var exitCode = CefRuntime.ExecuteProcess(mainArgs, cefApp, IntPtr.Zero);
			if (exitCode != -1) {
				Console.WriteLine($"Unexpected error {exitCode}");
				return exitCode;
			}


			Console.WriteLine($"Sub process exit {exitCode}");

			CefRuntime.Shutdown();
			return exitCode;
		}
	}

	internal sealed class DemoApp : CefApp {
		// protected override void OnBeforeCommandLineProcessing(string processType, CefCommandLine commandLine) {
		// }
	}
}