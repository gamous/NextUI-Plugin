using System;
using Dalamud.Game.Network;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Logging;
using NextUIPlugin.NetworkStructures;
using System.IO;
using System.Runtime.InteropServices;
using Newtonsoft.Json;
using NextUIPlugin.NetworkStructures.Client;

namespace NextUIPlugin.Data {
	public class NetworkHandler : IDisposable {
		protected string logDir;

		public NetworkHandler() {
			NextUIPlugin.gameNetwork.NetworkMessage += GameNetworkOnNetworkMessage;
#if DEBUG
			logDir = Path.Combine(
				NextUIPlugin.pluginInterface.GetPluginConfigDirectory(),
				"NetworkLogs"
			);

			if (!Directory.Exists(logDir)) {
				Directory.CreateDirectory(logDir);
			}
#endif
		}

		protected unsafe void GameNetworkOnNetworkMessage(
			IntPtr dataPtr,
			ushort opcode,
			uint sourceActorId,
			uint targetActorId,
			NetworkMessageDirection direction
		) {
			string dir = direction == NetworkMessageDirection.ZoneDown ? "Down" : "Up";
#if DEBUG
			// PluginLog.Log("NETWORK 0x" + opcode.ToString("X4") + " " + dir);
			var wholePtr = dataPtr - 0x20;

			using var stream = new UnmanagedMemoryStream((byte*)wholePtr.ToPointer(), 1544);
			using var reader = new BinaryReader(stream);
			var raw = reader.ReadBytes(1540);
			reader.Close();
			stream.Close();
			// PluginLog.Log(Convert.ToHexString(raw));

			var opcodeFileName = Path.Combine(logDir, dir + "_0x" + opcode.ToString("X4") + ".txt");
			if (File.Exists(opcodeFileName) && new FileInfo(opcodeFileName).Length > 10 * 1024 * 1024) {

			}
			else {
				File.AppendAllText(opcodeFileName, Convert.ToHexString(raw) + Environment.NewLine + Environment.NewLine);
			}
#endif

			// switch (opcode) {
			// 	case (ushort)ClientZoneIpcType.ChatHandler:
			// 		var dec = Marshal.PtrToStructure<XIVIpcChatHandler>(dataPtr);
			// 		// var serialized = JsonConvert.SerializeObject(dec);
			// 		NextUIPlugin.socketServer.Broadcast(new {
			// 			@event = "chatMessage",
			// 			opcode,
			// 			dir,
			// 			data = dec
			// 		});
			// 		break;
			// }

			// using UnmanagedMemoryStream stream = new((byte*)dataPtr.ToPointer(), 1544);
			// using BinaryReader reader = new(stream);
			// if (opcode == 0x03B0) {
			// 	// (ushort)ServerZoneIpcType.Chat
			// 	XivIpcChat chat = Marshal.PtrToStructure<XivIpcChat>(dataPtr);
			// 	// var sn = Marshal.PtrToStringUTF8(new IntPtr(chat.name));
			// 	// var sv = Marshal.PtrToStringUTF8(new IntPtr(chat.msg));
			// 	var sn = SeString.Parse(chat.msg, 32);
			// 	var sv = SeString.Parse(chat.msg, 1012);
			// 	PluginLog.Log("MSG FROM: " + sn.TextValue);
			// 	PluginLog.Log("MSG: " + sv.TextValue);
			// }
		}


		public void Dispose() {
			NextUIPlugin.gameNetwork.NetworkMessage -= GameNetworkOnNetworkMessage;
		}
	}
}