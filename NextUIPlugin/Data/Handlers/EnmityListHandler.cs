/*
Code regarding Enemy list borrowed from:

Copyright(c) 2021 DevlUI (https://github.com/delvui/delvui)
Modifications Copyright(c) 2021 NextUI

Full License: https://github.com/DelvUI/DelvUI/blob/main/LICENSE
*/

using System;
using System.Collections.Generic;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Memory;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Fleck;
using NextUIPlugin.Socket;

namespace NextUIPlugin.Data.Handlers {
	public static unsafe class EnmityListHandler {
		internal const int EnemyListInfoIndex = 19;
		internal const int EnemyListNamesIndex = 17;


		internal static List<uint> enmity = new();

		internal static RaptureAtkModule* raptureAtkModule;
		internal static AtkUnitBase* enemyList;
		
		#region Commands

		public static void RegisterCommands() {
			NextUISocket.RegisterCommand("getEnmityList", GetEnmityList);
		}

		internal static void GetEnmityList(IWebSocketConnection socket, SocketEvent ev) {
			var enemyArray = GetEnemyArray();
			if (enemyArray == null) {
				return;
			}

			var enemyCount = GetEnemyCount(enemyArray);
			var currentEnmity = GetEnemyObjectList(enemyArray, enemyCount);

			var enmityList = new List<object>();
			foreach (var enemyId in currentEnmity) {
				var enemy = NextUIPlugin.objectTable.SearchById(enemyId);
				if (enemy == null || enemy is not BattleChara chara) {
					continue;
				}

				enmityList.Add(DataConverter.ActorToObject(chara));
			}

			NextUISocket.Respond(socket, ev, enmityList);
		}

		#endregion

		public static void Initialize() {
			var uiModule = (UIModule*)NextUIPlugin.gameGui.GetUIModule();
			raptureAtkModule = uiModule->GetRaptureAtkModule();
			enemyList = (AtkUnitBase*)NextUIPlugin.gameGui.GetAddonByName("_EnemyList", 1);
		}
		
		public static void Watch() {
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
			if (currentEnmity.Count != enmity.Count || !DataHandler.CompareList(enmity, currentEnmity)) {
				enmity = currentEnmity;
				BroadcastEnmityListChanged(sockets, enmity);
			}

			// For now we dont use that
			// var letter = GetEnemyLetterForIndex(i);
			// var enmityLevel = GetEnmityLevelForIndex(i);
		}
		
		internal static NumberArrayData* GetEnemyArray() {
			return raptureAtkModule->AtkModule.AtkArrayDataHolder.NumberArrays[EnemyListInfoIndex];
		}

		internal static int GetEnemyCount(NumberArrayData* numberArrayData) {
			return numberArrayData->AtkArrayData.Size < 2 ? 0 : numberArrayData->IntArray[1];
		}

		internal static List<uint> GetEnemyObjectList(NumberArrayData* numberArrayData, int enemyCount) {
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

		internal static string? GetEnemyLetterForIndex(int index) {
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

		internal static int GetEnmityLevelForIndex(int index) {
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

		internal static void BroadcastEnmityListChanged(List<IWebSocketConnection> sockets, List<uint> newEnmity) {
			var currentEnmity = new List<object>();
			foreach (var enemyId in newEnmity) {
				var enemy = NextUIPlugin.objectTable.SearchById(enemyId);
				if (enemy == null || enemy is not BattleChara chara) {
					continue;
				}

				currentEnmity.Add(DataConverter.ActorToObject(chara));
			}

			NextUISocket.BroadcastTo(new {
				@event = "enmityListChanged",
				data = currentEnmity,
			}, sockets);
		}
	}
}