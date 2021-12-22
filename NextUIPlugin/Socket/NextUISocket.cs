using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Logging;
using FFXIVClientStructs.FFXIV.Client.UI;
using Fleck;
using Lumina.Excel;
using Newtonsoft.Json;
using NextUIPlugin.Data;
using Action = Lumina.Excel.GeneratedSheets.Action;
using Status = Lumina.Excel.GeneratedSheets.Status;

// ReSharper disable UnusedMember.Global
// ReSharper disable InconsistentNaming
namespace NextUIPlugin.Socket {
	public unsafe class NextUISocket : IDisposable {
		protected static class Signatures {
			internal const string SendTellCommand = "E8 ?? ?? ?? ?? B3 01 48 8B 74 24 ??";
		}

		protected delegate void SendTellCommandDelegate(
			long raptureModulePointer, char* characterName, ushort homeWorldId
		);

		public int Port { get; set; }
		protected WebSocketServer? server;

		protected readonly List<IWebSocketConnection> sockets = new();
		protected readonly Dictionary<string, List<IWebSocketConnection>> eventSubscriptions = new();
		public readonly Dictionary<BattleCharaCopy, List<IWebSocketConnection>> savedChara = new();

		protected readonly ExcelSheet<Action>? actionSheet;
		protected readonly ExcelSheet<Status>? statusSheet;

		protected SendTellCommandDelegate? SendTellCommand { get; }
		protected readonly UIModule* uiModule;

		public NextUISocket(int port) {
			Port = port;
			actionSheet = NextUIPlugin.dataManager.GetExcelSheet<Action>();
			statusSheet = NextUIPlugin.dataManager.GetExcelSheet<Status>();
			uiModule = (UIModule*)NextUIPlugin.gameGui.GetUIModule();

			var sendTellPtr = NextUIPlugin.sigScanner.ScanText(Signatures.SendTellCommand);
			if (sendTellPtr != IntPtr.Zero) {
				SendTellCommand = Marshal.GetDelegateForFunctionPointer<SendTellCommandDelegate>(sendTellPtr);
			}
			else {
				PluginLog.Warning("Signature for Send Tell Not found");
			}
		}

		public void Start() {
			server = new WebSocketServer("ws://" + IPAddress.Loopback + ":" + Port + "/ws");
			server.ListenerSocket.NoDelay = true;
			server.RestartAfterListenError = true;

			server.Start(socket => {
				socket.OnOpen = () => {
					sockets.Add(socket);

					// Sending initial player if socket connected after player has logged in
					var player = NextUIPlugin.clientState.LocalPlayer;
					if (player != null) {
						socket.Send(JsonConvert.SerializeObject(new {
							@event = "playerLogin",
							data = DataConverter.ActorToObject(player)
						}));
					}
				};

				socket.OnClose = () => {
					sockets.Remove(socket);
					// Remove socket from event subscriptions once disconnected
					foreach (var (_, connections) in eventSubscriptions) {
						if (connections.Contains(socket)) {
							connections.Remove(socket);
						}
					}

					// Remove socket from chara watch once disconnected
					foreach (var (_, connections) in savedChara) {
						if (connections.Contains(socket)) {
							connections.Remove(socket);
						}
					}
				};

				socket.OnMessage = message => { OnMessage(message, socket); };
			});
		}

		protected void OnMessage(string data, IWebSocketConnection socket) {
			var ev = JsonConvert.DeserializeObject<SocketEvent>(data);
			if (ev == null) {
				// Do not respond to malformed requests
				return;
			}

			try {
				var methodName = string.Concat(
					"Xiv",
					ev.type[0].ToString().ToUpper(),
					ev.type.AsSpan(1)
				);

				var thisType = GetType();
				var method = thisType.GetMethod(methodName);
				if (method == null) {
					Respond(socket, ev, new { success = false, message = "Unrecognized command: " + ev.type });
					return;
				}

				method.Invoke(this, new object[] { socket, ev });
			}
			catch (Exception err) {
				Respond(socket, ev, new { success = false, message = $"Unrecognized data: {data} {err}" });
			}
		}

		#region ContextMenu Actions

