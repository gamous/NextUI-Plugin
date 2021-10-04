using ImGuiNET;
using System.Numerics;
using Dalamud.Data;
using Dalamud.Game;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.Command;
using Dalamud.Interface;
using Dalamud.Logging;
using Dalamud.Plugin;
using Newtonsoft.Json;
using NextUIPlugin.Configuration;
using NextUIPlugin.Data;
using NextUIPlugin.Service;
using NextUIPlugin.Socket;

namespace NextUIPlugin {
	// ReSharper disable once InconsistentNaming
	public class NextUIPlugin : IDalamudPlugin {
		public string Name => "NextUIPlugin";

		public NextUIConfiguration configuration;

		/** Dalamud injected services */
		public readonly CommandManager commandManager;

		public static DalamudPluginInterface pluginInterface = null!;
		public static ObjectTable objectTable = null!;
		public static Framework framework = null!;
		public static ClientState clientState = null!;
		public static DataManager dataManager = null!;
		public static TargetManager targetManager = null!;

		public static MouseOverService? mouseOverService;

		// ReSharper disable once InconsistentNaming
		protected bool isNextUISetupOpen;
		public NextUISocket socketServer;

		protected DataHandler dataHandler;

		public NextUIPlugin(
			CommandManager commandManager,
			DalamudPluginInterface pluginInterface,
			ObjectTable objectTable,
			TargetManager targetManager,
			Framework framework,
			SigScanner sigScanner,
			DataManager dataManager,
			ClientState clientState
		) {
			this.commandManager = commandManager;
			NextUIPlugin.pluginInterface = pluginInterface;
			NextUIPlugin.objectTable = objectTable;
			NextUIPlugin.targetManager = targetManager;
			NextUIPlugin.framework = framework;
			NextUIPlugin.clientState = clientState;
			NextUIPlugin.dataManager = dataManager;
			mouseOverService = new MouseOverService(sigScanner, dataManager);

			commandManager.AddHandler("/nu", new CommandInfo(OnCommandDebugCombo) {
				HelpMessage = "Open NextUI Plugin configuration",
				ShowInHelp = true
			});

			configuration = pluginInterface.GetPluginConfig() as NextUIConfiguration ?? new NextUIConfiguration();
			PrepareConfig(configuration);
			PluginLog.Information(JsonConvert.SerializeObject(configuration));

			pluginInterface.UiBuilder.OpenConfigUi += UiBuilder_OnOpenConfigUi;
			pluginInterface.UiBuilder.Draw += UiBuilder_OnBuildUi;

			socketServer = new NextUISocket(objectTable, targetManager, configuration.socketPort);
			socketServer.Start();

			dataHandler = new DataHandler();
			// dataHandler.onPlayerNameChanged += NameChanged;
			dataHandler.onTargetChanged += OnTargetChanged;
			dataHandler.CastStart += CastStart;
			// dataHandler.onPartyChanged += PartyChanged;
		}

		protected void CastStart(string target, uint actionId, string name, float currentTime, float totalTime) {
			socketServer.Broadcast(JsonConvert.SerializeObject(new {
				@event = "castStart",
				target = target,
				actionId = actionId,
				actionName = name,
				currentTime = currentTime,
				totalTime = totalTime,
			}));
		}

		protected void OnTargetChanged(string type, uint? id, string? name) {
			// socketServer.Broadcast(JsonConvert.SerializeObject(new {
			// 	@event = "targetChanged",
			// 	targetType = type,
			// 	actorId = id,
			// 	actorName = name
			// }));
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

		protected void PrepareConfig(NextUIConfiguration nextUiConfiguration) {
			if (nextUiConfiguration.socketPort <= 1024 || nextUiConfiguration.socketPort > short.MaxValue) {
				PluginLog.Log("Resetting port to 32805");
				nextUiConfiguration.socketPort = 32805;
			}
		}

		protected void UpdateConfig() {
		}

		public void UiBuilder_OnOpenConfigUi() {
			isNextUISetupOpen = true;
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

			ImGui.InputInt("Socket Port", ref configuration.socketPort);

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
			commandManager.RemoveHandler("/nu");
			pluginInterface.Dispose();
			dataHandler.Dispose();
			socketServer.Dispose();
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