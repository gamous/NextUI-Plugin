using System;
using NextUIShared.Data;

namespace NextUIShared.Request {
	[Serializable]
	public class KeyEventRequest : DownstreamIpcRequest {
		public KeyEventType keyEventType;
		public bool systemKey;
		public int userKeyCode;
		public int nativeKeyCode;
		public InputModifier modifier;
		
		public KeyEventRequest() {
			reqType = "keyEvent";
		}
	}
}