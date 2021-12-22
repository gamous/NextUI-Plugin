using System;

namespace NextUIPlugin.Socket {
	[Serializable]
	public class SocketRequest {
		public uint requestFor;
		public string[]? events;
		public string data;
	}

	[Serializable]
	public class SocketEvent {
		public string guid = "";
		public string type = "";
		public string target = "";
		public string message = "";
		public SocketRequest? request;
		public bool accept = false;
	}
}