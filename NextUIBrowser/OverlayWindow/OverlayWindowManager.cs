using System;
using System.Collections.Generic;
using NextUIBrowser.RenderHandlers;
using NextUIShared;
using NextUIShared.Overlay;

namespace NextUIBrowser.OverlayWindow {
	public class OverlayWindowManager : IDisposable {
		protected Dictionary<Guid, OverlayWindow> overlayWindows = new();

		protected IGuiManager guiManager = null!; 
		public void Initialize(
			IGuiManager gui
		) {
			guiManager = gui;
			guiManager.RequestNewOverlay += CreateOverlayWindow;
		}

		// Requested creation of new overlay
		protected void CreateOverlayWindow(Guid guid, Overlay overlay) {
			var textureHandler = new TextureRenderHandler(guiManager.Device, overlay.Size);
			// Populate texture pointer in overlay data structure and notify if it changes
			overlay.TexturePointer = textureHandler.SharedTextureHandle;
			textureHandler.TexturePointerChange += ptr => {
				overlay.TexturePointer = ptr;
			};
			var overlayWindow = new OverlayWindow(overlay, textureHandler);
			overlayWindows.Add(guid, overlayWindow);

			overlayWindow.Initialize();
		}

		public void Dispose() {
			foreach (var overlay in overlayWindows) {
				overlay.Value.Dispose();
			}
		}
	}
}