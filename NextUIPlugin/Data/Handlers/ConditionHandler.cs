using System;
using System.Collections.Generic;
using Dalamud.Game.ClientState.Conditions;
using Fleck;
using NextUIPlugin.Socket;

namespace NextUIPlugin.Data.Handlers {
	public static class ConditionHandler {

		public static void RegisterCommands() {
			NextUISocket.RegisterCommand("getConditions", GetConditions);
			NextUIPlugin.condition.ConditionChange += ConditionOnConditionChange;
		}

		internal static void ConditionOnConditionChange(ConditionFlag flag, bool value) {
			var sockets = NextUIPlugin.socketServer.GetEventSubscriptions("conditionChanged");
			if (sockets == null || sockets.Count == 0) {
				return;
			}
			
			NextUISocket.BroadcastTo(new {
				@event = "conditionChanged",
				data = new {
					condition = flag.ToString(),
					value
				},
			}, sockets);
		}

		internal static void GetConditions(IWebSocketConnection socket, SocketEvent ev) {
			var values = Enum.GetValues<ConditionFlag>();
			var output = new Dictionary<string, bool>();
			foreach (var flag in values) {
				output.Add(flag.ToString(), NextUIPlugin.condition[flag]);
			}
			NextUISocket.Respond(socket, ev, output);
		}
	}
}