using System;
using System.IO;
using ImGuiNET;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using Dalamud.Data;
using Dalamud.Game;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.Command;
using Dalamud.Game.Network;
using Dalamud.Logging;
using Dalamud.Plugin;
using McMaster.NETCore.Plugins;
using Newtonsoft.Json;
using NextUIPlugin.Configuration;
using NextUIPlugin.Data;
using NextUIPlugin.Gui;
using NextUIPlugin.Service;
using NextUIPlugin.Socket;
using NextUIShared;

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

		public static MouseOverService? mouseOverService;

		// ReSharper disable once InconsistentNaming
		protected bool isNextUISetupOpen;
		public static NextUISocket socketServer = null!;

		protected DataHandler dataHandler;
		public static GuiManager? guiManager;
		
		/**
		 * DANGER ZONE
		 */
		protected PluginLoader? micropluginLoader;
		protected INuPlugin? microplugin;

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

			mouseOverService = new MouseOverService(sigScanner, dataManager);

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

			guiManager = new GuiManager();
			guiManager.Initialize(pluginInterface);

			// TestCef();
			TestCefPlugin();

			// overlayManager = new OverlayManager();
			// overlayManager.Initialize(pluginInterface);


			commandManager.AddHandler("/nu", new CommandInfo(OnCommandDebugCombo) {
				HelpMessage = "Open NextUI Plugin configuration",
				ShowInHelp = true
			});
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		public void TestCefPlugin() {
			// string? dir = pluginInterface.AssemblyLocation.DirectoryName;
			string dir = @"A:\Projects\Kaminaris\ffxiv\NextUIPlug\NextUIPlugin\NextUIBrowser\bin\Release\win-x64";
			PluginLog.Log("Trying to load");
			
			micropluginLoader = PluginLoader.CreateFromAssemblyFile(
				assemblyFile: Path.Combine(dir, "NextUIBrowser.dll"),
				sharedTypes: new[] { typeof(INuPlugin), typeof(GuiManager) },
				isUnloadable: false,
				configure: config => {
					config.IsUnloadable = false;
					config.LoadInMemory = false;
				} 
			);
			
			var assembly = micropluginLoader.LoadDefaultAssembly();
			microplugin = (INuPlugin?)assembly.CreateInstance("NextUIBrowser.BrowserPlugin");
			if (microplugin == null) {
				PluginLog.Warning("Unable to load BrowserPlugin");
				return;
			}

			microplugin.Initialize(dir, guiManager);
			PluginLog.Log("Successfully loaded BrowserPlugin");
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

			if (nextUiConfiguration.overlayUrl == "") {
				// FOR LOCAL V
				nextUiConfiguration.overlayUrl = "http://localhost:4200?OVERLAY_WS=ws://127.0.0.1:10501/ws";
			}
		}

		protected void UpdateConfig() {
		}

		public void UiBuilder_OnOpenConfigUi() {
			isNextUISetupOpen = true;
		}

		public void UiBuilder_OnBuildUi() {
			guiManager?.Render();

			if (!isNextUISetupOpen) {
				return;
			}

			ImGui.SetNextWindowSize(new Vector2(640, 480));
			ImGui.Begin(
				"NextUI Configuration",
				ref isNextUISetupOpen,
				ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoScrollbar
			);

			ImGui.Text("Configure socket port in order to push game data into NextUI.");
			ImGui.Separator();

			ImGui.InputInt("Socket Port", ref configuration.socketPort);

			ImGui.Text("Overlay Options");
			ImGui.Separator();

			ImGui.InputText("URL", ref configuration.overlayUrl, 255);
			if (ImGui.Button("Reload")) {
				PluginLog.Log("Reloading overlay");
				//guiManager?.Navigate(configuration.overlayUrl);
			}

			ImGui.SameLine();
			if (ImGui.Button("Debug")) {
				PluginLog.Log("Debugging overlay");
				//guiManager?.Debug();
			}

			ImGui.Separator();

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
			guiManager?.Dispose();
			

			microplugin.Shutdown();
			micropluginLoader?.Dispose();
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