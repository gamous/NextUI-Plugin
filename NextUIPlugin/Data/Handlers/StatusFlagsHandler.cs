using System;
using System.Collections.Generic;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects.Enums;
using Fleck;
using NextUIPlugin.Socket;

namespace NextUIPlugin.Data.Handlers {
	public static class StatusFlagsHandler {
		internal static StatusFlags playerFlags = 0;

		public static void RegisterCommands() {
			NextUISocket.RegisterCommand("getStatusFlags", GetStatusFlags);
		}

		public static void Watch() {
			var sockets = NextUIPlugin.socketServer.GetEventSubscriptions("statusFlagsChanged");
			if (sockets == null || sockets.Count == 0 || NextUIPlugin.clientState.LocalPlayer == null) {
				return;
			}

			if (playerFlags == NextUIPlugin.clientState.LocalPlayer.StatusFlags) {
				return;
			}

			playerFlags = NextUIPlugin.clientState.LocalPlayer.StatusFlags;
			NextUISocket.BroadcastTo(new {
				@event = "statusFlagsChanged",
				data = GetPlayerStatusFlags(),
			}, sockets);
		}

		internal static void GetStatusFlags(IWebSocketConnection socket, SocketEvent ev) {
			if (NextUIPlugin.clientState.LocalPlayer == null) {
				NextUISocket.Respond(socket, ev, new Dictionary<string, bool>());
				return;
			}

			var output = GetPlayerStatusFlags();
			NextUISocket.Respond(socket, ev, output);
		}

		internal static Dictionary<string, bool> GetPlayerStatusFlags() {
			var output = new Dictionary<string, bool>();
			
			var values = Enum.GetValues<StatusFlags>();
			foreach (var flag in values) {
				output.Add(
					flag.ToString(),
					NextUIPlugin.clientState.LocalPlayer!.StatusFlags.HasFlag(flag)
				);
			}

			return output;
		}
	}
}