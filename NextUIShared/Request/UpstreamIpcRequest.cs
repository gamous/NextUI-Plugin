using System;
using NextUIShared.Data;

namespace NextUIShared.Request {
	[Serializable]
	public class UpstreamIpcRequest {
		public string reqType = "none";
	}

	[Serializable]
	public class ReadyNotificationRequest : UpstreamIpcRequest {
		public ReadyNotificationRequest() {
			reqType = "ready";
		}
	}

	[Serializable]
	public class SetCursorRequest : UpstreamIpcRequest {
		public SetCursorRequest() {
			reqType = "setCursor";
		}
		public Cursor cursor;
	}
}