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
		public static event Action<ushort, string, NetworkMessageDirection, object, uint?>? NetworkListener;

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

			var needsConversion = false;

			var sockets = NextUIPlugin.socketServer.GetEventSubscriptions(eventName);
			if (sockets != null && sockets.Count > 0) {
				needsConversion = true;
			}

			if (NetworkListener?.GetInvocationList().Length > 0) {
				needsConversion = true;
			}

			if (!needsConversion) {
				return;
			}

			var dyn = NetworkBinding.Convert(opcode, dataPtr);

			if (dyn == null) {
				return;
			}
			NetworkListener?.Invoke(opcode, eventName, direction, dyn, targetActorId);

			if (sockets == null) {
				return;
			}

			NextUISocket.BroadcastTo(new {
				@event = eventName,
				data = new {
					opcode,
					dir,
					targetActorId,
					data = dyn
				}
			}, sockets);
		}

		public void Dispose() {
			NextUIPlugin.gameNetwork.NetworkMessage -= GameNetworkOnNetworkMessage;
		}
	}
}