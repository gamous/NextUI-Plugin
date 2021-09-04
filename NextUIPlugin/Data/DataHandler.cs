using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Game.ClientState.Actors.Types;
using Dalamud.Game.Internal;
using Dalamud.Plugin;

namespace NextUIPlugin.Data {
	public class DataHandler : IDisposable {
		protected readonly DalamudPluginInterface pluginInterface;

		public Action<string> onPlayerNameChanged;
		public Action<string, int, string> onTargetChanged;
		public Action<int, string> onHoverChanged;
		public Action<List<int>> onPartyChanged;

		protected readonly Dictionary<string, int> targets = new Dictionary<string, int>();
		protected List<int> party = new List<int>();

		public DataHandler(DalamudPluginInterface pluginInterface) {
			this.pluginInterface = pluginInterface;
			this.pluginInterface.Framework.OnUpdateEvent += FrameworkOnOnUpdateEvent;
		}

		protected void FrameworkOnOnUpdateEvent(Framework framework) {
			PlayerCharacter player = pluginInterface.ClientState.LocalPlayer;
			if (player is null) {
				return;
			}

			Dictionary<string, Actor> currentTargets = new Dictionary<string, Actor> {
				{ "target", pluginInterface.ClientState.Targets.CurrentTarget },
				{ "hover", pluginInterface.ClientState.Targets.MouseOverTarget },
				{ "focus", pluginInterface.ClientState.Targets.FocusTarget },
			};

			foreach (KeyValuePair<string, Actor> entry in currentTargets) {
				if (!targets.ContainsKey(entry.Key)) {
					targets[entry.Key] = 0;
				}

				if (entry.Value != null) {
					if (targets[entry.Key] != entry.Value.ActorId) {
						targets[entry.Key] = entry.Value.ActorId;
						onTargetChanged?.Invoke(entry.Key, entry.Value.ActorId, entry.Value.Name);
					}
				}
				else {
					if (targets[entry.Key] != 0) {
						targets[entry.Key] = 0;
						onTargetChanged?.Invoke(entry.Key, 0, "");
					}
				}
			}

			List<int> currentParty = pluginInterface.ClientState.PartyList
				.Select(partyMember => partyMember.Actor.ActorId).ToList();

			if (party.Count != currentParty.Count) {
				onPartyChanged?.Invoke(currentParty);
			}
			else {
				List<int> firstNotSecond = party.Except(currentParty).ToList();
				List<int> secondNotFirst = currentParty.Except(party).ToList();
		
				bool eq = !firstNotSecond.Any() && !secondNotFirst.Any();
				if (!eq) {
					onPartyChanged?.Invoke(currentParty);
					party = currentParty;
				}
			}

		}

		public void Dispose() {
			pluginInterface.Framework.OnUpdateEvent -= FrameworkOnOnUpdateEvent;
		}
	}
}