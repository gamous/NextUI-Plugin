using System.Collections.Generic;
using System.Linq;
using Dalamud.Game.ClientState.Objects.Types;
using Fleck;
using NextUIPlugin.Socket;

namespace NextUIPlugin.Data.Handlers {
	public static class ActorHandler {
		internal static readonly Dictionary<BattleCharaCopy, List<IWebSocketConnection>> savedChara = new();

		public static void RegisterCommands() {
			NextUISocket.RegisterCommand("watchActor", WatchActor);
			NextUISocket.RegisterCommand("unwatchActor", UnwatchActor);
			NextUISocket.RegisterCommand("unwatchAllActors", UnwatchAllActors);
			NextUISocket.RegisterCommand("getActor", GetActor);
			NextUISocket.RegisterCommand("getActors", GetActors);
		}

		// Special treatment if we save any sockets
		public static void RemoveSocket(IWebSocketConnection socket) {
			// Remove socket from chara watch once disconnected
			foreach (var (_, connections) in savedChara) {
				if (connections.Contains(socket)) {
					connections.Remove(socket);
				}
			}
		}

		/**
		 * Fetch all actors from game
		 */
		internal static void GetActors(IWebSocketConnection socket, SocketEvent ev) {
			var actors = new List<object>();
			foreach (var actor in NextUIPlugin.objectTable) {
				if (actor is BattleChara chara) {
					actors.Add(DataConverter.ActorToObject(chara));
				}
			}

			NextUISocket.Respond(socket, ev, actors);
		}

		/**
		 * Fetch a single actor from table
		 */
		internal static void GetActor(IWebSocketConnection socket, SocketEvent ev) {
			var objectId = ev.request?.id ?? 0;
			if (objectId == 0) {
				return;
			}

			var actor = NextUIPlugin.objectTable.SearchById(objectId);

			if (actor != null && actor is BattleChara chara) {
				NextUISocket.Respond(socket, ev, DataConverter.ActorToObject(chara));
				return;
			}

			NextUISocket.Respond(socket, ev, null);
		}

		internal static void WatchActor(IWebSocketConnection socket, SocketEvent ev) {
			var objectId = ev.request?.id ?? 0;
			if (objectId == 0) {
				return;
			}

			var obj = NextUIPlugin.objectTable.SearchById(objectId);

			if (obj is not BattleChara chara) {
				NextUISocket.Respond(socket, ev, new { success = false });
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

			NextUISocket.Respond(socket, ev, new { success = true });
		}

		internal static void UnwatchAllActors(IWebSocketConnection socket, SocketEvent ev) {
			var toClean = new List<BattleCharaCopy>();
			foreach (var (charaCopy, connections) in savedChara) {
				if (connections.Contains(socket)) {
					connections.Remove(socket);
				}

				if (connections.Count == 0) {
					toClean.Add(charaCopy);
				}
			}

			foreach (var charaCopy in toClean) {
				savedChara.Remove(charaCopy);
			}

			NextUISocket.Respond(socket, ev, new { success = true });
		}

		internal static void UnwatchActor(IWebSocketConnection socket, SocketEvent ev) {
			var objectId = ev.request?.id ?? 0;
			if (objectId == 0) {
				return;
			}

			var foundChara = savedChara.Keys.FirstOrDefault(c => c.ObjectId == objectId);
			if (foundChara == null) {
				NextUISocket.Respond(socket, ev, new { success = false });
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

			NextUISocket.Respond(socket, ev, new { success = true });
		}

		public static void Watch() {
			// Do not process if player is not loaded
			foreach (var (charaCopy, socketList) in savedChara) {
				var obj = NextUIPlugin.objectTable.SearchById(charaCopy.ObjectId);
				if (obj is BattleChara chara) {
					if (charaCopy.HasChanged(chara, false, false)) {
						charaCopy.UpdateFromBattleChara(chara);
						BroadcastActorChanged(charaCopy.ObjectId, false, socketList, chara);
					}
				}
				else {
					BroadcastActorChanged(charaCopy.ObjectId, true, socketList);
					// Chara no longer exists in game memory, no need to watch it
					savedChara.Remove(charaCopy);
				}
			}
		}

		internal static void BroadcastActorChanged(
			uint actorId,
			bool removed,
			List<IWebSocketConnection> sockets,
			BattleChara? chara = null
		) {
			NextUISocket.BroadcastTo(new {
				@event = "actorChanged",
				data = new {
					actorId,
					removed,
					actor = chara != null ? DataConverter.ActorToObject(chara) : null
				}
			}, sockets);
		}
	}
}