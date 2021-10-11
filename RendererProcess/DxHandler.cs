using System;
using System.Linq;
using D3D11 = SharpDX.Direct3D11;
using DXGI = SharpDX.DXGI;

namespace RendererProcess {
	static class DxHandler {
		public static D3D11.Device? Device { get; set; }

		public static bool Initialize(long adapterLuid) {
			// Find the adapter matching the luid from the parent process
			DXGI.Factory1 factory = new();

			DXGI.Adapter? gameAdapter = factory
				.Adapters
				.FirstOrDefault(adapter => adapter.Description.Luid == adapterLuid);

			if (gameAdapter == null) {
				string foundLuids = string.Join(",", factory.Adapters.Select(adapter => adapter.Description.Luid));
				Console.Error.WriteLine(
					$"FATAL: Could not find adapter matching game adapter LUID {adapterLuid}. Found: {foundLuids}."
				);
				return false;
			}

			// Use the adapter to build the device we'll use
			D3D11.DeviceCreationFlags flags = D3D11.DeviceCreationFlags.BgraSupport;
#if DEBUG
			flags |= D3D11.DeviceCreationFlags.Debug;
#endif

			Device = new D3D11.Device(gameAdapter, flags);

			return true;
		}

		public static void Shutdown() {
			Device?.Dispose();
		}
	}
}