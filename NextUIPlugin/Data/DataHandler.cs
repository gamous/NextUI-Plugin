using System;
using Dalamud.Game.ClientState.Actors.Types;
using Dalamud.Game.Internal;
using Dalamud.Plugin;

namespace NextUIPlugin.Data {
	public class DataHandler {
		protected readonly DalamudPluginInterface pluginInterface;

		public Action<string> onPlayerNameChanged;
		public Action<int, string> onTargetChanged;

		protected int targetId; 

		public DataHandler(DalamudPluginInterface pluginInterface) {
			this.pluginInterface = pluginInterface;
			this.pluginInterface.Framework.OnUpdateEvent += FrameworkOnOnUpdateEvent;
		}

		protected void FrameworkOnOnUpdateEvent(Framework framework) {
			PlayerCharacter player = pluginInterface.ClientState.LocalPlayer;
			if (player is null) {
				return;
			}

			Actor currentTarget = pluginInterface.ClientState.Targets.CurrentTarget;
			if (currentTarget != null) {
				if (targetId != currentTarget.ActorId) {
					onTargetChanged?.Invoke(currentTarget.ActorId, currentTarget.Name);
				}
				targetId = currentTarget.ActorId;
			}
			else {
				targetId = 0;
				onTargetChanged?.Invoke(0, "");
			}

			// onPlayerNameChanged?.Invoke(player.Name);
		}
	}
}