using Dalamud.Logging;
using Xilium.CefGlue;

namespace NextUIBrowser.Cef.App {
	// ReSharper disable once InconsistentNaming
	public class NUCefApp : CefApp {
		protected override void OnBeforeCommandLineProcessing(string processType, CefCommandLine commandLine) {
			PluginLog.Log("OnBeforeCommandLineProcessing: {0} {1}", processType, commandLine);

			// browser (main) process
			if (!string.IsNullOrEmpty(processType)) {
				return;
			}

			commandLine.AppendSwitch("autoplay-policy", "no-user-gesture-required");

			if (!commandLine.HasSwitch("disable-gpu")) {
				commandLine.AppendSwitch("disable-gpu");
			}

			if (!commandLine.HasSwitch("disable-gpu-compositing")) {
				commandLine.AppendSwitch("disable-gpu-compositing");
			}

			// Synchronize the frame rate between all processes. This results in
			// decreased CPU usage by avoiding the generation of extra frames that
			// would otherwise be discarded. The frame rate can be set at browser
			// creation time via IBrowserSettings.WindowlessFrameRate or changed
			// dynamically using IBrowserHost.SetWindowlessFrameRate. In cefclient
			// it can be set via the command-line using `--off-screen-frame-rate=XX`.
			// See https://bitbucket.org/chromiumembedded/cef/issues/1368 for details.
			if (!commandLine.HasSwitch("enable-begin-frame-scheduling")) {
				commandLine.AppendSwitch("enable-begin-frame-scheduling");
			}

			PluginLog.Log($"CLI Switches: {commandLine}");
		}
	}
}