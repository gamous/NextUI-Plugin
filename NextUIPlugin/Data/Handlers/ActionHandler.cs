using Fleck;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;
using NextUIPlugin.Socket;

namespace NextUIPlugin.Data.Handlers {
	public static class ActionHandler {
		internal static ExcelSheet<Action>? actionSheet;

		public static void RegisterCommands() {
			actionSheet = NextUIPlugin.dataManager.GetExcelSheet<Action>();
			NextUISocket.RegisterCommand("getAction", GetAction);
		}

		internal static void GetAction(IWebSocketConnection socket, SocketEvent ev) {
			var objectId = ev.request?.id ?? 0;
			if (objectId == 0) {
				return;
			}

			var action = actionSheet?.GetRow(objectId);
			NextUISocket.Respond(socket, ev, action == null ? null : DataConverter.ActionToObject(action));
		}
	}
}