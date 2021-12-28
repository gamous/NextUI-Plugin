using System;
using Fleck;
using NextUIPlugin.Socket;

namespace NextUIPlugin.Data.Handlers {
	public static class MouseOverHandler {
		public static void RegisterCommands() {
			NextUISocket.RegisterCommand("clearMouseOverEx", (socket, ev) => { SetMouseOverEx(socket, ev, false); });
			NextUISocket.RegisterCommand("setMouseOverEx", (socket, ev) => { SetMouseOverEx(socket, ev); });
		}

		internal static void SetMouseOverEx(IWebSocketConnection socket, SocketEvent ev, bool set = true) {
			try {
				if (set) {
					var objectId = ev.request?.id ?? 0;
					if (objectId == 0) {
						return;
					}

					var target = NextUIPlugin.objectTable.SearchById(objectId);
					if (target == null) {
						NextUISocket.Respond(socket, ev, new { success = false, message = "Invalid object ID" });
						return;
					}

					NextUIPlugin.mouseOverService.target = target;
				}
				else {
					NextUIPlugin.mouseOverService.target = null;
				}

				NextUISocket.Respond(socket, ev, new { success = true });
			}
			catch (Exception err) {
				NextUISocket.Respond(socket, ev, new { success = false, message = err.ToString() });
			}
		}
	}
}