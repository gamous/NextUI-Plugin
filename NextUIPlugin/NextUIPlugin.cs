using System;
using System.IO;
using System.Linq;
using ImGuiNET;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using CefSharp;
using CefSharp.OffScreen;
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
using NextUIPlugin.Overlay;
using NextUIPlugin.Service;
using NextUIPlugin.Socket;

namespace NextUIPlugin {
	// ReSharper disable once InconsistentNaming
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
		public static OverlayManager? overlayManager;

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


			// TestCef();
			TestCefPlugin();

			overlayManager = new OverlayManager();
			overlayManager.Initialize(pluginInterface);

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
			
			var plug = PluginLoader.CreateFromAssemblyFile(
				assemblyFile: Path.Combine(dir, "NextUIBrowser.dll"),
				sharedTypes: new[] { typeof(INuPlugin) },
				isUnloadable: false,
				configure: config => {
					config.IsUnloadable = false;
					config.LoadInMemory = false;
					// config.DefaultContext.
				} 
			);
			
			PluginLog.Log("CEFDLL 1?");
			var assembly = plug.LoadDefaultAssembly();
			PluginLog.Log("CEFDLL 2?");
			var pluginInst = assembly.CreateInstance("NextUIBrowser.BrowserPlugin");
			PluginLog.Log("CEFDLL 3?");
			var plugType = pluginInst.GetType();
			PluginLog.Log("CEFDLL 4?");
			plugType.GetMethod("Initialize", BindingFlags.Public | BindingFlags.Instance).Invoke(
				pluginInst,
				new[] { dir }
			);
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		public void TestCef() {
			string? dir = pluginInterface.AssemblyLocation.DirectoryName;
			// PluginLog.Log("DIR LOC " + dir);

			var plug = PluginLoader.CreateFromAssemblyFile(
				assemblyFile: Path.Combine(dir, "CefSharp.OffScreen.dll"),
				sharedTypes: Array.Empty<Type>(),
				isUnloadable: false
			);
			PluginLog.Log("CEF 1?");
			var assembly = plug.LoadDefaultAssembly();

			var plug2 = PluginLoader.CreateFromAssemblyFile(
				assemblyFile: Path.Combine(dir, "CefSharp.dll"),
				sharedTypes: Array.Empty<Type>(),
				isUnloadable: false
			);
			PluginLog.Log("CEF 1x?");
			// foreach (var VARIABLE in plug2.EnterContextualReflection()) {
			// 	
			// }
			var assemblyCef = plug2.LoadAssembly("CefSharp");


			// var assembly = Assembly.LoadFile(Path.Combine(dir, "CefSharp.OffScreen.dll"));
			// var cefAss = Assembly.LoadFile(Path.Combine(dir, "CefSharp.dll"));
			PluginLog.Log("CEF 2?");

			foreach (var VARIABLE in assembly.GetTypes()) {
				PluginLog.Log("FOUND? " + VARIABLE.Name);
			}

			var settings = assembly.CreateInstance("CefSharp.OffScreen.CefSettings");
			var settingsType = settings.GetType();

			PluginLog.Log("CEF 3?" + settings?.GetType().ToString());
			var browser = Path.Combine(dir, @"CefSharp.BrowserSubprocess.exe");
			var locales = Path.Combine(dir, @"locales\");
			var res = Path.Combine(dir);

			// settings.GetType().GetMethod("EnableAudio").Invoke(settings, Array.Empty<object>());

			// CefSettings
			// settings.BrowserSubprocessPath = browser;
			// settings.LocalesDirPath = locales;
			// settings.ResourcesDirPath = res;

			settingsType.GetProperty("BrowserSubprocessPath").SetValue(settings, browser);
			settingsType.GetProperty("LocalesDirPath").SetValue(settings, locales);
			settingsType.GetProperty("ResourcesDirPath").SetValue(settings, res);

			// Set BrowserSubProcessPath when cefsharp moved to the subfolder
			// if (resolved) {


			// new CefLibraryHandle()
			// }

			// Make sure you set performDependencyCheck false
			// settings.CefCommandLineArgs["autoplay-policy"] = "no-user-gesture-required";
			PluginLog.Log("CEF 4?");
			// settings.EnableAudio();
			foreach (var VARIABLE in settingsType.GetMethods(BindingFlags.Public | BindingFlags.Instance)) {
				PluginLog.Log("Method " + VARIABLE.Name);
			}

			settingsType.GetMethod("EnableAudio").Invoke(settings, Array.Empty<object>());
			PluginLog.Log("CEF 5?");
			// settings.SetOffScreenRenderingBestPerformanceArgs();
			settingsType.GetMethod("SetOffScreenRenderingBestPerformanceArgs").Invoke(settings, Array.Empty<object>());
			PluginLog.Log("CEF WORKINGXXXX?");

			foreach (var VARIABLE in assemblyCef.GetTypes()) {
				PluginLog.Warning("TYPE " + VARIABLE.Name);
			}

			var cefType = assemblyCef.GetType("CefSharp.Cef");
			cefType.GetMethod("Initialize", BindingFlags.Static | BindingFlags.Public).Invoke(null, new[] {
				settings, true, null
			});
			// Cef.Initialize(settings, true, null);
			// Cef.IsInitialized
			PluginLog.Log("CEF WORKING?");
			PluginLog.Log("CEF WORKING? " + cefType
				.GetProperty("IsInitialized", BindingFlags.Static | BindingFlags.Public)
				.GetValue(null).ToString()
			);
		}

		protected static Assembly? CurrentDomain_AssemblyResolve(object? sender, ResolveEventArgs args) {
			PluginLog.Log("ATTX " + args.Name);
			if (args.Name.StartsWith("CefSharp")) {
				// Set to true, so BrowserSubprocessPath will be set
				string? dir = pluginInterface.AssemblyLocation.DirectoryName;
				PluginLog.Log("ATT" + args.Name);
				string assemblyName = args.Name.Split(new[] { ',' }, 2)[0]; //  + ".dll"

				PluginLog.Log("ATTN " + assemblyName);
				string subfolderPath = Path.Combine(
					dir, assemblyName
				).Replace(".dll.dll", ".dll").Replace(".dll.dll", ".dll");
				PluginLog.Log("ATT2 " + subfolderPath);
				return File.Exists(subfolderPath) ? Assembly.LoadFile(subfolderPath) : null;
			}

			return null;
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
			overlayManager?.Render();

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
				overlayManager?.Navigate(configuration.overlayUrl);
			}

			ImGui.SameLine();
			if (ImGui.Button("Debug")) {
				PluginLog.Log("Debugging overlay");
				overlayManager?.Debug();
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
			overlayManager?.Dispose();
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