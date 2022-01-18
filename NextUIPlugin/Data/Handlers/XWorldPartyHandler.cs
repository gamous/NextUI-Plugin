using System;
using System.Collections.Generic;
using Dalamud.Memory;
using FFXIVClientStructs.FFXIV.Client.UI.Info;
using Fleck;
using NextUIPlugin.Socket;

namespace NextUIPlugin.Data.Handlers {
	public static unsafe class XWorldPartyHandler {
		internal static List<ulong> xwParty = new();

		internal static InfoProxyCrossRealm* infoProxyCrossRealm;

		public static void RegisterCommands() {
			infoProxyCrossRealm = InfoProxyCrossRealm.Instance();
			
			NextUISocket.RegisterCommand("getCrossWorldParty", GetXWorldParty);
		}

		internal static void GetXWorldParty(IWebSocketConnection socket, SocketEvent ev) {
			var currentParty = GetCrossWorldParty();

			NextUISocket.Respond(socket, ev, new { currentParty });
		}

		public static void Watch() {
			var sockets = NextUIPlugin.socketServer.GetEventSubscriptions("crossWorldPartyChanged");
			if (sockets == null || sockets.Count == 0) {
				return;
			}

			var currentParty = GetCrossWorldPartyIds();

			if (xwParty.Count != currentParty.Count) {
				xwParty = currentParty;
				BroadcastPartyChanged(sockets);
				return;
			}

			var eq = DataHandler.CompareList(xwParty, currentParty);
			if (eq) {
				return;
			}

			BroadcastPartyChanged(sockets);
			xwParty = currentParty;
		}

		internal static List<ulong> GetCrossWorldPartyIds() {
			var output = new List<ulong>();

			var xwSize = InfoProxyCrossRealm.GetPartyMemberCount();
			if (xwSize == 0) {
				return output;
			}

			for (var i = 0u; i < xwSize; i++) {
				var xwPm = InfoProxyCrossRealm.GetGroupMember(i);
				output.Add(xwPm->ContentId);
			}

			return output;
		}

		internal static List<object> GetCrossWorldParty() {
			var output = new List<object>();

			var xwSize = InfoProxyCrossRealm.GetPartyMemberCount();
			if (xwSize == 0) {
				return output;
			}

			for (var i = 0u; i < xwSize; i++) {
				var xwPm = InfoProxyCrossRealm.GetGroupMember(i, infoProxyCrossRealm->LocalPlayerGroupIndex);
				var n = MemoryHelper.ReadStringNullTerminated((IntPtr)xwPm->Name);
				output.Add(new {
					name = n,
					contentId = xwPm->ContentId.ToString("X16"),
					jobId = xwPm->ClassJobId,
					level = xwPm->Level,
					homeWorld = xwPm->HomeWorld,
					currentWorld = xwPm->CurrentWorld,
					memberIndex = xwPm->MemberIndex,
					isPartyLeader = xwPm->IsPartyLeader != 0,
				});
			}

			return output;
		}

		internal static void BroadcastPartyChanged(List<IWebSocketConnection> sockets) {
			var currentParty = GetCrossWorldParty();

			NextUISocket.BroadcastTo(new {
				@event = "crossWorldPartyChanged",
				data = new { currentParty },
			}, sockets);
		}
	}
}