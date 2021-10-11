﻿using System;

namespace RendererProcess.Data {
	[Flags]
	public enum InputModifier {
		None = 0,
		Shift = 1 << 0,
		Control = 1 << 1,
		Alt = 1 << 2,
	}
}