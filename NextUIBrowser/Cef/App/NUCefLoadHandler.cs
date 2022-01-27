using System;
using Dalamud.Logging;
using Xilium.CefGlue;

namespace NextUIBrowser.Cef.App {
	// ReSharper disable once InconsistentNaming
	public class NUCefLoadHandler : CefLoadHandler {

		protected override void OnLoadStart(CefBrowser browser, CefFrame frame, CefTransitionType transitionType) {
			// A single CefBrowser instance can handle multiple requests
			//   for a single URL if there are frames (i.e. <FRAME>, <IFRAME>).
			if (frame.IsMain) {
				PluginLog.Log($"START: {browser.GetMainFrame().Url}");
			}
			//PluginLog.Log($"START WAT: {frame.Url}");
		}

		protected override void OnLoadEnd(CefBrowser browser, CefFrame frame, int httpStatusCode) {
			if (frame.IsMain) {
				PluginLog.Log($"END: {browser.GetMainFrame().Url}, {httpStatusCode}");
			}
		}
	}
}