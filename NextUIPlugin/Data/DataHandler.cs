using System;
using System.Collections.Generic;
using Dalamud.Game;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Fleck;
using NextUIPlugin.Service;
using NextUIPlugin.Socket;
using SpellAction = Lumina.Excel.GeneratedSheets.Action;

namespace NextUIPlugin.Data {
	public class DataHandler : IDisposable {
		protected readonly Dictionary<string, (uint?, string?)> targets = new();
		protected readonly Dictionary<string, bool> casts = new();
		protected List<uint> party = new List<uint>();

		public DataHandler() {
			NextUIPlugin.framework.Update += FrameworkOnUpdate;
			NextUIPlugin.chatGui.ChatMessage += ChatGuiOnChatMessage;
			NextUIPlugin.clientState.Login += ClientStateOnLogin;
			NextUIPlugin.clientState.Logout += ClientStateOnLogout;
		}

		protected void ClientStateOnLogout(object? sender, EventArgs e) {
			NextUIPlugin.socketServer.Broadcast(new {
				@event = "playerLogout",
			});
		}

		protected void ClientStateOnLogin(object? sender, EventArgs e) {
			var player = NextUIPlugin.clientState.LocalPlayer;
			NextUIPlugin.socketServer.Broadcast(new {
				@event = "playerLogin",
				player = player != null ? NextUISocket.ActorToObject(player) : null
			});
		}

		protected void ChatGuiOnChatMessage(
			XivChatType type,
			uint senderId,
			ref SeString sender,
			ref SeString message,
			ref bool isHandled
		) {
			var sockets = NextUIPlugin.socketServer.GetEventSubscriptions("chatMessage");
			if (sockets != null && sockets.Count > 0) {
				NextUIPlugin.socketServer.BroadcastTo(new {
					@event = "chatMessage",
					senderId,
					sender = sender.TextValue,
					message = message.TextValue,
				}, sockets);
			}
		}

		protected void FrameworkOnUpdate(Framework framework) {
			WatchCasts();
			WatchTargets();
			WatchBattleChara();

			// List<int> currentParty = NextUIPlugin.clientState.
			// 	.Select(partyMember => partyMember.Actor.ActorId).ToList();
			//
			// if (party.Count != currentParty.Count) {
			// 	onPartyChanged?.Invoke(currentParty);
			// }
			// else {
			// 	List<int> firstNotSecond = party.Except(currentParty).ToList();
			// 	List<int> secondNotFirst = currentParty.Except(party).ToList();
			//
			// 	bool eq = !firstNotSecond.Any() && !secondNotFirst.Any();
			// 	if (!eq) {
			// 		onPartyChanged?.Invoke(currentParty);
			// 		party = currentParty;
			// 	}
			// }
		}

		/**
		 * Watch common differences on battle actors and report any changes to it (ignores position and rotation)
		 */
		protected void WatchBattleChara() {
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

		protected void WatchTargets() {
			Dictionary<string, GameObject?> currentTargets = new() {
				{ "target", NextUIPlugin.targetManager.Target },
				{ "targetOfTarget", NextUIPlugin.targetManager.Target?.TargetObject },
				{ "hover", NextUIPlugin.targetManager.MouseOverTarget },
				{ "focus", NextUIPlugin.targetManager.FocusTarget },
			};

			foreach ((string key, var value) in currentTargets) {
				if (!targets.ContainsKey(key)) {
					targets[key] = (null, null);
				}

				var (targetId, targetName) = targets[key];
				if (value != null) {
					if (targetId != value.ObjectId || targetName != value.Name.TextValue) {
						targets[key] = (value.ObjectId, value.Name.TextValue);
						BroadcastTargetChanged(key, value);
					}
				}
				else {
					if (targetId != null) {
						targets[key] = (null, null);
						BroadcastTargetChanged(key);
					}
				}
			}
		}

		protected void WatchCasts() {
			Dictionary<string, GameObject?> actorsCasts = new() {
				{ "player", NextUIPlugin.clientState.LocalPlayer },
				{ "target", NextUIPlugin.targetManager.Target },
				{ "targetOfTarget", NextUIPlugin.targetManager.Target?.TargetObject },
				{ "focus", NextUIPlugin.targetManager.FocusTarget },
			};

			foreach ((string key, var actor) in actorsCasts) {
				if (actor == null || actor is not BattleChara battleChara) {
					continue;
				}

				if (!casts.ContainsKey(key)) {
					casts[key] = false;
				}

				var targetIsCasting = casts[key];

				if (battleChara.IsCasting != targetIsCasting && battleChara.IsCasting) {
					string castName = ActionService.GetActionNameFromBattleChara(battleChara);
					BroadcastCastStart(
						key,
						battleChara.CastActionId,
						castName,
						battleChara.CurrentCastTime,
						battleChara.TotalCastTime,
						battleChara.CastTargetObjectId
					);
				}

				casts[key] = battleChara.IsCasting;
			}
		}

		/*
		protected void PartyChanged(List<int> party) {
			socketServer.Broadcast(JsonConvert.SerializeObject(new SocketEventPartyChanged {
				guid = Guid.NewGuid().ToString(),
				type = "partyChanged",
				party = party.ToArray()
			}));
		}

		protected void NameChanged(string name) {
			socketServer.Broadcast("player name: " + name);
		}


		*/

		#region Broadcasters

		protected static void BroadcastTargetChanged(
			string targetType,
			GameObject? actor = null
		) {
			NextUIPlugin.socketServer.Broadcast(new {
				@event = "targetChanged",
				targetType,
				actorId = actor?.ObjectId,
				actorName = actor?.Name.TextValue,
				actor = actor == null ? null : (actor is BattleChara chara ? NextUISocket.ActorToObject(chara) : null),
			});
		}

		protected static void BroadcastActorChanged(
			uint actorId,
			bool removed,
			List<IWebSocketConnection> sockets,
			BattleChara? chara = null
		) {
			NextUIPlugin.socketServer.BroadcastTo(new {
				@event = "actorChanged",
				actorId,
				removed,
				actor = chara != null ? NextUISocket.ActorToObject(chara) : null
			}, sockets);
		}

		protected static void BroadcastCastStart(
			string target,
			uint actionId,
			string actionName,
			float currentTime,
			float totalTime,
			uint targetId
		) {
			NextUIPlugin.socketServer.Broadcast(new {
				@event = "castStart",
				target,
				actionId,
				actionName,
				currentTime,
				totalTime,
				targetId,
			});
		}

		#endregion

		public void Dispose() {
			NextUIPlugin.framework.Update -= FrameworkOnUpdate;
			NextUIPlugin.chatGui.ChatMessage -= ChatGuiOnChatMessage;
			NextUIPlugin.clientState.Login -= ClientStateOnLogin;
			NextUIPlugin.clientState.Logout -= ClientStateOnLogout;
		}
	}
}