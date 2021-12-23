/*
Code regarding Enemy list borrowed from:

Copyright(c) 2021 DevlUI (https://github.com/delvui/delvui)
Modifications Copyright(c) 2021 NextUI

Full License: https://github.com/DelvUI/DelvUI/blob/main/LICENSE
*/

using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Game;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Memory;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Fleck;
using NextUIPlugin.Data.Handlers;
using SpellAction = Lumina.Excel.GeneratedSheets.Action;

namespace NextUIPlugin.Data {
	public unsafe class DataHandler : IDisposable {
		protected const int EnemyListInfoIndex = 19;
		protected const int EnemyListNamesIndex = 17;

		protected readonly Dictionary<string, (uint?, string?)> targets = new();
		protected List<uint> party = new();
		protected uint partyLeader;

		protected List<uint> enmity = new();

		protected RaptureAtkModule* raptureAtkModule;
		protected AtkUnitBase* enemyList;

		public DataHandler() {
			var uiModule = (UIModule*)NextUIPlugin.gameGui.GetUIModule();
			raptureAtkModule = uiModule->GetRaptureAtkModule();
			enemyList = (AtkUnitBase*)NextUIPlugin.gameGui.GetAddonByName("_EnemyList", 1);

			NextUIPlugin.framework.Update += FrameworkOnUpdate;
			NextUIPlugin.chatGui.ChatMessage += ChatGuiOnChatMessage;
			NextUIPlugin.clientState.Login += ClientStateOnLogin;
			NextUIPlugin.clientState.Logout += ClientStateOnLogout;
			NextUIPlugin.clientState.TerritoryChanged += ClientStateOnTerritoryChanged;
		}

		protected void FrameworkOnUpdate(Framework framework) {
			WatchTargets();
			WatchBattleChara();
			WatchParty();
			WatchEnmityList();
			WatchEnmityList();
			UiVisibility.Watch();
		}

		#region Enmity

		protected void WatchEnmityList() {
			var sockets = NextUIPlugin.socketServer.GetEventSubscriptions("enmityListChanged");
			if (sockets == null || sockets.Count == 0) {
				return;
			}

			var enemyArray = GetEnemyArray();
			if (enemyArray == null) {
				return;
			}

			var enemyCount = GetEnemyCount(enemyArray);

			var currentEnmity = GetEnemyObjectList(enemyArray, enemyCount);
			if (currentEnmity.Count != enmity.Count || !CompareList(enmity, currentEnmity)) {
				enmity = currentEnmity;
				BroadcastEnmityListChanged(sockets, enmity);
			}

			// For now we dont use that
			// var letter = GetEnemyLetterForIndex(i);
			// var enmityLevel = GetEnmityLevelForIndex(i);
		}

		protected NumberArrayData* GetEnemyArray() {
			return raptureAtkModule->AtkModule.AtkArrayDataHolder.NumberArrays[EnemyListInfoIndex];
		}

		protected int GetEnemyCount(NumberArrayData* numberArrayData) {
			return numberArrayData->AtkArrayData.Size < 2 ? 0 : numberArrayData->IntArray[1];
		}

		protected List<uint> GetEnemyObjectList(NumberArrayData* numberArrayData, int enemyCount) {
			var output = new List<uint>();
			for (var i = 0; i < enemyCount; i++) {
				var index = 8 + (i * 6);
				if (numberArrayData->AtkArrayData.Size <= index) {
					break;
				}

				var objectId = (uint)numberArrayData->IntArray[index];
				output.Add(objectId);
			}

			return output;
		}

		protected string? GetEnemyLetterForIndex(int index) {
			if (raptureAtkModule == null ||
			    raptureAtkModule->AtkModule.AtkArrayDataHolder.StringArrayCount <= EnemyListNamesIndex) {
				return null;
			}

			var stringArrayData = raptureAtkModule->AtkModule.AtkArrayDataHolder.StringArrays[EnemyListNamesIndex];

			var i = index * 2;
			if (stringArrayData->AtkArrayData.Size <= i) {
				return null;
			}

			string name = MemoryHelper
				.ReadSeStringNullTerminated(new IntPtr(stringArrayData->StringArray[i]))
				.ToString();

			if (name.Length == 0) {
				return null;
			}

			var letterSymbol = name[0];
			var letter = (char)(65 + letterSymbol - 57457);
			return letter.ToString();
		}

