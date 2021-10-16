using System;

namespace NextUIShared {
	public interface IGuiManager {
		public long AdapterLuid { get; set; }

		public event Action<Guid, Overlay.Overlay> RequestNewOverlay;
		public void MicroPluginLoaded();
	}
}