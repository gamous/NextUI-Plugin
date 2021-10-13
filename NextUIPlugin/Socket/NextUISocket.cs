using System;
using System.Collections.Generic;
using System.Net;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Logging;
using Fleck;
using Newtonsoft.Json;

namespace NextUIPlugin.Socket {
	// ReSharper disable once InconsistentNaming
	public class NextUISocket: IDisposable {
		public int Port { get; set; }
		protected WebSocketServer? server;
		List<IWebSocketConnection> sockets = new();

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
				SocketEvent? ev = JsonConvert.DeserializeObject<SocketEvent>(data);
				if (ev == null) {
					return;
				}

				switch (ev.type) {
					case "setTarget":
						XivSetTarget(socket, ev, "target");
						break;
					case "setFocus":
						XivSetTarget(socket, ev, "focus");
						break;
					case "setMouseOver":
						XivSetTarget(socket, ev, "mouseOver");
						break;
					case "setMouseOverEx":
						XivSetMouseOver(socket, ev);
						break;
					case "clearMouseOverEx":
						XivSetMouseOver(socket, ev, false);
						break;
					case "setAcceptFocus":
						XivSetAcceptFocus(socket, ev);
						break;
					default:
						socket.Send(JsonResponse(false, "", "Unrecognized command: " + ev.type));
						break;
				}
			}
			catch (Exception err) {
				socket.Send(JsonResponse(false, "", "Unrecognized data: " + data + " " + err));
			}
		}

		protected void XivSetAcceptFocus(IWebSocketConnection socket, SocketEvent ev) {
			NextUIPlugin.overlayManager?.SetAcceptFocus(ev.accept);
			string msg = "AcceptFocus Changed " + ev.accept;
			socket.Send(JsonResponse(true, ev.guid, msg, ""));
			PluginLog.Log(msg);
		}

		protected void XivSetTarget(IWebSocketConnection socket, SocketEvent ev, string type) {
			try {
				uint targetId = uint.Parse(ev.target);
				GameObject? target = objectTable.SearchById(targetId);
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

		protected void XivSetMouseOver(IWebSocketConnection socket, SocketEvent ev, bool set = true) {
			try {
				uint targetId = uint.Parse(ev.target);
				GameObject? target = objectTable.SearchById(targetId);
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

		public string JsonResponse(bool success, string guid, string message, string target = "") {
			return JsonConvert.SerializeObject(new SocketResponse {
				guid = guid, success = success, message = message, target = target
			});
		}

		public void Broadcast(string message) {
			sockets.ForEach(s => s.Send(message));
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
	}
}