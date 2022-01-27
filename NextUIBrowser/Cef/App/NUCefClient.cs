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
		public readonly NUCefContextMenuHandler dialogHandler;

		public NUCefClient(Overlay overlay) {
			loadHandler = new NUCefLoadHandler();
			dialogHandler = new NUCefContextMenuHandler();
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

		protected override CefContextMenuHandler GetContextMenuHandler() {
			return dialogHandler;
		}

		public void Dispose() {
			renderHandler.Dispose();
			GC.SuppressFinalize(this);
		}
	}
}