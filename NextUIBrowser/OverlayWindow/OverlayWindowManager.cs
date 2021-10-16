using System;
using System.Collections.Generic;
using System.Linq;
using NextUIBrowser.RenderHandlers;
using NextUIShared;
using NextUIShared.Overlay;
using D3D11 = SharpDX.Direct3D11;
using DXGI = SharpDX.DXGI;

namespace NextUIBrowser.OverlayWindow {
	public class OverlayWindowManager : IDisposable {
		protected Dictionary<Guid, OverlayWindow> overlayWindows = new();

		public static D3D11.Device Device { get; private set; }
		protected IGuiManager guiManager = null!;

		public void Initialize(
			IGuiManager gui
		) {
			guiManager = gui;
			LoadDxDevice();
			guiManager.RequestNewOverlay += CreateOverlayWindow;
		}

		protected void LoadDxDevice() {
			// Find the adapter matching the luid from the parent process
			var factory = new DXGI.Factory1();
			DXGI.Adapter? gameAdapter = factory.Adapters
				.FirstOrDefault(adapter => adapter.Description.Luid == guiManager.AdapterLuid);

			if (gameAdapter == null) {
				// var foundLuids = string.Join(",", factory.Adapters.Select(adapter => adapter.Description.Luid));
				// Console.Error.WriteLine($"FATAL: Could not find adapter matching game adapter LUID {adapterLuid}. Found: {foundLuids}.");
				return;
			}

			// Use the adapter to build the device we'll use
			var flags = D3D11.DeviceCreationFlags.BgraSupport;
#if DEBUG
			flags |= D3D11.DeviceCreationFlags.Debug;
#endif

			Device = new D3D11.Device(gameAdapter, flags);
		}

		// Requested creation of new overlay
		protected void CreateOverlayWindow(Guid guid, Overlay overlay) {
			var textureHandler = new TextureRenderHandler(Device, overlay.Size);
			// Populate texture pointer in overlay data structure and notify if it changes
			overlay.TexturePointer = textureHandler.SharedTextureHandle;
			textureHandler.TexturePointerChange += ptr => { overlay.TexturePointer = ptr; };
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