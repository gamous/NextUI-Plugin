using System.Security;
using Dalamud.Data;
using Dalamud.Game;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.ClientState.Party;
using Dalamud.Game.Command;
using Dalamud.Game.Gui;
using Dalamud.Game.Network;
using Dalamud.IoC;
using Dalamud.Logging;
using Dalamud.Plugin;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;
using Newtonsoft.Json;
using NextUIPlugin.Configuration;
using NextUIPlugin.Data;
using NextUIPlugin.Gui;
using NextUIPlugin.Service;
using NextUIPlugin.Socket;
using XivCommon;
using Condition = Dalamud.Game.ClientState.Conditions.Condition;

[assembly:AllowPartiallyTrustedCallers]
namespace NextUIPlugin {
	// ReSharper disable once InconsistentNaming
	// ReSharper disable once ClassNeverInstantiated.Global
	public class NextUIPlugin : IDalamudPlugin {
		public string Name => "NextUIPlugin";

		public static NextUIConfiguration configuration = null!;

		/** Dalamud injected services */
		// ReSharper disable InconsistentNaming
		// ReSharper disable ReplaceAutoPropertyWithComputedProperty
		[PluginService] public static CommandManager commandManager { get; set; } = null!;
		[PluginService] public static DalamudPluginInterface pluginInterface { get; set; } = null!;
		[PluginService] public static ObjectTable objectTable { get; set; } = null!;
		[PluginService] public static Framework framework { get; set; } = null!;
		[PluginService] public static GameNetwork gameNetwork { get; set; } = null!;
		[PluginService] public static ClientState clientState { get; set; } = null!;
		[PluginService] public static DataManager dataManager { get; set; } = null!;
		[PluginService] public static TargetManager targetManager { get; set; } = null!;
		[PluginService] public static Condition condition { get; set; } = null!;
		[PluginService] public static PartyList partyList { get; set; } = null!;
		[PluginService] public static SigScanner sigScanner { get; set; } = null!;
		[PluginService] public static ChatGui chatGui { get; set; } = null!;
		[PluginService] public static GameGui gameGui { get; set; } = null!;
		// ReSharper enable InconsistentNaming
		// ReSharper enable ReplaceAutoPropertyWithComputedProperty

		/** Internal services */
		public static MouseOverService mouseOverService = null!;
		public static GuiManager guiManager = null!;
		public static NextUISocket socketServer = null!;

		public readonly DataHandler dataHandler;
		public readonly NetworkHandler networkHandler;
		public static XivCommonBase xivCommon { get; set; } = null!;
		public static ExcelSheet<BNpcName>? npcNameSheet;

		public NextUIPlugin() {
			pluginInterface.UiBuilder.DisableCutsceneUiHide = true;

			npcNameSheet = dataManager.GetExcelSheet<BNpcName>();
			xivCommon = new XivCommonBase();
			mouseOverService = new MouseOverService();

			configuration = pluginInterface.GetPluginConfig() as NextUIConfiguration ?? new NextUIConfiguration();
			configuration.PrepareConfiguration();
			PluginLog.Information(JsonConvert.SerializeObject(configuration));

			pluginInterface.UiBuilder.OpenConfigUi += OnOpenConfigUi;
			pluginInterface.UiBuilder.Draw += Render;

			socketServer = new NextUISocket(configuration.socketPort);
			socketServer.Start();

			dataHandler = new DataHandler();
			networkHandler = new NetworkHandler();

			guiManager = new GuiManager();
			guiManager.Initialize(pluginInterface);

			MicroPluginService.Initialize();
			IPCService.InitIpc();

			commandManager.AddHandler("/nu", new CommandInfo(OnCommandDebugCombo) {
				HelpMessage = "Open NextUI Plugin configuration. \n" +
				              "/nu toggle → Toggles all visible overlays.",
				ShowInHelp = true
			});
		}

		public void OnOpenConfigUi() {
			ConfigWindow.isConfigOpen = true;
		}

		public void Render() {
			guiManager?.Render();
			MicroPluginService.DrawProgress();
			MicroPluginService.DrawWarningWindow();
			if (!ConfigWindow.isConfigOpen) {
				return;
			}

			ConfigWindow.RenderConfig();
		}

		public void Dispose() {
			commandManager.RemoveHandler("/nu");
			dataHandler.Dispose();
			socketServer.Dispose();
			guiManager?.Dispose();
			MicroPluginService.Shutdown();
			IPCService.Deinit();
			mouseOverService.Dispose();

			xivCommon?.Dispose();
		}

		protected void OnCommandDebugCombo(string command, string arguments) {
			string[] argumentsParts = arguments.Split();

			switch (argumentsParts[0]) {
				case "toggle":
					guiManager!.ToggleOverlays();
					break;
				case "reload":
					guiManager!.ReloadOverlays();
					break;
				default:
					ConfigWindow.isConfigOpen = true;
					break;
			}

			pluginInterface.SavePluginConfig(configuration);
		}
	}
}