using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Dalamud.Logging;
using Fleck;
using Lumina.Excel;
using Newtonsoft.Json;
using NextUIPlugin.Data;
using NextUIPlugin.Data.Handlers;
using BattleChara = Dalamud.Game.ClientState.Objects.Types.BattleChara;
using Status = Lumina.Excel.GeneratedSheets.Status;

// ReSharper disable UnusedMember.Global
// ReSharper disable InconsistentNaming
namespace NextUIPlugin.Socket {
	public class NextUISocket : IDisposable {
		public int Port { get; set; }
		protected WebSocketServer? server;

		protected readonly List<IWebSocketConnection> sockets = new();
		protected readonly Dictionary<string, List<IWebSocketConnection>> eventSubscriptions = new();


		protected static Dictionary<string, Action<IWebSocketConnection, SocketEvent>> actions = new();

		protected bool running;

		public NextUISocket(int port) {
			Port = port;
		}

		public void Start() {
			server = new WebSocketServer("ws://" + IPAddress.Loopback + ":" + Port + "/ws");
			server.ListenerSocket.NoDelay = true;
			server.RestartAfterListenError = true;

			// Register commands before starting server
			TargetHandler.RegisterCommands();
			ContextHandler.RegisterCommands();
			PartyHandler.RegisterCommands();
			EnmityListHandler.RegisterCommands();
			ActorHandler.RegisterCommands();
			ActionHandler.RegisterCommands();
			StatusHandler.RegisterCommands();
			MouseOverHandler.RegisterCommands();

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

					ActorHandler.RemoveSocket(socket);
				};

				socket.OnMessage = message => { OnMessage(message, socket); };
			});
			running = true;
		}

		public bool IsRunning() {
			return running;
		}

		public static void RegisterCommand(string commandName, Action<IWebSocketConnection, SocketEvent> command) {
			if (actions.ContainsKey(commandName)) {
				throw new Exception($"Socket Command already registered: {commandName}");
			}

			actions.Add(commandName, command);
		}

		protected void OnMessage(string data, IWebSocketConnection socket) {
			var ev = JsonConvert.DeserializeObject<SocketEvent>(data);
			if (ev == null) {
				// Do not respond to malformed requests
				return;
			}

			try {
				if (actions.TryGetValue(ev.type, out var action)) {
					action(socket, ev);
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
					Respond(socket, ev, new { success = false, message = "Unrecognized command: " + ev.type });
					return;
				}

				method.Invoke(this, new object[] { socket, ev });
			}
			catch (Exception err) {
				Respond(socket, ev, new { success = false, message = $"Unrecognized data: {data} {err}" });
			}
		}


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

			Respond(socket, ev, new { success = true, message = $"Unsubscribed from: {string.Join(", ", events)}" });
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

		public void XivGetPlayer(IWebSocketConnection socket, SocketEvent ev) {
			var player = NextUIPlugin.clientState.LocalPlayer;
			if (player == null) {
				Respond(socket, ev, null);
				return;
			}

			var actor = (BattleChara)NextUIPlugin.objectTable.SearchById(player.ObjectId)!;
			Respond(socket, ev, DataConverter.ActorToObject(actor));
		}

		public void XivSetAcceptFocus(IWebSocketConnection socket, SocketEvent ev) {
			string msg = "AcceptFocus Changed " + ev.accept;
			foreach (var ov in NextUIPlugin.guiManager.overlays) {
				ov.acceptFocus = ev.accept;
			}

			Respond(socket, ev, new { success = true, message = msg });
			PluginLog.Log(msg);
		}

		#region Internal methods

		public void Broadcast(string message) {
			sockets.ForEach(s => s.Send(message));
		}

		public void Broadcast(object message) {
			Broadcast(JsonConvert.SerializeObject(message));
		}

		public static void BroadcastTo(object data, List<IWebSocketConnection> socketConnections) {
			foreach (var connection in socketConnections) {
				connection.Send(JsonConvert.SerializeObject(data));
			}
		}

		public static void Send(IWebSocketConnection socket, object message) {
			socket.Send(JsonConvert.SerializeObject(message));
		}

		public static void Respond(IWebSocketConnection socket, SocketEvent ev, object? data) {
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
			sockets.ToList().ForEach(s => s.Close());
			server?.Dispose();
		}

		#endregion
	}
}