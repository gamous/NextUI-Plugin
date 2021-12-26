using System.Collections.Generic;
using Dalamud.Game.ClientState.Objects.Types;
using Fleck;

namespace NextUIPlugin.Data.Handlers {
	public static class CharaWatcher {
		/**
		 * Watch common differences on battle actors and report any changes to it (ignores position and rotation)
		 */
		public static void Watch() {
			// Do not process if player is not loaded
			foreach (var (charaCopy, socketList) in NextUIPlugin.socketServer.savedChara) {
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
					NextUIPlugin.socketServer.savedChara.Remove(charaCopy);
				}
			}
		}

		internal static void BroadcastActorChanged(
			uint actorId,
			bool removed,
			List<IWebSocketConnection> sockets,
			BattleChara? chara = null
		) {
			NextUIPlugin.socketServer.BroadcastTo(new {
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