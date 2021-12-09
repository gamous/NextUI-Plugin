using System;
using System.Collections.Generic;
using System.Net;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.ClientState.Statuses;
using Dalamud.Logging;
using Fleck;
using Newtonsoft.Json;
// using BattleChara = FFXIVClientStructs.FFXIV.Client.Game.Character.BattleChara;
// ReSharper disable UnusedMember.Global

namespace NextUIPlugin.Socket {
	// ReSharper disable once InconsistentNaming
	public class NextUISocket : IDisposable {
		public int Port { get; set; }
		protected WebSocketServer? server;
		protected readonly List<IWebSocketConnection> sockets = new();

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
				socket.OnClose = () => { sockets.Remove(socket); };
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

		// ReSharper disable once UnusedMember.Global
		public void XivSetAcceptFocus(IWebSocketConnection socket, SocketEvent ev) {
			string msg = "AcceptFocus Changed " + ev.accept;
			foreach (var ov in NextUIPlugin.guiManager.overlays) {
				ov.acceptFocus = ev.accept;
			}

			socket.Send(JsonResponse(true, ev.guid, msg));
			PluginLog.Log(msg);
		}

		// ReSharper disable once UnusedMember.Global
		public void XivSetTarget(IWebSocketConnection socket, SocketEvent ev) {
			SetTarget(socket, ev, "target");
		}

		// ReSharper disable once UnusedMember.Global
		public void XivSetFocus(IWebSocketConnection socket, SocketEvent ev) {
			SetTarget(socket, ev, "focus");
		}

		// ReSharper disable once UnusedMember.Global
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

		// ReSharper disable once UnusedMember.Global
		public void XivClearMouseOverEx(IWebSocketConnection socket, SocketEvent ev) {
			SetMouseOverEx(socket, ev, false);
		}

		// ReSharper disable once UnusedMember.Global
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
				jobId = actor.ClassJob.Id,
				level = actor.Level,
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