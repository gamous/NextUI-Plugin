using System.Collections.Generic;
using Dalamud.Game.ClientState.Objects.Types;
using Fleck;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;
using NextUIPlugin.Socket;

namespace NextUIPlugin.Data.Handlers {
	public static class StatusHandler {
		internal static ExcelSheet<Status>? statusSheet;
		
		public static void RegisterCommands() {
			statusSheet = NextUIPlugin.dataManager.GetExcelSheet<Status>();
			NextUISocket.RegisterCommand("getActorStatuses", GetActorStatuses);
			NextUISocket.RegisterCommand("getStatus", GetStatus);
		}

		internal static void GetStatus(IWebSocketConnection socket, SocketEvent ev) {
			var objectId = ev.request?.id ?? 0;
			if (objectId == 0) {
				return;
			}

			var status = statusSheet?.GetRow(objectId);
			NextUISocket.Respond(socket, ev, status == null ? null : DataConverter.LuminaStatusToObject(status));
		}
		
		internal static void GetActorStatuses(IWebSocketConnection socket, SocketEvent ev) {
			var objectId = ev.request?.id ?? 0;
			if (objectId == 0) {
				return;
			}

			var actor = NextUIPlugin.objectTable.SearchById(objectId);

			var statusList = new List<object>();
			if (actor != null && actor is BattleChara chara) {
				foreach (var status in chara.StatusList) {
					statusList.Add(DataConverter.StatusToObject(status));
				}
			}

			NextUISocket.Respond(socket, ev, statusList);
		}
	}
}