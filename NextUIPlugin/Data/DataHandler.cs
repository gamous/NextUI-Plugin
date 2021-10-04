using System;
using System.Collections.Generic;
using Dalamud.Game;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using NextUIPlugin.Service;
using SpellAction = Lumina.Excel.GeneratedSheets.Action;
using BattleChara = FFXIVClientStructs.FFXIV.Client.Game.Character.BattleChara;

namespace NextUIPlugin.Data {
	public class DataHandler : IDisposable {
		public Action<string>? onPlayerNameChanged;
		public Action<string, uint?, string?>? onTargetChanged;
		public Action<uint, string>? onHoverChanged;
		public Action<List<uint>>? onPartyChanged;
		public Action<string, uint, string, float, float>? CastStart;

		protected readonly Dictionary<string, uint?> targets = new();
		protected readonly Dictionary<string, bool> casts = new();
		protected List<uint> party = new List<uint>();

		public DataHandler() {
			NextUIPlugin.framework.Update += FrameworkOnUpdate;
		}

		protected unsafe void FrameworkOnUpdate(Framework framework) {
			WatchCasts();

			Dictionary<string, GameObject?> currentTargets = new() {
				{ "target", NextUIPlugin.targetManager.Target },
				{ "targetOfTarget", NextUIPlugin.targetManager.Target?.TargetObject },
				{ "hover", NextUIPlugin.targetManager.MouseOverTarget },
				{ "focus", NextUIPlugin.targetManager.FocusTarget },
			};
			
			foreach ((string key, GameObject? value) in currentTargets) {
				if (!targets.ContainsKey(key)) {
					targets[key] = null;
				}

				if (value != null) {
					if (targets[key] != value.ObjectId) {
						targets[key] = value.ObjectId;
						onTargetChanged?.Invoke(key, value.ObjectId, value.Name.TextValue);
					}
				}
				else {
					if (targets[key] != null) {
						targets[key] = null;
						onTargetChanged?.Invoke(key, null, null);
					}
				}
			}
			
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

		protected unsafe void WatchCasts() {
			Dictionary<string, GameObject?> actorsCasts = new() {
				{ "player", NextUIPlugin.clientState.LocalPlayer },
				{ "target", NextUIPlugin.targetManager.Target },
				{ "targetOfTarget", NextUIPlugin.targetManager.Target?.TargetObject },
				{ "focus", NextUIPlugin.targetManager.FocusTarget },
			};

			foreach ((string key, GameObject? actor) in actorsCasts) {
				if (actor == null) {
					continue;
				}

				if (!casts.ContainsKey(key)) {
					casts[key] = false;
				}

				BattleChara* battleChara = (BattleChara*)actor.Address;
				BattleChara.CastInfo castInfo = battleChara->SpellCastInfo;

				bool isCasting = castInfo.IsCasting > 0;
				bool targetIsCasting = casts[key];

				if (isCasting != targetIsCasting && isCasting) {
					string castName = ActionService.GetActionNameFromCastInfo(castInfo);
					CastStart?.Invoke(key, castInfo.ActionID, castName, castInfo.CurrentCastTime, castInfo.TotalCastTime);
				}

				casts[key] = isCasting;
			}
		}

		public void Dispose() {
			NextUIPlugin.framework.Update -= FrameworkOnUpdate;
		}
	}
}