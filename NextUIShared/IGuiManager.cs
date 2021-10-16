using System;

namespace NextUIShared {
	public interface IGuiManager {
		public long AdapterLuid { get; set; }

		public Action<Guid, Overlay.Overlay> RequestNewOverlay { get; set; }
	}
}