		public void XivExamine(IWebSocketConnection socket, SocketEvent ev) {
			var objectId = ev.request?.requestFor ?? 0;
			if (objectId == 0) {
				return;
			}

			var obj = NextUIPlugin.objectTable.SearchById(objectId);
			if (obj != null && obj.ObjectKind == ObjectKind.Player) {
				NextUIPlugin.xivCommon.Functions.Examine.OpenExamineWindow(obj.ObjectId);

				Respond(socket, ev, new { success = true });
				return;
			}

			Respond(socket, ev, new { success = false, message = "Invalid object" });
		}

		public void XivLeaveParty(IWebSocketConnection socket, SocketEvent ev) {
			if (NextUIPlugin.partyList.Length == 0) {
				Respond(socket, ev, new { success = false, message = "Not in party" });
				return;
			}

			NextUIPlugin.xivCommon.Functions.Chat.SendMessage("/leave");

			Respond(socket, ev, new { success = true });
		}

		public void XivDisbandParty(IWebSocketConnection socket, SocketEvent ev) {
			if (NextUIPlugin.partyList.Length == 0) {
				Respond(socket, ev, new { success = false, message = "Not in party" });
				return;
			}

			var partyLeaderIndex = (int)NextUIPlugin.partyList.PartyLeaderIndex;
			var partyLeaderId = NextUIPlugin.partyList[partyLeaderIndex]?.ObjectId;
			if (partyLeaderId != NextUIPlugin.clientState.LocalPlayer?.ObjectId) {
				Respond(socket, ev, new { success = false, message = "Not a party leader" });
				return;
			}

			NextUIPlugin.xivCommon.Functions.Chat.SendMessage("/partycmd breakup");

			Respond(socket, ev, new { success = true });
		}

		/**
		 * Open emote window
		 * Ref: https://github.com/xivapi/ffxiv-datamining/blob/master/csv/MainCommand.csv
		 */
		public void XivShowEmoteWindow(IWebSocketConnection socket, SocketEvent ev) {
			NextUIPlugin.xivCommon.Functions.Chat.SendMessage("/emotelist");

			Respond(socket, ev, new { success = true });
		}

		/**
		 * Open signs window
		 */
		public void XivShowSignsWindow(IWebSocketConnection socket, SocketEvent ev) {
			NextUIPlugin.xivCommon.Functions.Chat.SendMessage("/enemysign");

			Respond(socket, ev, new { success = true });
		}

		public void XivSendTell(IWebSocketConnection socket, SocketEvent ev) {
			var objectId = ev.request?.requestFor ?? 0;
			if (objectId == 0) {
				return;
			}

			var obj = NextUIPlugin.objectTable.SearchById(objectId);

			if (obj != null && SendTellCommand != null && obj is PlayerCharacter player) {
				var raptureShellModulePointer = (IntPtr)uiModule->GetRaptureShellModule();
				var rap = raptureShellModulePointer.ToInt64();

				var gameObject = (FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject*)player.Address;
				SendTellCommand(rap, (char*)gameObject->Name, (ushort)player.HomeWorld.Id);

				Respond(socket, ev, new { success = true });
				return;
			}

			Respond(socket, ev, new { success = false, message = "Invalid object" });
		}

		#endregion

		#region Event Subscription

		public void XivSubscribeTo(IWebSocketConnection socket, SocketEvent ev) {
			var events = ev.request?.events;
			if (events == null || events.Length == 0) {
				Respond(socket, ev, new { success = false, message = "Invalid events" });
				return;
			}

			foreach (var eventName in events) {
				if (!eventSubscriptions.ContainsKey(eventName)) {
					eventSubscriptions.Add(eventName, new List<IWebSocketConnection>());
				}

				if (eventSubscriptions[eventName].Contains(socket)) {
					continue;
				}

				eventSubscriptions[eventName].Add(socket);
			}

			Respond(socket, ev, new { success = true, message = $"Subscribed to: {string.Join(", ", events)}" });
		}

		public void XivUnsubscribeFrom(IWebSocketConnection socket, SocketEvent ev) {
			var events = ev.request?.events;
			if (events == null || events.Length == 0) {
				Respond(socket, ev, new { success = false, message = "Invalid events" });
				return;
			}

			foreach (var eventName in events) {
				if (!eventSubscriptions.ContainsKey(eventName)) {
					continue;
				}

				if (!eventSubscriptions[eventName].Contains(socket)) {
					continue;
				}

				eventSubscriptions[eventName].Remove(socket);
			}

			Respond(socket, ev, new { success = true, message = $"Unsubscribed to: {string.Join(", ", events)}" });
		}

