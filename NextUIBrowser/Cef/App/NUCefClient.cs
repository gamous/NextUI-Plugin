using System;
using NextUIShared.Model;
using Xilium.CefGlue;

namespace NextUIBrowser.Cef.App {
	// ReSharper disable once InconsistentNaming
	public class NUCefClient : CefClient {
		public readonly NUCefLoadHandler loadHandler;
		public readonly NUCefRenderHandler renderHandler;

		public event Action<CefBrowser>? AfterBrowserLoad;

		public NUCefClient(Overlay overlay) {
			loadHandler = new NUCefLoadHandler();
			renderHandler = new NUCefRenderHandler(overlay);
			loadHandler.AfterBrowserLoad += browser => {
				AfterBrowserLoad?.Invoke(browser);
			};
		}

		protected override CefRenderHandler GetRenderHandler() {
			return renderHandler;
		}

		protected override CefLoadHandler GetLoadHandler() {
			return loadHandler;
		}
	}
}