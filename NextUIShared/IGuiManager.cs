using System;
using D3D11 = SharpDX.Direct3D11;

namespace NextUIShared {
	public interface IGuiManager {
		
		public D3D11.Device Device { get; set; } 
		public IntPtr WindowHandlePtr { get; set; }
		public static long AdapterLuid { get; set; }

		public Action<Guid, Overlay.Overlay> RequestNewOverlay { get; set; }
	}
}