		public List<IWebSocketConnection>? GetEventSubscriptions(string eventName) {
			eventSubscriptions.TryGetValue(eventName, out var socketConnections);
			return socketConnections;
		}

		public bool HasEventSubscriptions(string eventName) {
			var socketConnections = GetEventSubscriptions(eventName);
			return socketConnections != null && socketConnections.Count > 0;
		}

		#endregion

		#region Actor Watch

		public void XivWatchActor(IWebSocketConnection socket, SocketEvent ev) {
			uint objectId = ev.request?.requestFor ?? 0;
			if (objectId == 0) {
				return;
			}

			var obj = NextUIPlugin.objectTable.SearchById(objectId);

			if (obj is not BattleChara chara) {
				Send(socket, new {
					@event = "watchActor",
					ev.guid,
					success = false
				});
				return;
			}

			var foundChara = savedChara.Keys.FirstOrDefault(c => c.ObjectId == objectId);
			if (foundChara == null) {
				// we did not found chara as key
				foundChara = BattleCharaCopy.FromBattleChara(chara);
				savedChara.Add(foundChara, new List<IWebSocketConnection> { socket });
				return;
			}

			// someone else is already watching
			if (!savedChara[foundChara].Contains(socket)) {
				savedChara[foundChara].Add(socket);
			}

			Send(socket, new {
				@event = "watchActor",
				ev.guid,
				success = true
			});
		}

		public void XivUnwatchActor(IWebSocketConnection socket, SocketEvent ev) {
			var objectId = ev.request?.requestFor ?? 0;
			if (objectId == 0) {
				return;
			}

			var foundChara = savedChara.Keys.FirstOrDefault(c => c.ObjectId == objectId);
			if (foundChara == null) {
				Send(socket, new {
					@event = "unwatchActor",
					ev.guid,
					success = false
				});
				return;
			}

			// This socket was indeed watching, removing it
			if (savedChara[foundChara].Contains(socket)) {
				savedChara[foundChara].Remove(socket);
			}

			// No one else watching, removing empty socket list
			if (savedChara[foundChara].Count == 0) {
				savedChara.Remove(foundChara);
			}

			Send(socket, new {
				@event = "watchActor",
				ev.guid,
				success = true
			});
		}

		#endregion

		public void XivGetParty(IWebSocketConnection socket, SocketEvent ev) {
			var currentParty = new List<object>();
			foreach (var partyMember in NextUIPlugin.partyList) {
				currentParty.Add(DataConverter.PartyMemberToObject(partyMember));
			}

			Respond(socket, ev, currentParty);
		}

		public void XivGetAction(IWebSocketConnection socket, SocketEvent ev) {
			var objectId = ev.request?.requestFor ?? 0;
			if (objectId == 0) {
				return;
			}

			var action = actionSheet?.GetRow(objectId);
			Respond(socket, ev, action == null ? null : DataConverter.ActionToObject(action));
		}

		public void XivGetStatus(IWebSocketConnection socket, SocketEvent ev) {
			var objectId = ev.request?.requestFor ?? 0;
			if (objectId == 0) {
				return;
			}

			var status = statusSheet?.GetRow(objectId);
			Respond(socket, ev, status == null ? null : DataConverter.LuminaStatusToObject(status));
		}

		public void XivGetPlayer(IWebSocketConnection socket, SocketEvent ev) {
			var player = NextUIPlugin.clientState.LocalPlayer;
			if (player == null) {
				Respond(socket, ev, null);
				return;
			}

			var actor = (BattleChara)NextUIPlugin.objectTable.SearchById(player.ObjectId)!;
			Respond(socket, ev, DataConverter.ActorToObject(actor));
		}

		public void XivGetActors(IWebSocketConnection socket, SocketEvent ev) {
			var actors = new List<object>();
			foreach (var actor in NextUIPlugin.objectTable) {
				if (actor is BattleChara chara) {
					actors.Add(DataConverter.ActorToObject(chara));
				}
			}

			Respond(socket, ev, actors);
		}

