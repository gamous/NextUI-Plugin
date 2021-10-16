using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Logging;
using NextUIBrowser.RenderHandlers;
using NextUIShared;
using NextUIShared.Overlay;
using D3D11 = SharpDX.Direct3D11;
using DXGI = SharpDX.DXGI;

namespace NextUIBrowser.OverlayWindow {
	public class OverlayWindowManager : IDisposable {
		protected readonly List<OverlayWindow> overlayWindows = new();

		protected static D3D11.Device? device;
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
			DXGI.Adapter? gameAdapter = factory
				.Adapters
				.FirstOrDefault(adapter => adapter.Description.Luid == guiManager.AdapterLuid);

			if (gameAdapter == null) {
				var foundLuids = string.Join(",", factory.Adapters.Select(adapter => adapter.Description.Luid));
				// Console.Error.WriteLine($"FATAL: Could not find adapter matching game adapter LUID {adapterLuid}. Found: {foundLuids}.");
				PluginLog.Error(
					$"FATAL: Could not find adapter matching game adapter LUID {guiManager.AdapterLuid}. " +
					$"Found: {foundLuids}."
				);
				return;
			}

			// Use the adapter to build the device we'll use
			var flags = D3D11.DeviceCreationFlags.BgraSupport;
#if DEBUG
			flags |= D3D11.DeviceCreationFlags.Debug;
#endif

			device = new D3D11.Device(gameAdapter, flags);
		}

		// Requested creation of new overlay
		protected void CreateOverlayWindow(Guid guid, Overlay overlay) {
			if (device == null) {
				PluginLog.Warning("Cannot create overlay window, device was not initialized");
				return;
			}

			var textureHandler = new TextureRenderHandler(device, overlay.Size);

			// Populate texture pointer in overlay data structure and notify if it changes
			overlay.TexturePointer = textureHandler.SharedTextureHandle;
			textureHandler.TexturePointerChange += ptr => { overlay.TexturePointer = ptr; };
			// Also request cursor if it changes
			textureHandler.CursorChanged += (_, cursor) => { overlay.SetCursor(cursor); };

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