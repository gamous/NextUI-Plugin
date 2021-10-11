using System;
using RendererProcess.Data;

namespace RendererProcess.Ipc {
	[Serializable]
	public class KeyEventRequest : DownstreamIpcRequest {
		public KeyEventType keyEventType;
		public bool systemKey;
		public int userKeyCode;
		public int nativeKeyCode;
		public InputModifier modifier;
	}
}