		protected int GetEnmityLevelForIndex(int index) {
			// gets enmity level by checking texture in enemy list addon
			if (enemyList == null || enemyList->RootNode == null) {
				return 0;
			}

			var id = index == 0 ? 2 : 20000 + index; // makes no sense but it is what it is (blame SE)
			var node = enemyList->GetNodeById((uint)id);
			if (node == null || node->GetComponent() == null) {
				return 0;
			}

			var imageNode = (AtkImageNode*)node->GetComponent()->UldManager.SearchNodeById(13);

			return imageNode == null ? 0 : Math.Min(4, imageNode->PartId + 1);
		}

		#endregion

		protected void WatchParty() {
			var sockets = NextUIPlugin.socketServer.GetEventSubscriptions("partyChanged");
			if (sockets == null || sockets.Count == 0) {
				return;
			}

			var currentParty = NextUIPlugin.partyList
				.Select(partyMember => partyMember.ObjectId).ToList();

			if (party.Count != currentParty.Count || partyLeader != NextUIPlugin.partyList.PartyLeaderIndex) {
				partyLeader = NextUIPlugin.partyList.PartyLeaderIndex;
				party = currentParty;
				BroadcastPartyChanged(sockets, partyLeader);
				return;
			}

			var eq = CompareList(party, currentParty);
			if (eq) {
				return;
			}

			BroadcastPartyChanged(sockets, NextUIPlugin.partyList.PartyLeaderIndex);
			party = currentParty;
		}

		protected static bool CompareList<T>(List<T> a, List<T> b) {
			var firstNotSecond = a.Except(b).ToList();
			var secondNotFirst = b.Except(a).ToList();

			return !firstNotSecond.Any() && !secondNotFirst.Any();
		}

		/**
		 * Watch common differences on battle actors and report any changes to it (ignores position and rotation)
		 */
		protected void WatchBattleChara() {
			// Do not process if player is not loaded
			if (NextUIPlugin.clientState.LocalPlayer == null) {
				return;
			}

			foreach (var (charaCopy, socketList) in NextUIPlugin.socketServer.savedChara) {
				var obj = NextUIPlugin.objectTable.SearchById(charaCopy.ObjectId);
				if (obj is BattleChara chara) {
					if (charaCopy.HasChanged(chara, false, false)) {
						charaCopy.UpdateFromBattleChara(chara);
						BroadcastActorChanged(charaCopy.ObjectId, false, socketList, chara);
					}
				}
				else {
					BroadcastActorChanged(charaCopy.ObjectId, true, socketList);
					// Chara no longer exists in game memory, no need to watch it
					NextUIPlugin.socketServer.savedChara.Remove(charaCopy);
				}
			}
		}

		protected void WatchTargets() {
			var sockets = NextUIPlugin.socketServer.GetEventSubscriptions("targetChanged");
			if (sockets == null || sockets.Count == 0) {
				return;
			}

			Dictionary<string, GameObject?> currentTargets = new() {
				{ "target", NextUIPlugin.targetManager.Target },
				{ "targetOfTarget", NextUIPlugin.targetManager.Target?.TargetObject },
				{ "hover", NextUIPlugin.targetManager.MouseOverTarget },
				{ "focus", NextUIPlugin.targetManager.FocusTarget },
			};

			foreach ((string key, var value) in currentTargets) {
				if (!targets.ContainsKey(key)) {
					targets[key] = (null, null);
				}

				var (targetId, targetName) = targets[key];
				if (value != null) {
					if (targetId != value.ObjectId || targetName != value.Name.TextValue) {
						targets[key] = (value.ObjectId, value.Name.TextValue);
						BroadcastTargetChanged(sockets, key, value);
					}
				}
				else {
					if (targetId != null) {
						targets[key] = (null, null);
						BroadcastTargetChanged(sockets, key);
					}
				}
			}
		}

