using System;

namespace NextUIPlugin.Socket {
	[Serializable]
	public class SocketRequest {
		public uint id;
		public object? data;
		public string[]? events;
	}

	[Serializable]
	public class SocketEvent {
		public string guid = "";
		public string type = "";
		public string message = "";
		public SocketRequest? request;
		public bool accept = false;
	}
}