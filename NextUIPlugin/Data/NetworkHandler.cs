using System;
using Dalamud.Game.Network;
using System.IO;
using System.Linq;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Logging;
using NextUIPlugin.NetworkStructures;
using NextUIPlugin.NetworkStructures.Server;
using NextUIPlugin.Socket;

namespace NextUIPlugin.Data {
	public class NetworkHandler : IDisposable {
#if DEBUG
		protected string logDir;
		protected uint[] ignoredOpcodes = new[] { 0x0296u, 0x0097u, 0x02E6u };

		public NetworkHandler() {
			NextUIPlugin.gameNetwork.NetworkMessage += GameNetworkOnNetworkMessage;

			logDir = Path.Combine(
				NextUIPlugin.pluginInterface.GetPluginConfigDirectory(),
				"NetworkLogs"
			);

			if (!Directory.Exists(logDir)) {
				Directory.CreateDirectory(logDir);
			}
		}
#else
		public NetworkHandler() {
			NextUIPlugin.gameNetwork.NetworkMessage += GameNetworkOnNetworkMessage;
		}
#endif

		protected unsafe void GameNetworkOnNetworkMessage(
			IntPtr dataPtr,
			ushort opcode,
			uint sourceActorId,
			uint targetActorId,
			NetworkMessageDirection direction
		) {
			string dir = direction == NetworkMessageDirection.ZoneDown ? "Down" : "Up";
#if DEBUG
			string opcName;
			try {
				opcName = ((ServerZoneIpcType)opcode).ToString("F");
			}
			catch (Exception) {
				opcName = "unk";
			}

			if (!ignoredOpcodes.Contains(opcode)) {
				//PluginLog.Log($"NETWORK 0x{opcode:X4}: {opcName} - {dir} - {targetActorId}");
			}

			var wholePtr = dataPtr - 0x20;
			using var stream = new UnmanagedMemoryStream((byte*)wholePtr.ToPointer(), 1544);
			using var reader = new BinaryReader(stream);
			var raw = reader.ReadBytes(1540);
			reader.Close();
			stream.Close();
			// PluginLog.Log(Convert.ToHexString(raw));
			string targName = "";
			var opcodeFileName = Path.Combine(logDir, dir + "_0x" + opcode.ToString("X4") + ".txt");
			if (File.Exists(opcodeFileName) && new FileInfo(opcodeFileName).Length > 10 * 1024 * 1024) {

			}
			else {
				File.AppendAllText(opcodeFileName,
					$"T: {targetActorId} {targName}" + Environment.NewLine +
					Convert.ToHexString(raw) + Environment.NewLine + Environment.NewLine
				);
			}
#endif

			NetworkBinding.binding.TryGetValue(opcode, out var eventName);
			if (eventName == null) {
				return;
			}

			var sockets = NextUIPlugin.socketServer.GetEventSubscriptions(eventName);
			if (sockets == null || sockets.Count == 0) {
				return;
			}

			var dyn = NetworkBinding.Convert(opcode, dataPtr, targetActorId);

			// if (opcode == (ushort)ServerZoneIpcType.ActorControl) {
			// 	var actorControl = (XivIpcActorControl)dyn;
			// 	if (actorControl.category == XivIpcActorControlCategory.OverTime) {
			// 		var timestamp = ""; // no idea yet
			// 		var id = dyn.targetActorId.ToString("X4");
			// 		var name = dyn.targetActorName;
			// 		var which = actorControl.param2 == 4 ? "HoT" : "DoT";
			// 		var effectId = "??";
			// 		var chara = (BattleChara)dyn.chara;
			// 		var currentHp = chara.CurrentHp;
			// 		var maxHp = chara.MaxHp;
			// 		var currentMp = chara.CurrentMp;
			// 		var maxMp = chara.MaxMp;
			// 		var x = chara.Position.X;
			// 		var y = chara.Position.Y;
			// 		var z = chara.Position.Z;
			// 		var damage = "??";
			// 		var heading = "??";
			// 		var ll = $"24|{timestamp}|{id}|{name}|{which}|{effectId}|{damage}|{currentHp}|{maxHp}|{currentMp}|{maxMp}|[?]|[?]|{x}|{y}|{z}|{heading}"
			// 		
			// 	}
			// }
			
			if (dyn != null) {
				NextUISocket.BroadcastTo(new {
					@event = eventName,
					data = new {
						opcode,
						dir,
						data = dyn
					}
				}, sockets);
			}
		}

		public void Dispose() {
			NextUIPlugin.gameNetwork.NetworkMessage -= GameNetworkOnNetworkMessage;
		}
	}
}