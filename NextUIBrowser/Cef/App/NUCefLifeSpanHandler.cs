using System;
using Dalamud.Logging;
using Xilium.CefGlue;

namespace NextUIBrowser.Cef.App {
	// ReSharper disable once InconsistentNaming
	public class NUCefLifeSpanHandler : CefLifeSpanHandler {
		public event Action<CefBrowser>? AfterBrowserLoad;

		protected override void OnAfterCreated(CefBrowser browser) {
			PluginLog.Log($"Browser created {browser.IsValid}");
			AfterBrowserLoad?.Invoke(browser);
		}
	}
}