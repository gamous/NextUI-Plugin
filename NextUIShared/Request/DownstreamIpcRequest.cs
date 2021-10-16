using System;

namespace NextUIShared.Request {
	[Serializable]
	public class DownstreamIpcRequest {
		public string reqType = "none";
	}

	[Serializable]
	public class NavigateInlayRequest : DownstreamIpcRequest {
		public string url = "";

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
		public string url = "";
		public int width;
		public int height;

		public NewInlayRequest() {
			reqType = "new";
		}
	}

	[Serializable]
	public class ResizeInlayRequest : DownstreamIpcRequest {
		public int width;
		public int height;

		public ResizeInlayRequest() {
			reqType = "resize";
		}
	}
}