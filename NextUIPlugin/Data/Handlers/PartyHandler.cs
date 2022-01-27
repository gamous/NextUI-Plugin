using System;
using System.Collections.Generic;
using Dalamud.Memory;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using Fleck;
using NextUIPlugin.Socket;

namespace NextUIPlugin.Data.Handlers {
	public static unsafe class PartyHandler {
		internal static List<uint> party = new();
		internal static uint partyLeader;

		internal static UIModule* uiModule;
		internal static AgentHUD* agentHud;

		#region Commands

		public static void RegisterCommands() {
			uiModule = (UIModule*)NextUIPlugin.gameGui.GetUIModule();
			agentHud = uiModule->GetAgentModule()->GetAgentHUD();

			NextUISocket.RegisterCommand("getParty", GetParty);
		}

		internal static void GetParty(IWebSocketConnection socket, SocketEvent ev) {
			var currentParty = GetPartyList();

			var newPartyLeader = NextUIPlugin.partyList.PartyLeaderIndex;
			NextUISocket.Respond(socket, ev, new { currentParty, partyLeader = newPartyLeader });
		}

		#endregion

		internal static List<uint> GetPartyIds() {
			var list = (HudPartyMember*)agentHud->PartyMemberList;

			var output = new List<uint>();
			for (var i = 0; i < (short)agentHud->PartyMemberCount; i++) {
				var partyMember = list[i];
				output.Add(partyMember.ObjectId);
			}

			return output;
		}

		internal static List<object> GetPartyList() {
			var currentParty = new List<object>();
			var list = (HudPartyMember*)agentHud->PartyMemberList;
			for (var i = 0; i < (short)agentHud->PartyMemberCount; i++) {
				var partyMemberRaw = list[i];

				currentParty.Add(BattleCharaRawToObject(
					partyMemberRaw
				));
			}

			return currentParty;
		}

		public static void Watch() {
			var sockets = NextUIPlugin.socketServer.GetEventSubscriptions("partyChanged");
			if (sockets == null || sockets.Count == 0) {
				return;
			}

			var currentParty = GetPartyIds();

			if (party.Count != currentParty.Count || partyLeader != NextUIPlugin.partyList.PartyLeaderIndex) {
				partyLeader = NextUIPlugin.partyList.PartyLeaderIndex;
				party = currentParty;
				BroadcastPartyChanged(sockets, partyLeader);
				return;
			}

			var eq = DataHandler.CompareList(party, currentParty);
			if (eq) {
				return;
			}

			BroadcastPartyChanged(sockets, NextUIPlugin.partyList.PartyLeaderIndex);
			party = currentParty;
		}

		internal static void BroadcastPartyChanged(List<IWebSocketConnection> sockets, uint newPartyLeader) {
			var currentParty = GetPartyList();

			NextUISocket.BroadcastTo(new {
				@event = "partyChanged",
				data = new { currentParty, newPartyLeader },
			}, sockets);
		}

		internal static object BattleCharaRawToObject(HudPartyMember partyMember) {
			var name = MemoryHelper.ReadSeString((IntPtr)partyMember.Name, 64);
			var objectId = partyMember.ObjectId;
			if (partyMember.ObjectId == 0 || partyMember.Object == (void*)0) {
				return new {
					id = objectId,
					name = name.TextValue,
					contentId = partyMember.ContentId.ToString("X16"),
					provisional = true
				};
			}

			var chara = partyMember.Object->Character;
			var companyTag = MemoryHelper.ReadSeString((IntPtr)chara.FreeCompanyTag, 6);
			var gameObject = (GameObject*)partyMember.Object;
			return new {
				id = objectId,
				name = name.TextValue,
				nameId = chara.NameID,
				contentId = partyMember.ContentId.ToString("X16"),
				position = new { x = gameObject->Position.X, y = gameObject->Position.Y, z = gameObject->Position.Z },
				hp = chara.Health,
				hpMax = chara.MaxHealth,
				mana = chara.Mana,
				manaMax = chara.MaxMana,
				gp = chara.GatheringPoints,
				gpMax = chara.MaxGatheringPoints,
				cp = chara.CraftingPoints,
				cpMax = chara.MaxCraftingPoints,
				jobId = chara.ClassJob,
				level = chara.Level,
				rotation = gameObject->Rotation,
				companyTag = companyTag.TextValue,
			};
		}
	}
}