using System;
using Newtonsoft.Json;
using RendererProcess.Data;

namespace RendererProcess.Ipc {
	[Serializable]
	public class UpstreamIpcRequest {
		public string reqType = "none";

		public static UpstreamIpcRequest? FromJson(string json) {
			UpstreamIpcRequest? plain = JsonConvert.DeserializeObject<UpstreamIpcRequest>(json);
			if (plain == null) {
				throw new Exception("Invalid Json");
			}

			switch (plain.reqType) {
				case "none": return plain;
				case "ready": return JsonConvert.DeserializeObject<ReadyNotificationRequest>(json);
				case "setCursor": return JsonConvert.DeserializeObject<SetCursorRequest>(json);
			}

			throw new Exception("Unknown reqType");
		}
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