using System;
using NextUIShared.Data;

namespace NextUIShared.Request {
	[Serializable]
	public struct KeyEventRequest {
		public KeyEventType keyEventType;
		public bool systemKey;
		public int userKeyCode;
		public int nativeKeyCode;
		public InputModifier modifier;
	}
}