		#region Broadcasters

		protected static void BroadcastTargetChanged(
			List<IWebSocketConnection> sockets,
			string targetType,
			GameObject? actor = null
		) {
			NextUIPlugin.socketServer.BroadcastTo(new {
				@event = "targetChanged",
				data = new {
					targetType,
					actorId = actor?.ObjectId,
					actorName = actor?.Name.TextValue,
					actor = actor == null
						? null
						: (actor is BattleChara chara ? DataConverter.ActorToObject(chara) : null),
				}
			}, sockets);
		}

		protected static void BroadcastPartyChanged(List<IWebSocketConnection> sockets, uint partyLeader) {
			var currentParty = new List<object>();
			foreach (var partyMember in NextUIPlugin.partyList) {
				currentParty.Add(DataConverter.PartyMemberToObject(partyMember));
			}

			NextUIPlugin.socketServer.BroadcastTo(new {
				@event = "partyChanged",
				data = new { currentParty, partyLeader },
			}, sockets);
		}

		protected static void BroadcastEnmityListChanged(List<IWebSocketConnection> sockets, List<uint> enmity) {
			var currentEnmity = new List<object>();
			foreach (var enemyId in enmity) {
				var enemy = NextUIPlugin.objectTable.SearchById(enemyId);
				if (enemy == null || enemy is not BattleChara chara) {
					continue;
				}

				currentEnmity.Add(DataConverter.ActorToObject(chara));
			}

			NextUIPlugin.socketServer.BroadcastTo(new {
				@event = "enmityListChanged",
				data = currentEnmity,
			}, sockets);
		}

		protected static void BroadcastActorChanged(
			uint actorId,
			bool removed,
			List<IWebSocketConnection> sockets,
			BattleChara? chara = null
		) {
			NextUIPlugin.socketServer.BroadcastTo(new {
				@event = "actorChanged",
				data = new {
					actorId,
					removed,
					actor = chara != null ? DataConverter.ActorToObject(chara) : null
				}
			}, sockets);
		}

		protected void ClientStateOnTerritoryChanged(object? sender, ushort e) {
			NextUIPlugin.socketServer.Broadcast(new {
				@event = "zoneChanged",
				data = e
			});
		}

		protected void ClientStateOnLogout(object? sender, EventArgs e) {
			NextUIPlugin.socketServer.Broadcast(new {
				@event = "playerLogout",
			});
		}

		protected void ClientStateOnLogin(object? sender, EventArgs e) {
			var player = NextUIPlugin.clientState.LocalPlayer;
			NextUIPlugin.socketServer.Broadcast(new {
				@event = "playerLogin",
				data = player != null ? DataConverter.ActorToObject(player) : null
			});

			UiVisibility.Initialize();
		}

		protected void ChatGuiOnChatMessage(
			XivChatType type,
			uint senderId,
			ref SeString sender,
			ref SeString message,
			ref bool isHandled
		) {
			var sockets = NextUIPlugin.socketServer.GetEventSubscriptions("chatMessage");
			if (sockets != null && sockets.Count > 0) {
				NextUIPlugin.socketServer.BroadcastTo(new {
					@event = "chatMessage",
					data = new {
						typeId = (ushort)type,
						senderId,
						sender = sender.TextValue,
						message = message.TextValue,
					}
				}, sockets);
			}
		}

		#endregion

		public void Dispose() {
			NextUIPlugin.framework.Update -= FrameworkOnUpdate;
			NextUIPlugin.chatGui.ChatMessage -= ChatGuiOnChatMessage;
			NextUIPlugin.clientState.Login -= ClientStateOnLogin;
			NextUIPlugin.clientState.Logout -= ClientStateOnLogout;
			NextUIPlugin.clientState.TerritoryChanged -= ClientStateOnTerritoryChanged;
		}
	}
}