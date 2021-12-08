using System;

namespace NextUIPlugin.Socket {
	[Serializable]
	public class SocketRequest {
		public uint requestFor;
	}

	[Serializable]
	public class SocketEvent {
		public string guid = "";
		public string type = "";
		public string target = "";
		public string message = "";
		public SocketRequest request;
		public bool accept = false;
	}

	public class SocketEventPartyChanged: SocketEvent {
		public int[] party;

		public SocketEventPartyChanged(int[] party) {
			this.party = party;
		}
	}
}