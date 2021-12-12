using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.ClientState.Statuses;
using Dalamud.Logging;
using Fleck;
using Newtonsoft.Json;
using NextUIPlugin.Data;

// ReSharper disable UnusedMember.Global
// ReSharper disable InconsistentNaming
namespace NextUIPlugin.Socket {
	public class NextUISocket : IDisposable {
		public int Port { get; set; }
		protected WebSocketServer? server;
		protected readonly List<IWebSocketConnection> sockets = new();
		protected readonly Dictionary<string, List<IWebSocketConnection>> eventSubscriptions = new();
		public readonly Dictionary<BattleCharaCopy, List<IWebSocketConnection>> savedChara = new();

		protected readonly ObjectTable objectTable;
		protected readonly TargetManager targetManager;

		public NextUISocket(
			ObjectTable objectTable,
			TargetManager targetManager,
			int port
		) {
			this.objectTable = objectTable;
			this.targetManager = targetManager;
			Port = port;
		}

		public void Start() {
			server = new WebSocketServer("ws://" + IPAddress.Loopback + ":" + Port + "/ws");
			server.ListenerSocket.NoDelay = true;
			server.RestartAfterListenError = true;

			server.Start(socket => {
				socket.OnOpen = () => { sockets.Add(socket); };
				socket.OnClose = () => {
					sockets.Remove(socket);
					foreach (var (_, connections) in eventSubscriptions) {
						if (connections.Contains(socket)) {
							connections.Remove(socket);
						}
					}
				};
				socket.OnMessage = message => { OnMessage(message, socket); };
			});
		}

		protected void OnMessage(string data, IWebSocketConnection socket) {
			try {
				var ev = JsonConvert.DeserializeObject<SocketEvent>(data);
				if (ev == null) {
					return;
				}

				var methodName = string.Concat(
					"Xiv",
					ev.type[0].ToString().ToUpper(),
					ev.type.AsSpan(1)
				);

				var thisType = GetType();
				var method = thisType.GetMethod(methodName);
				if (method == null) {
					socket.Send(JsonResponse(false, "", "Unrecognized command: " + ev.type));
					return;
				}

				method.Invoke(this, new object[] { socket, ev });
			}
			catch (Exception err) {
				socket.Send(JsonResponse(false, "", "Unrecognized data: " + data + " " + err));
			}
		}

		#region Event Subscription

