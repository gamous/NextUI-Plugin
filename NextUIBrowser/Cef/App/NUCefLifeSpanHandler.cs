using System;
using Dalamud.Logging;
using Xilium.CefGlue;

namespace NextUIBrowser.Cef.App {
	// ReSharper disable once InconsistentNaming
	public class NUCefLifeSpanHandler : CefLifeSpanHandler {
		public event Action<CefBrowser>? AfterBrowserLoad;
		public event Action<CefBrowser>? AfterBrowserPopupLoad;

		protected override void OnAfterCreated(CefBrowser browser) {
			if (browser.IsPopup) {
				PluginLog.Log($"Browser popup created {browser.IsValid}");
				AfterBrowserPopupLoad?.Invoke(browser);
				return;
			}

			PluginLog.Log($"Browser created {browser.IsValid}");
			AfterBrowserLoad?.Invoke(browser);
		}
	}
}