		public void XivGetActor(IWebSocketConnection socket, SocketEvent ev) {
			var objectId = ev.request?.requestFor ?? 0;
			if (objectId == 0) {
				return;
			}

			var actor = NextUIPlugin.objectTable.SearchById(objectId);

			if (actor != null && actor is BattleChara chara) {
				Respond(socket, ev, DataConverter.ActorToObject(chara));
				return;
			}

			Respond(socket, ev, null);
		}

		public void XivGetActorStatuses(IWebSocketConnection socket, SocketEvent ev) {
			var objectId = ev.request?.requestFor ?? 0;
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

			Respond(socket, ev, statusList);
		}

		public void XivSetAcceptFocus(IWebSocketConnection socket, SocketEvent ev) {
			string msg = "AcceptFocus Changed " + ev.accept;
			foreach (var ov in NextUIPlugin.guiManager.overlays) {
				ov.acceptFocus = ev.accept;
			}

			Respond(socket, ev, new { success = true, message = msg });
			PluginLog.Log(msg);
		}

		public void XivSetTarget(IWebSocketConnection socket, SocketEvent ev) {
			SetTarget(socket, ev, "target");
		}

		public void XivSetFocus(IWebSocketConnection socket, SocketEvent ev) {
			SetTarget(socket, ev, "focus");
		}

		public void XivSetMouseOver(IWebSocketConnection socket, SocketEvent ev) {
			SetTarget(socket, ev, "mouseOver");
		}

		public void SetTarget(IWebSocketConnection socket, SocketEvent ev, string type) {
			try {
				var targetId = uint.Parse(ev.target);
				var target = NextUIPlugin.objectTable.SearchById(targetId);
				if (target == null) {
					Respond(socket, ev, new { success = false, message = "Invalid object ID" });
					return;
				}

				switch (type) {
					case "target":
						NextUIPlugin.targetManager.SetTarget(target);
						break;
					case "focus":
						NextUIPlugin.targetManager.SetFocusTarget(target);
						break;
					case "mouseOver":
						NextUIPlugin.targetManager.SetMouseOverTarget(target);
						break;
				}

				Respond(socket, ev, new { success = true });
			}
			catch (Exception err) {
				Respond(socket, ev, new { success = false, message = err.ToString() });
			}
		}

		public void XivClearMouseOverEx(IWebSocketConnection socket, SocketEvent ev) {
			SetMouseOverEx(socket, ev, false);
		}

		public void XivSetMouseOverEx(IWebSocketConnection socket, SocketEvent ev) {
			SetMouseOverEx(socket, ev);
		}

		public void SetMouseOverEx(IWebSocketConnection socket, SocketEvent ev, bool set = true) {
			try {
				if (set) {
					var targetId = uint.Parse(ev.target);
					var target = NextUIPlugin.objectTable.SearchById(targetId);
					if (target == null) {
						Respond(socket, ev, new { success = false, message = "Invalid object ID" });
						return;
					}

					NextUIPlugin.mouseOverService.target = target;
				}
				else {
					NextUIPlugin.mouseOverService.target = null;
				}

				Respond(socket, ev, new { success = true });
			}
			catch (Exception err) {
				Respond(socket, ev, new { success = false, message = err.ToString() });
			}
		}

		#region Internal methods

		public void Broadcast(string message) {
			sockets.ForEach(s => s.Send(message));
		}

		public void Broadcast(object message) {
			Broadcast(JsonConvert.SerializeObject(message));
		}

		public void BroadcastTo(object data, List<IWebSocketConnection> socketConnections) {
			foreach (var connection in socketConnections) {
				connection.Send(JsonConvert.SerializeObject(data));
			}
		}

		public void Send(IWebSocketConnection socket, object message) {
			socket.Send(JsonConvert.SerializeObject(message));
		}

		public void Respond(IWebSocketConnection socket, SocketEvent ev, object? data) {
			Send(socket, new {
				@event = ev.type,
				ev.guid,
				data
			});
		}

		public void Stop() {
			try {
				server?.Dispose();
			}
			catch (Exception e) {
				PluginLog.Log(e.ToString());
			}
		}

		public void Dispose() {
			sockets.ForEach(s => s.Close());
			server?.Dispose();
		}

		#endregion
	}
}