		public void XivSubscribeEvents(IWebSocketConnection socket, SocketEvent ev) {
			var events = ev.request.events;
			if (events.Length == 0) {
				socket.Send(JsonResponse(false, "", "Invalid events"));
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
		}

		public void XivUnsubscribeEvents(IWebSocketConnection socket, SocketEvent ev) {
			var events = ev.request.events;
			if (events.Length == 0) {
				socket.Send(JsonResponse(false, "", "Invalid events"));
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
		}

		public List<IWebSocketConnection>? GetEventSubscriptions(string eventName) {
			eventSubscriptions.TryGetValue(eventName, out var socketConnections);
			return socketConnections;
		}

		#endregion

		#region Actor Watch

		public void XivWatchActor(IWebSocketConnection socket, SocketEvent ev) {
			var objectId = ev.request.requestFor;
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
			var objectId = ev.request.requestFor;
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

		public void XivGetPlayer(IWebSocketConnection socket, SocketEvent ev) {
			var player = NextUIPlugin.clientState.LocalPlayer;
			if (player == null) {
				Send(socket, new {
					@event = "getPlayer",
					ev.guid,
					player = (object)null!
				});
				return;
			}

			var actor = (BattleChara)NextUIPlugin.objectTable.SearchById(player.ObjectId)!;
			Send(socket, new {
				@event = "getPlayer",
				ev.guid,
				player = ActorToObject(actor)
			});
		}

		public void XivGetActors(IWebSocketConnection socket, SocketEvent ev) {
			var actors = new List<object>();
			foreach (var actor in NextUIPlugin.objectTable) {
				if (actor is BattleChara chara) {
					actors.Add(ActorToObject(chara));
				}
			}

			Send(socket, new {
				@event = "actors",
				ev.guid,
				actors
			});
		}

		public void XivGetActor(IWebSocketConnection socket, SocketEvent ev) {
			var objectId = ev.request.requestFor;
			var actor = NextUIPlugin.objectTable.SearchById(objectId);

			if (actor != null && actor is BattleChara chara) {
				Send(socket, new {
					@event = "actor",
					ev.guid,
					actor = ActorToObject(chara)
				});
				return;
			}

			Send(socket, new {
				@event = "actor",
				ev.guid,
				actor = (object)null!
			});
		}

		public void XivGetActorStatuses(IWebSocketConnection socket, SocketEvent ev) {
			var objectId = ev.request.requestFor;
			var actor = NextUIPlugin.objectTable.SearchById(objectId);

			var statusList = new List<object>();
			if (actor != null && actor is BattleChara chara) {
				foreach (var status in chara.StatusList) {
					statusList.Add(StatusToObject(status));
				}
			}

			Send(socket, new {
				@event = "statuses",
				ev.guid,
				statuses = statusList
			});
		}

		public void XivSetAcceptFocus(IWebSocketConnection socket, SocketEvent ev) {
			string msg = "AcceptFocus Changed " + ev.accept;
			foreach (var ov in NextUIPlugin.guiManager.overlays) {
				ov.acceptFocus = ev.accept;
			}

			socket.Send(JsonResponse(true, ev.guid, msg));
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
				var target = objectTable.SearchById(targetId);
				if (target == null) {
					socket.Send(JsonResponse(false, ev.guid, "Invalid object ID", ev.target));
					return;
				}

				switch (type) {
					case "target":
						targetManager.SetTarget(target);
						break;
					case "focus":
						targetManager.SetFocusTarget(target);
						break;
					case "mouseOver":
						targetManager.SetMouseOverTarget(target);
						break;
				}

				socket.Send(JsonResponse(true, ev.guid, "", ev.target));
			}
			catch (Exception err) {
				socket.Send(JsonResponse(false, ev.guid, err.ToString(), ev.target));
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
				var targetId = uint.Parse(ev.target);
				var target = objectTable.SearchById(targetId);
				if (target == null) {
					socket.Send(JsonResponse(false, ev.guid, "Invalid object ID", ev.target));
					return;
				}

				if (NextUIPlugin.mouseOverService != null) {
					NextUIPlugin.mouseOverService.Target = set ? target : null;
				}

				socket.Send(JsonResponse(true, ev.guid, "Mouse over set: " + target.Name.TextValue, ev.target));
			}
			catch (Exception err) {
				socket.Send(JsonResponse(false, ev.guid, err.ToString(), ev.target));
			}
		}

		#region Data Helpers

		public static object ActorToObject(BattleChara actor) {
			return new {
				id = actor.ObjectId,
				name = actor.Name.TextValue,
				position = actor.Position,
				hp = actor.CurrentHp,
				hpMax = actor.MaxHp,
				mana = actor.CurrentMp,
				manaMax = actor.MaxMp,
				gp = actor.CurrentGp,
				gpMax = actor.MaxGp,
				cp = actor.CurrentCp,
				cpMax = actor.MaxCp,
				jobId = actor.ClassJob.Id,
				level = actor.Level,
				rotation = actor.Rotation,
				companyTag = actor.CompanyTag.TextValue,
			};
		}

		public static object StatusToObject(Status status) {
			return new {
				id = status.StatusId,
				name = status.GameData.Name.ToString(),
				remains = status.RemainingTime,
				sourceId = status.SourceID,
				stack = status.StackCount
			};
		}

		#endregion

		#region Internal methods

		public static string JsonResponse(bool success, string guid, string message, string target = "") {
			return JsonConvert.SerializeObject(new SocketResponse {
				guid = guid, success = success, message = message, target = target
			});
		}

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