using System;

namespace NextUIShared.Data {
	[Flags]
	public enum InputModifier {
		None = 0,
		Shift = 1 << 0,
		Control = 1 << 1,
		Alt = 1 << 2,
		MouseLeft = 1 << 3,
		MouseRight = 1 << 4,
		MouseMiddle = 1 << 5
	}
}