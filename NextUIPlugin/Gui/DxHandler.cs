using Dalamud.Plugin;
using D3D11 = SharpDX.Direct3D11;
using DXGI = SharpDX.DXGI;
using System;

namespace NextUIPlugin.Overlay {
	public static class DxHandler {
		public static D3D11.Device? Device { get; set; }
		public static IntPtr WindowHandle { get; set; }
		public static long AdapterLuid { get; set; }

		public static void Initialize(DalamudPluginInterface pluginInterface) {
			Device = pluginInterface.UiBuilder.Device;
			//Device = new D3D11.Device(SharpDX.Direct3D.DriverType.Hardware, D3D11.DeviceCreationFlags.BgraSupport | D3D11.DeviceCreationFlags.Debug);

			// Grab the window handle, we'll use this for setting up our wndproc hook
			WindowHandle = pluginInterface.UiBuilder.WindowHandlePtr;

			// Get the game's device adapter, we'll need that as a reference for the render process.
			DXGI.Device? dxgiDevice = Device.QueryInterface<DXGI.Device>();
			AdapterLuid = dxgiDevice.Adapter.Description.Luid;
		}

		public static void Shutdown() {
			Device = null;
		}
	}
}