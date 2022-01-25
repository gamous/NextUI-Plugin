using System;
using NextUIShared.Model;
using Xilium.CefGlue;

namespace NextUIBrowser.Cef.App {
	// ReSharper disable once InconsistentNaming
	public class NUCefClient : CefClient, IDisposable {
		public readonly NUCefLoadHandler loadHandler;
		public readonly NUCefRenderHandler renderHandler;
		public readonly NUCefLifeSpanHandler lifeSpanHandler;
		public readonly NUCefDisplayHandler displayHandler;

		public NUCefClient(Overlay overlay) {
			loadHandler = new NUCefLoadHandler();
			renderHandler = new NUCefRenderHandler(overlay);
			displayHandler = new NUCefDisplayHandler(renderHandler);
			lifeSpanHandler = new NUCefLifeSpanHandler();
		}

		protected override CefRenderHandler GetRenderHandler() {
			return renderHandler;
		}

		protected override CefLoadHandler GetLoadHandler() {
			return loadHandler;
		}

		protected override CefLifeSpanHandler GetLifeSpanHandler() {
			return lifeSpanHandler;
		}

		protected override CefDisplayHandler GetDisplayHandler() {
			return displayHandler;
		}

		public void Dispose() {
			renderHandler.Dispose();
			GC.SuppressFinalize(this);
		}
	}
}