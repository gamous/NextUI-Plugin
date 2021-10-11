using System;
using RendererProcess.Data;

namespace RendererProcess.Ipc {
	[Serializable]
	public class UpstreamIpcRequest {
		public string type = "";
		public Cursor? cursor;
	}
}