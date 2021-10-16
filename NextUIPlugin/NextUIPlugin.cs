using System;
using System.Drawing;
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
			dataHandler.TargetChanged += TargetChanged;
			dataHandler.CastStart += CastStart;
			// dataHandler.onPartyChanged += PartyChanged;

			guiManager = new GuiManager();
			guiManager.Initialize(pluginInterface);

			LoadMicroPlugin();

			commandManager.AddHandler("/nu", new CommandInfo(OnCommandDebugCombo) {
				HelpMessage = "Open NextUI Plugin configuration",
				ShowInHelp = true
			});
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		public void LoadMicroPlugin() {
			// string? dir = pluginInterface.AssemblyLocation.DirectoryName;
			string dir = @"A:\Projects\Kaminaris\ffxiv\NextUIPlug\NextUIPlugin\NextUIBrowser\bin\Release\win-x64";
			PluginLog.Log("Trying to load");

			micropluginLoader = PluginLoader.CreateFromAssemblyFile(
				assemblyFile: Path.Combine(dir, "NextUIBrowser.dll"),
				sharedTypes: new[] { typeof(INuPlugin), typeof(IGuiManager) },
				isUnloadable: false,
				configure: config => {
					config.IsUnloadable = false;
					config.LoadInMemory = false;
				}
			);
			PluginLog.Log("Loaded microplugin");

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

			ImGui.Text("Overlays");
			ImGui.Separator();

			RenderPaneSelector();
			// RenderOverlayPane();

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

		protected OverlayGui? selectedOverlay;

		protected void RenderPaneSelector() {
			// Selector pane
			ImGui.BeginGroup();
			ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(0, 0));

			var selectorWidth = 100;
			ImGui.BeginChild("panes", new Vector2(selectorWidth, -ImGui.GetFrameHeightWithSpacing()), true);

			// General settings
			// if (ImGui.Selectable($"General", selectedOverlay == null)) {
			// 	selectedOverlay = null;
			// }

			// Inlay selector list
			ImGui.Dummy(new Vector2(0, 5));
			ImGui.PushStyleVar(ImGuiStyleVar.Alpha, 0.5f);
			ImGui.Text("- Overlays -");
			ImGui.PopStyleVar();

			foreach (var overlay in guiManager.overlays) {
				if (ImGui.Selectable(
					$"{overlay.overlay.Name}##{overlay.overlay.Guid}",
					selectedOverlay == overlay
				)) {
					selectedOverlay = overlay;
				}
			}

			ImGui.EndChild();

			// Selector controls
			ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 0);
			// ImGui.PushFont(UiBuilder.IconFont);

			var buttonWidth = selectorWidth; //  / 2
			if (ImGui.Button("Add", new Vector2(buttonWidth, 0))) {
				var created = guiManager.CreateOverlay("https://google.com", new Size(800, 600));
				selectedOverlay = created;
			}

			// ImGui.PopFont();
			ImGui.PopStyleVar(2);

			ImGui.EndGroup();
		}

		protected void RenderOverlayPane() {
			ImGui.SameLine();
			ImGui.BeginChild("details");
			RenderOverlayConfig();

			ImGui.EndChild();
		}


		private bool RenderOverlayConfig() {
			if (selectedOverlay == null) {
				return false;
			}

			var dirty = false;

			var overlay = selectedOverlay.overlay;
			ImGui.PushID(overlay.Guid.ToString());

			var ovName = overlay.Name;
			var ovUrl = overlay.Url;

			dirty |= ImGui.InputText("Name", ref ovName, 100);

			// ImGui.PushStyleVar(ImGuiStyleVar.Alpha, 0.5f);
			// var commandName = GetInlayCommandName(inlayConfig);
			// ImGui.InputText("Command Name", ref commandName, 100);
			// ImGui.PopStyleVar();

			dirty |= ImGui.InputText("URL", ref ovUrl, 1000);
			if (ImGui.IsItemDeactivatedAfterEdit()) {
				selectedOverlay.overlay.Url = ovUrl;
				// selectedOverlay.Navigate(ovUrl);
			}

			// ImGui.SetNextItemWidth(100);
			// ImGui.Columns(2, "boolInlayOptions", false);

			var true_ = true;
			// if (inlayConfig.ClickThrough) {
			// 	ImGui.PushStyleVar(ImGuiStyleVar.Alpha, 0.5f);
			// }

			// dirty |= ImGui.Checkbox("Locked", ref inlayConfig.ClickThrough ? ref true_ : ref inlayConfig.Locked);
			// if (inlayConfig.ClickThrough) {
			// 	ImGui.PopStyleVar();
			// }

			// if (ImGui.IsItemHovered()) {
			// 	ImGui.SetTooltip(
			// 		"Prevent the inlay from being resized or moved. This is implicitly set by Click Through.");
			// }

			// ImGui.NextColumn();

			// dirty |= ImGui.Checkbox("Hidden", ref inlayConfig.Hidden);
			// if (ImGui.IsItemHovered()) {
			// 	ImGui.SetTooltip(
			// 		"Hide the inlay. This does not stop the inlay from executing, only from being displayed.");
			// }

			// ImGui.NextColumn();

			//
			// if (inlayConfig.ClickThrough) {
			// 	ImGui.PushStyleVar(ImGuiStyleVar.Alpha, 0.5f);
			// }
			//
			// dirty |= ImGui.Checkbox("Type Through",
			// 	ref inlayConfig.ClickThrough ? ref true_ : ref inlayConfig.TypeThrough);
			// if (inlayConfig.ClickThrough) {
			// 	ImGui.PopStyleVar();
			// }

			// if (ImGui.IsItemHovered()) {
			// 	ImGui.SetTooltip(
			// 		"Prevent the inlay from intercepting any keyboard events. Implicitly set by Click Through.");
			// }

			// ImGui.NextColumn();

			// dirty |= ImGui.Checkbox("Click Through", ref inlayConfig.ClickThrough);
			// if (ImGui.IsItemHovered()) {
			// 	ImGui.SetTooltip(
			// 		"Prevent the inlay from intercepting any mouse events. Implicitly sets Locked and Type Through.");
			// }
			//
			// ImGui.NextColumn();
			//
			// ImGui.Columns(1);

			if (ImGui.Button("Reload")) {
				selectedOverlay.overlay.Reload();
			}

			ImGui.SameLine();
			if (ImGui.Button("Open Dev Tools")) {
				selectedOverlay.overlay.Debug();
			}

			ImGui.PopID();

			return dirty;
		}

		public void Dispose() {
			commandManager.RemoveHandler("/nu");
			pluginInterface.Dispose();
			dataHandler.Dispose();
			socketServer.Dispose();
			guiManager?.Dispose();

			microplugin?.Shutdown();
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