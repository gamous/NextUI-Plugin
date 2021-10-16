using System;
using NextUIShared.Model;

namespace NextUIShared {
	public interface IGuiManager {
		public long AdapterLuid { get; set; }

		public event Action<Overlay> RequestNewOverlay;
		public void MicroPluginLoaded();
	}
}