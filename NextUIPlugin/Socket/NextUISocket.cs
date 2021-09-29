using System;
using System.Diagnostics;
using System.Net;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Logging;
using Newtonsoft.Json;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace NextUIPlugin.Socket {
	public class SocketHandler : WebSocketBehavior {
		protected readonly ObjectTable objectTable;
		protected readonly TargetManager targetManager;

		public SocketHandler(
			ObjectTable objectTable,
			TargetManager targetManager
		) {
			this.objectTable = objectTable;
			this.targetManager = targetManager;
		}

		protected override void OnMessage(MessageEventArgs e) {
			try {
				SocketEvent? ev = JsonConvert.DeserializeObject<SocketEvent>(e.Data);
				if (ev == null) {
					return;
				}

				switch (ev.type) {
					case "setTarget":
						XivSetTarget(ev, "target");
						break;
					case "setFocus":
						XivSetTarget(ev, "focus");
						break;
					case "setMouseOver":
						XivSetTarget(ev, "mouseOver");
						break;
					case "setMouseOverEx":
						XivSetMouseOver(ev);
						break;
					case "clearMouseOverEx":
						XivSetMouseOver(ev, false);
						break;
					default:
						XivUnrecognizedCommand(ev);
						break;
				}
			}
			catch (Exception err) {
				Send(JsonConvert.SerializeObject(new SocketResponse {
					success = false, message = "Unrecognized data: " + e.Data + " " + err
				}));
			}
		}

		protected void XivUnrecognizedCommand(SocketEvent ev) {
			Send(JsonConvert.SerializeObject(new SocketResponse {
				guid = ev.guid, success = false, message = "Unrecognized command: " + ev.type
			}));
		}

		protected void XivSetTarget(SocketEvent ev, string type) {
			try {
				uint targetId = uint.Parse(ev.target);
				GameObject? target = objectTable.SearchById(targetId);
				if (target == null) {
					Send(JsonConvert.SerializeObject(new SocketResponse {
						guid = ev.guid, target = ev.target, success = false, message = "Invalid object ID"
					}));
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

				Send(JsonConvert.SerializeObject(new SocketResponse {
					guid = ev.guid, target = ev.target, success = true, message = ""
				}));
			}
			catch (Exception err) {
				Send(JsonConvert.SerializeObject(new SocketResponse {
					guid = ev.guid, target = ev.target, success = false, message = err.ToString()
				}));
			}
		}

		protected void XivSetMouseOver(SocketEvent ev, bool set = true) {
			try {
				uint targetId = uint.Parse(ev.target);
				GameObject? target = objectTable.SearchById(targetId);
				if (target == null) {
					Send(JsonConvert.SerializeObject(new SocketResponse {
						guid = ev.guid, target = ev.target, success = false, message = "Invalid object ID"
					}));
					return;
				}

				if (NextUIPlugin.mouseOverService != null) {
					NextUIPlugin.mouseOverService.Target = set ? target : null;
				}

				Send(JsonConvert.SerializeObject(new SocketResponse {
					guid = ev.guid, target = ev.target, success = true, message = "Mouse over set: " + target.Name.TextValue
				}));
			}
			catch (Exception err) {
				Send(JsonConvert.SerializeObject(new SocketResponse {
					guid = ev.guid, target = ev.target, success = false, message = err.ToString()
				}));
			}
		}
	}

	// ReSharper disable once InconsistentNaming
	public class NextUISocket {
		public int Port { get; set; }
		protected HttpServer? server;

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
			server = new HttpServer(IPAddress.Loopback, Port, false);
			server.AddWebSocketService("/ws", () => new SocketHandler(objectTable, targetManager));
			server.Start();
		}

		public void Stop() {
			try {
				server?.Stop();
			}
			catch (Exception e) {
				PluginLog.Log(e.ToString());
			}
		}

		public void Broadcast(string message) {
			server?.WebSocketServices.Broadcast(message);
		}
	}
}