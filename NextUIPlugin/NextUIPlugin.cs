using Dalamud.Data;
using Dalamud.Game;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.ClientState.Party;
using Dalamud.Game.Command;
using Dalamud.Game.Network;
using Dalamud.IoC;
using Dalamud.Logging;
using Dalamud.Plugin;
using Newtonsoft.Json;
using NextUIPlugin.Configuration;
using NextUIPlugin.Data;
using NextUIPlugin.Gui;
using NextUIPlugin.Service;
using NextUIPlugin.Socket;

namespace NextUIPlugin {
	// ReSharper disable once InconsistentNaming
	// ReSharper disable once ClassNeverInstantiated.Global
	public class NextUIPlugin : IDalamudPlugin {
		public string Name => "NextUIPlugin";

		public static NextUIConfiguration configuration = null!;

		/** Dalamud injected services */
		public readonly CommandManager commandManager;

		public static DalamudPluginInterface pluginInterface = null!;
		public static ObjectTable objectTable = null!;
		public static Framework framework = null!;
		public static GameNetwork gameNetwork = null!;
		public static ClientState clientState = null!;
		public static DataManager dataManager = null!;
		public static TargetManager targetManager = null!;
		[PluginService] public static Condition Condition { get; protected set; } = null!;
		[PluginService] public static PartyList PartyList { get; protected set; } = null!;

		public static MouseOverService? mouseOverService;

		// ReSharper disable once InconsistentNaming
		public static NextUISocket socketServer = null!;

		protected DataHandler dataHandler;
		public static GuiManager? guiManager;

		public NextUIPlugin(
			CommandManager commandManager,
			DalamudPluginInterface pluginInterface,
			ObjectTable objectTable,
			TargetManager targetManager,
			Framework framework,
			SigScanner sigScanner,
			DataManager dataManager,
			ClientState clientState,
			GameNetwork gameNetwork
		) {
			this.commandManager = commandManager;
			NextUIPlugin.pluginInterface = pluginInterface;
			NextUIPlugin.objectTable = objectTable;
			NextUIPlugin.targetManager = targetManager;
			NextUIPlugin.framework = framework;
			NextUIPlugin.clientState = clientState;
			NextUIPlugin.dataManager = dataManager;
			NextUIPlugin.gameNetwork = gameNetwork;

			pluginInterface.UiBuilder.DisableCutsceneUiHide = true;

			mouseOverService = new MouseOverService(sigScanner, dataManager);

			configuration = pluginInterface.GetPluginConfig() as NextUIConfiguration ?? new NextUIConfiguration();
			configuration.PrepareConfiguration();
			PluginLog.Information(JsonConvert.SerializeObject(configuration));

			pluginInterface.UiBuilder.OpenConfigUi += OnOpenConfigUi;
			pluginInterface.UiBuilder.Draw += Render;

			socketServer = new NextUISocket(objectTable, targetManager, configuration.socketPort);
			socketServer.Start();

			dataHandler = new DataHandler();
			// dataHandler.onPlayerNameChanged += NameChanged;
			dataHandler.TargetChanged += TargetChanged;
			dataHandler.CastStart += CastStart;
			// dataHandler.onPartyChanged += PartyChanged;

			guiManager = new GuiManager();
			guiManager.Initialize(pluginInterface);

			MicroPluginService.Initialize();

			commandManager.AddHandler("/nu", new CommandInfo(OnCommandDebugCombo) {
				HelpMessage = "Open NextUI Plugin configuration. \n" +
				              "/nu toggle → Toggles all visible overlays.",
				ShowInHelp = true
			});
		}

		protected void CastStart(
			string target,
			uint actionId,
			string name,
			float currentTime,
			float totalTime,
			uint targetId
		) {
			socketServer.Broadcast(JsonConvert.SerializeObject(new {
				@event = "castStart",
				target = target,
				actionId = actionId,
				actionName = name,
				currentTime = currentTime,
				totalTime = totalTime,
				targetId,
			}));
		}

		protected void TargetChanged(string type, uint? id, string? name) {
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

		public void OnOpenConfigUi() {
			ConfigWindow.isConfigOpen = true;
		}

		public void Render() {
			guiManager?.Render();

			if (!ConfigWindow.isConfigOpen) {
				return;
			}

			ConfigWindow.RenderConfig();
		}

		public void Dispose() {
			commandManager.RemoveHandler("/nu");
			pluginInterface.Dispose();
			dataHandler.Dispose();
			socketServer.Dispose();
			guiManager?.Dispose();

			MicroPluginService.Shutdown();
		}

		protected void OnCommandDebugCombo(string command, string arguments) {
			string[] argumentsParts = arguments.Split();

			switch (argumentsParts[0]) {
				case "toggle":
					guiManager!.ToggleOverlays();
					break;
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
					ConfigWindow.isConfigOpen = true;
					break;
			}

			pluginInterface.SavePluginConfig(configuration);
		}
	}
}