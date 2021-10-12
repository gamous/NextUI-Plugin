using System;
using Newtonsoft.Json;

namespace RendererProcess.Ipc {
	[Serializable]
	public class DownstreamIpcRequest {
		public string reqType = "none";

		public static DownstreamIpcRequest? FromJson(string json) {
			DownstreamIpcRequest? plain = JsonConvert.DeserializeObject<DownstreamIpcRequest>(json);
			if (plain == null) {
				throw new Exception("Invalid Json");
			}

			switch (plain.reqType) {
				case "none": return plain;
				case "navigate": return JsonConvert.DeserializeObject<NavigateInlayRequest>(json);
				case "debug": return JsonConvert.DeserializeObject<DebugInlayRequest>(json);
				case "remove": return JsonConvert.DeserializeObject<RemoveInlayRequest>(json);
				case "new": return JsonConvert.DeserializeObject<NewInlayRequest>(json);
				case "resize": return JsonConvert.DeserializeObject<ResizeInlayRequest>(json);
				case "mouseEvent": return JsonConvert.DeserializeObject<MouseEventRequest>(json);
				case "keyEvent": return JsonConvert.DeserializeObject<KeyEventRequest>(json);
			}

			throw new Exception("Unknown reqType");
		}
	}

	[Serializable]
	public class NavigateInlayRequest : DownstreamIpcRequest {
		public string Url;

		public NavigateInlayRequest() {
			reqType = "navigate";
		}
	}

	[Serializable]
	public class DebugInlayRequest : DownstreamIpcRequest {
		public DebugInlayRequest() {
			reqType = "debug";
		}
	}

	[Serializable]
	public class RemoveInlayRequest : DownstreamIpcRequest {
		public RemoveInlayRequest() {
			reqType = "remove";
		}
		// public Guid Guid;
	}

	[Serializable]
	public class NewInlayRequest : DownstreamIpcRequest {
		public string Url;
		public int Width;
		public int Height;

		public NewInlayRequest() {
			reqType = "new";
		}
	}

	[Serializable]
	public class ResizeInlayRequest : DownstreamIpcRequest {
		public int Width;
		public int Height;

		public ResizeInlayRequest() {
			reqType = "resize";
		}
	}
}