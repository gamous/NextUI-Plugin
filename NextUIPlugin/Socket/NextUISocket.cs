using System;
using System.Globalization;
using System.Linq;
using System.Net;
using Dalamud.Game.ClientState.Actors.Types;
using Dalamud.Plugin;
using Newtonsoft.Json;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace NextUIPlugin.Socket {
	public class SocketHandler : WebSocketBehavior {
		protected readonly DalamudPluginInterface pluginInterface;

		public SocketHandler(DalamudPluginInterface pluginInterface) {
			this.pluginInterface = pluginInterface;
		}

		protected override void OnMessage(MessageEventArgs e) {
			try {
				SocketEvent ev = JsonConvert.DeserializeObject<SocketEvent>(e.Data);
				if (ev == null) {
					return;
				}

				switch (ev.type) {
					case "setTarget": 
						XivSetTarget(ev);
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

		protected void XivSetTarget(SocketEvent ev) {
			try {
				int targetId = int.Parse(ev.target);
				Actor target = pluginInterface.ClientState.Actors.First(a => a.ActorId == targetId);
				pluginInterface.ClientState.Targets.SetCurrentTarget(target);
				// pluginInterface.ClientState.
				// Marshal.WriteIntPtr(this.address.TargetManager, offset, actorAddress);
				Send(JsonConvert.SerializeObject(new SocketResponse {
					guid = ev.guid, target = ev.target, success = true
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
		protected HttpServer server;
		protected readonly DalamudPluginInterface pluginInterface;

		public NextUISocket(DalamudPluginInterface pluginInterface, int port) {
			this.pluginInterface = pluginInterface;
			Port = port;
		}

		public void Start() {
			server = new HttpServer(IPAddress.Loopback, Port, false);
			server.AddWebSocketService("/ws", () => new SocketHandler(pluginInterface));
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
			server.WebSocketServices.Broadcast(message);
		}
	}
}