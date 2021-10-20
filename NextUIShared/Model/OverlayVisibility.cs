﻿using System;
// ReSharper disable InconsistentNaming

namespace NextUIShared.Model {
	[Flags]
	public enum OverlayVisibility {
		DuringCutscene = 1,
		InCombat = 2,
		InGroup = 4,
		InPVP = 8,
		InDeepDungeon = 16,
	}
}