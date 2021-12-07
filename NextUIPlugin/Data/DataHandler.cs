using System;
using System.Collections.Generic;
using Dalamud.Game;
using Dalamud.Game.ClientState.Objects.Types;
using NextUIPlugin.Service;
using SpellAction = Lumina.Excel.GeneratedSheets.Action;
using BattleChara = FFXIVClientStructs.FFXIV.Client.Game.Character.BattleChara;

namespace NextUIPlugin.Data {
	public class DataHandler : IDisposable {
		protected readonly Dictionary<string, uint?> targets = new();
		protected readonly Dictionary<string, bool> casts = new();
		protected List<uint> party = new List<uint>();

		public DataHandler() {
			NextUIPlugin.framework.Update += FrameworkOnUpdate;
		}

		protected void FrameworkOnUpdate(Framework framework) {
			WatchCasts();
			WatchTargets();

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

		protected void WatchTargets() {
			Dictionary<string, GameObject?> currentTargets = new() {
				{ "target", NextUIPlugin.targetManager.Target },
				{ "targetOfTarget", NextUIPlugin.targetManager.Target?.TargetObject },
				{ "hover", NextUIPlugin.targetManager.MouseOverTarget },
				{ "focus", NextUIPlugin.targetManager.FocusTarget },
			};

			foreach ((string key, var value) in currentTargets) {
				if (!targets.ContainsKey(key)) {
					targets[key] = null;
				}

				if (value != null) {
					if (targets[key] != value.ObjectId) {
						targets[key] = value.ObjectId;
						BroadcastTargetChanged(key, value.ObjectId, value.Name.TextValue);
					}
				}
				else {
					if (targets[key] != null) {
						targets[key] = null;
						BroadcastTargetChanged(key);
					}
				}
			}
		}

		protected unsafe void WatchCasts() {
			Dictionary<string, GameObject?> actorsCasts = new() {
				{ "player", NextUIPlugin.clientState.LocalPlayer },
				{ "target", NextUIPlugin.targetManager.Target },
				{ "targetOfTarget", NextUIPlugin.targetManager.Target?.TargetObject },
				{ "focus", NextUIPlugin.targetManager.FocusTarget },
			};

			foreach ((string key, var actor) in actorsCasts) {
				if (actor == null) {
					continue;
				}

				if (!casts.ContainsKey(key)) {
					casts[key] = false;
				}

				var battleChara = (BattleChara*)actor.Address;
				var castInfo = battleChara->SpellCastInfo;

				var isCasting = castInfo.IsCasting > 0;
				var targetIsCasting = casts[key];

				if (isCasting != targetIsCasting && isCasting) {
					string castName = ActionService.GetActionNameFromCastInfo(actor.TargetObject, castInfo);
					BroadcastCastStart(
						key,
						castInfo.ActionID,
						castName,
						castInfo.CurrentCastTime,
						castInfo.TotalCastTime,
						castInfo.CastTargetID
					);
				}

				casts[key] = isCasting;
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
			uint? actorId = null,
			string? actorName = null
		) {
			NextUIPlugin.socketServer.Broadcast(new {
				@event = "targetChanged",
				targetType,
				actorId,
				actorName
			});
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
		}
	}
}