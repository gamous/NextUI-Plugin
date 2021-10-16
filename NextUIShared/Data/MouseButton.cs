using System;

namespace NextUIShared.Data {
	[Flags]
	public enum MouseButton {
		None = 0,
		Primary = 1 << 0,
		Secondary = 1 << 1,
		Tertiary = 1 << 2,
		Fourth = 1 << 3,
		Fifth = 1 << 4,
	}
}