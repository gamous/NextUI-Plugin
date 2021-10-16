using System.Drawing;
using System.Numerics;
using ImGuiNET;
using NextUIPlugin.Gui;

namespace NextUIPlugin.Configuration {
	public static class ConfigWindow {
		public static bool isConfigOpen;

		public static void RenderConfig() {
			ImGui.SetNextWindowSize(new Vector2(640, 480));
			ImGui.Begin(
				"NextUI Configuration",
				ref isConfigOpen,
				ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoScrollbar
			);

			ImGui.Text("Configure socket port in order to push game data into NextUI.");
			ImGui.Separator();

			ImGui.InputInt("Socket Port", ref NextUIPlugin.configuration.socketPort);

			ImGui.Text("Overlays");
			ImGui.Separator();

			RenderPaneSelector();
			RenderOverlayPane();

			if (ImGui.Button("Save")) {
				NextUIPlugin.pluginInterface.SavePluginConfig(NextUIPlugin.configuration);
			}

			ImGui.SameLine();
			if (ImGui.Button("Save and Close")) {
				NextUIPlugin.pluginInterface.SavePluginConfig(NextUIPlugin.configuration);
				isConfigOpen = false;
			}

			ImGui.End();
		}

		internal static OverlayGui? selectedOverlay;

		internal static void RenderPaneSelector() {
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

			foreach (var overlay in NextUIPlugin.guiManager.overlays) {
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
				var created = NextUIPlugin.guiManager.CreateOverlay("https://google.com", new Size(800, 600));
				selectedOverlay = created;
			}

			// ImGui.PopFont();
			ImGui.PopStyleVar(2);

			ImGui.EndGroup();
		}

		internal static void RenderOverlayPane() {
			ImGui.SameLine();
			ImGui.BeginChild("details");
			RenderOverlayConfig();

			ImGui.EndChild();
		}


		private static bool RenderOverlayConfig() {
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
	}
}