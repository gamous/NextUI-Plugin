using System;
using System.Collections.Generic;
using NextUIBrowser.RenderHandlers;
using NextUIShared;
using NextUIShared.Model;

namespace NextUIBrowser.OverlayWindow {
	public class OverlayWindowManager : IDisposable {
		protected readonly List<OverlayWindow> overlayWindows = new();

		protected IGuiManager guiManager = null!;

		public void Initialize(
			IGuiManager gui
		) {
			guiManager = gui;
			guiManager.RequestNewOverlay += CreateOverlayWindow;
		}


		// Requested creation of new overlay
		protected void CreateOverlayWindow(Overlay overlay) {
			var textureHandler = new TextureRenderHandler(overlay);
			var overlayWindow = new OverlayWindow(overlay, textureHandler);
			overlayWindows.Add(overlayWindow);

			overlayWindow.Initialize();
		}

		public void Dispose() {
			foreach (var overlay in overlayWindows) {
				overlay.Dispose();
			}
		}
	}
}