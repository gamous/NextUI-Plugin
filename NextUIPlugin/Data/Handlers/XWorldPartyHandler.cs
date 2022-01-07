using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Dalamud.Logging;
using Dalamud.Memory;
using Fleck;
using NextUIPlugin.Data.CrossWorld;
using NextUIPlugin.Socket;

namespace NextUIPlugin.Data.Handlers {
	public static unsafe class XWorldPartyHandler {
		internal static List<ulong> xwParty = new();

		delegate IntPtr CrossRealmGetPtr();

		static CrossRealmGetPtr? crossRealmGetPtrDelegate;

		internal static CrossWorldParty* crossWorldParty;

		public static void RegisterCommands() {
			var proxyCrossRealmPtr =
				NextUIPlugin.sigScanner.ScanText("48 8B 05 ?? ?? ?? ?? C3 CC CC CC CC CC CC CC CC 40 53 41 57");

			crossRealmGetPtrDelegate =
				Marshal.GetDelegateForFunctionPointer<CrossRealmGetPtr>(proxyCrossRealmPtr);

			PluginLog.Information("InfoProxyCrossRealm_GetPtr: 0x" + proxyCrossRealmPtr.ToString("X"));

			var offset2 = crossRealmGetPtrDelegate() + 0x3A0;
			crossWorldParty = (CrossWorldParty*)offset2;

			NextUISocket.RegisterCommand("getCrossWorldParty", GetXWorldParty);
		}

		internal static void GetXWorldParty(IWebSocketConnection socket, SocketEvent ev) {
			if (crossRealmGetPtrDelegate == null) {
				return;
			}
			var currentParty = GetCrossWorldParty();

			NextUISocket.Respond(socket, ev, new { currentParty });
		}

		public static void Watch() {
			var sockets = NextUIPlugin.socketServer.GetEventSubscriptions("crossWorldPartyChanged");
			if (sockets == null || sockets.Count == 0 || crossRealmGetPtrDelegate == null) {
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
			var xwSize = crossWorldParty->PartySize;
			var xwList = crossWorldParty->PartyMemberList;
			for (var i = 0; i < xwSize; i++) {
				var xwPm = (CrossWorldPartyMember*)((IntPtr)xwList + i * 80);
				output.Add(xwPm->ContentId);
			}

			return output;
		}

		internal static List<object> GetCrossWorldParty() {
			var output = new List<object>();

			var xwSize = crossWorldParty->PartySize;
			var xwList = crossWorldParty->PartyMemberList;

			for (var i = 0; i < xwSize; i++) {
				var xwPm = (CrossWorldPartyMember*)((IntPtr)xwList + i * 80);
				var n = MemoryHelper.ReadStringNullTerminated((IntPtr)xwPm->Name);
				output.Add(new {
					name = n,
					contentId = xwPm->ContentId,
					jobId = xwPm->JobId,
					level = xwPm->Level,
					homeWorld = xwPm->HomeWorld,
					currentWorld = xwPm->CurrentWorld,
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