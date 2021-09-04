using System.Numerics;
using Dalamud.Game.Command;
using Dalamud.Plugin;
using ImGuiNET;
using Newtonsoft.Json;
using NextUIPlugin.Configuration;
using NextUIPlugin.Data;
using NextUIPlugin.Socket;

namespace NextUIPlugin {
	// ReSharper disable once InconsistentNaming
	public class NextUIPlugin : IDalamudPlugin {
		public string Name => "NextUIPlugin";

		protected DalamudPluginInterface pluginInterface;
		public NextUIConfiguration configuration;

		// ReSharper disable once InconsistentNaming
		protected bool isNextUISetupOpen;
		public int socketPort = 32805;
		public NextUISocket socketServer;

		protected DataHandler dataHandler;

		public void Initialize(DalamudPluginInterface dalamudPluginInterface) {
			pluginInterface = dalamudPluginInterface;

			pluginInterface.CommandManager.AddHandler("/nu", new CommandInfo(OnCommandDebugCombo) {
				HelpMessage = "Open NextUI Plugin configuration",
				ShowInHelp = true
			});

			configuration = pluginInterface.GetPluginConfig() as NextUIConfiguration ?? new NextUIConfiguration();

			pluginInterface.UiBuilder.OnOpenConfigUi += (sender, args) => isNextUISetupOpen = true;
			pluginInterface.UiBuilder.OnBuildUi += UiBuilder_OnBuildUi;

			socketServer = new NextUISocket(pluginInterface, socketPort);
			socketServer.Start();

			dataHandler = new DataHandler(pluginInterface);
			dataHandler.onPlayerNameChanged += NameChanged;
			dataHandler.onTargetChanged += TargetChanged;
		}

		protected void NameChanged(string name) {
			socketServer.Broadcast("player name: " + name);
		}

		protected void TargetChanged(int id, string name) {
			socketServer.Broadcast(JsonConvert.SerializeObject(new {
				Event = "targetChanged",
				ActorId = id,
				ActorName = name
			}));
		}

		protected void UpdateConfig() {
		}

		public void UiBuilder_OnBuildUi() {
			if (!isNextUISetupOpen) {
				return;
			}


			ImGui.SetNextWindowSize(new Vector2(740, 490));
			ImGui.Begin(
				"NextUI Configuration",
				ref isNextUISetupOpen,
				ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoScrollbar
			);

			ImGui.Text("Configure socket port in order to push game data into NextUI.");
			ImGui.Separator();

			ImGui.InputInt("Socket Port", ref socketPort);

			if (ImGui.Button("Save")) {
				pluginInterface.SavePluginConfig(configuration);
				UpdateConfig();
			}

			ImGui.SameLine();
			if (ImGui.Button("Save and Close")) {
				pluginInterface.SavePluginConfig(configuration);
				isNextUISetupOpen = false;
				UpdateConfig();
			}

			ImGui.End();
		}

		public void Dispose() {
			pluginInterface.CommandManager.RemoveHandler("/nu");
			pluginInterface.Dispose();
			socketServer.Stop();
		}

		protected void OnCommandDebugCombo(string command, string arguments) {
			string[] argumentsParts = arguments.Split();

			switch (argumentsParts[0]) {
				// case "setall": {
				// 	foreach (var value in Enum.GetValues(typeof(CustomComboPreset)).Cast<CustomComboPreset>()) {
				// 		if (value == CustomComboPreset.None)
				// 			continue;
				//
				// 		this.Configuration.ComboPresets |= value;
				// 	}
				//
				// 	this.pluginInterface.Framework.Gui.Chat.Print("all SET");
				// }
				// 	break;

				default:
					isNextUISetupOpen = true;
					break;
			}

			pluginInterface.SavePluginConfig(configuration);
		}
	}
}