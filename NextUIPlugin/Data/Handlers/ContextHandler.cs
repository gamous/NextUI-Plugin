using System;
using System.Runtime.InteropServices;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Logging;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using Fleck;
using NextUIPlugin.Socket;

namespace NextUIPlugin.Data.Handlers {
	public static unsafe class ContextHandler {
		internal static class Signatures {
			internal const string SendTellCommandSig = "E8 ?? ?? ?? ?? B3 01 48 8B 74 24 ??";
		}

		internal delegate void SendTellCommandDelegate(
			long raptureModulePointer, char* characterName, ushort homeWorldId
		);

		internal static SendTellCommandDelegate? SendTellCommand { get; set; }
		internal static UIModule* uiModule;

		public static void RegisterCommands() {
			uiModule = (UIModule*)NextUIPlugin.gameGui.GetUIModule();
			var sendTellPtr = NextUIPlugin.sigScanner.ScanText(Signatures.SendTellCommandSig);
			if (sendTellPtr != IntPtr.Zero) {
				SendTellCommand = Marshal.GetDelegateForFunctionPointer<SendTellCommandDelegate>(sendTellPtr);
			}
			else {
				PluginLog.Warning("Signature for Send Tell Not found");
			}

			NextUISocket.RegisterCommand("examine", Examine);
			NextUISocket.RegisterCommand("leaveParty", LeaveParty);
			NextUISocket.RegisterCommand("disbandParty", DisbandParty);
			NextUISocket.RegisterCommand("sendTell", SendTell);

			NextUISocket.RegisterCommand("showEmoteWindow", (socket, ev) => {
				NextUIPlugin.xivCommon.Functions.Chat.SendMessage("/emotelist");
				NextUISocket.Respond(socket, ev, new { success = true });
			});

			NextUISocket.RegisterCommand("showSignsWindow", (socket, ev) => {
				NextUIPlugin.xivCommon.Functions.Chat.SendMessage("/enemysign");
				NextUISocket.Respond(socket, ev, new { success = true });
			});

			NextUISocket.RegisterCommand("inviteToParty", (socket, ev) => {
				NextUIPlugin.xivCommon.Functions.Chat.SendMessage("/invite <target>");
				NextUISocket.Respond(socket, ev, new { success = true });
			});

			NextUISocket.RegisterCommand("meldRequest", (socket, ev) => {
				NextUIPlugin.xivCommon.Functions.Chat.SendMessage("/meldrequest");
				NextUISocket.Respond(socket, ev, new { success = true });
			});

			NextUISocket.RegisterCommand("tradeRequest", (socket, ev) => {
				NextUIPlugin.xivCommon.Functions.Chat.SendMessage("/trade");
				NextUISocket.Respond(socket, ev, new { success = true });
			});

			NextUISocket.RegisterCommand("followTarget", (socket, ev) => {
				NextUIPlugin.xivCommon.Functions.Chat.SendMessage("/follow");
				NextUISocket.Respond(socket, ev, new { success = true });
			});

			NextUISocket.RegisterCommand("promotePartyMember", (socket, ev) => {
				PartyLeaderOperation(socket, ev, "leader");
			});

			NextUISocket.RegisterCommand("kickFromParty", (socket, ev) => {
				PartyLeaderOperation(socket, ev, "kick");
			});
		}

		public static void Examine(IWebSocketConnection socket, SocketEvent ev) {
			var objectId = ev.request?.id ?? 0;
			if (objectId == 0) {
				return;
			}

			var obj = NextUIPlugin.objectTable.SearchById(objectId);
			if (obj != null && obj.ObjectKind == ObjectKind.Player) {
				NextUIPlugin.xivCommon.Functions.Examine.OpenExamineWindow(obj.ObjectId);

				NextUISocket.Respond(socket, ev, new { success = true });
				return;
			}

			NextUISocket.Respond(socket, ev, new { success = false, message = "Invalid object" });
		}

		internal static void LeaveParty(IWebSocketConnection socket, SocketEvent ev) {
			if (!IsInParty()) {
				NextUISocket.Respond(socket, ev, new { success = false, message = "Not in party" });
				return;
			}

			NextUIPlugin.xivCommon.Functions.Chat.SendMessage("/leave");

			NextUISocket.Respond(socket, ev, new { success = true });
		}

		internal static void DisbandParty(IWebSocketConnection socket, SocketEvent ev) {
			if (!IsInParty()) {
				NextUISocket.Respond(socket, ev, new { success = false, message = "Not in party" });
				return;
			}

			if (IsPartyLeader()) {
				NextUISocket.Respond(socket, ev, new { success = false, message = "Not a party leader" });
				return;
			}

			NextUIPlugin.xivCommon.Functions.Chat.SendMessage("/partycmd breakup");

			NextUISocket.Respond(socket, ev, new { success = true });
		}

		internal static void PartyLeaderOperation(IWebSocketConnection socket, SocketEvent ev, string op) {
			var objectId = ev.request?.id ?? 0;
			if (objectId == 0) {
				return;
			}

			if (NextUIPlugin.partyList.Length == 0) {
				NextUISocket.Respond(socket, ev, new { success = false, message = "Not in party" });
				return;
			}

			if (IsPartyLeader()) {
				NextUISocket.Respond(socket, ev, new { success = false, message = "Not a party leader" });
				return;
			}

			var obj = NextUIPlugin.objectTable.SearchById(objectId);
			if (obj != null && obj is PlayerCharacter character) {
				var index = GetPlayerPartyIndex(character);
				if (index is > 0 and < 9) {
					NextUIPlugin.xivCommon.Functions.Chat.SendMessage($"/{op} <{index}>");
					NextUISocket.Respond(socket, ev, new { success = true });
					return;
				}
			}

			NextUISocket.Respond(socket, ev, new { success = false });
		}

		internal static void SendTell(IWebSocketConnection socket, SocketEvent ev) {
			var objectId = ev.request?.id ?? 0;
			if (objectId == 0) {
				return;
			}

			var obj = NextUIPlugin.objectTable.SearchById(objectId);

			if (obj != null && SendTellCommand != null && obj is PlayerCharacter player) {
				var raptureShellModulePointer = (IntPtr)uiModule->GetRaptureShellModule();
				var rap = raptureShellModulePointer.ToInt64();

				var gameObject = (FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject*)player.Address;
				SendTellCommand(rap, (char*)gameObject->Name, (ushort)player.HomeWorld.Id);

				NextUISocket.Respond(socket, ev, new { success = true });
				return;
			}

			NextUISocket.Respond(socket, ev, new { success = false, message = "Invalid object" });
		}


		internal static int? GetPlayerPartyIndex(PlayerCharacter character) {
			var agentHud = uiModule->GetAgentModule()->GetAgentHUD();
			var list = (HudPartyMember*)agentHud->PartyMemberList;

			for (var i = 0; i < (short)agentHud->PartyMemberCount; i++) {
				var partyMember = list[i];
				if (partyMember.ObjectId != character.ObjectId) {
					continue;
				}

				return i + 1;
			}

			return null;
		}

		internal static bool IsInParty() {
			return NextUIPlugin.partyList.Length > 0;
		}

		internal static bool IsPartyLeader() {
			var partyLeaderIndex = (int)NextUIPlugin.partyList.PartyLeaderIndex;
			var partyLeaderId = NextUIPlugin.partyList[partyLeaderIndex]?.ObjectId;
			return partyLeaderId != NextUIPlugin.clientState.LocalPlayer?.ObjectId;
		}
	}
}