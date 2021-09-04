namespace NextUIPlugin.Socket {
	public class SocketEvent {
		public string guid;
		public string type;
		public string target;
		public string message;
	}

	public class SocketEventPartyChanged: SocketEvent {
		public int[] party;
	}
}