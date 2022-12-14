using System;
using System.Drawing;
using System.Linq;
using System.Numerics;
using Dalamud.Logging;
using ImGuiNET;
using NextUIPlugin.Gui;
using NextUIPlugin.Model;

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

			ImGui.Text("Websocket Server Port");
			ImGui.SameLine();
			ImGui.TextColored(new Vector4(1f, 0f, 0f, 1f), "WARNING: Do not touch unless you know what you are doing");
			ImGui.Separator();

			ImGui.InputInt("Socket Port", ref NextUIPlugin.configuration.socketPort);

			ImGui.Text("Overlays");
			ImGui.Separator();

			RenderPaneSelector();
			RenderOverlayPane();

			ImGui.SetCursorPos(new Vector2(8, 450));

			if (ImGui.Button("Save")) {
				SaveConfig();
			}

			ImGui.SameLine();
			if (ImGui.Button("Save and Close")) {
				SaveConfig();
				isConfigOpen = false;
			}

			ImGui.End();
		}

		internal static void SaveConfig() {
			NextUIPlugin.configuration.overlays = NextUIPlugin.guiManager.SaveOverlays();
			if (NextUIPlugin.socketServer.Port != NextUIPlugin.configuration.socketPort) {
				NextUIPlugin.socketServer.Port = NextUIPlugin.configuration.socketPort;
				NextUIPlugin.socketServer.Restart();
			}

			NextUIPlugin.pluginInterface.SavePluginConfig(NextUIPlugin.configuration);
		}

		internal static OverlayGui? selectedOverlay;

		internal static void RenderPaneSelector() {
			// Selector pane
			ImGui.BeginGroup();
			ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(0, 0));

			var selectorWidth = 200;
			ImGui.BeginChild("panes", new Vector2(selectorWidth, 300), true);

			// Inlay selector list
			foreach (var overlay in NextUIPlugin.guiManager.overlays) {
				if (ImGui.Selectable(
					$"{overlay.overlay.Name}##{overlay.overlay.Guid.ToString()}",
					selectedOverlay == overlay
				)) {
					selectedOverlay = overlay;
				}
			}

			ImGui.EndChild();

			// Selector controls
			ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 0);

			if (ImGui.Button("Add", new Vector2(selectorWidth, 0))) {
				var created = NextUIPlugin.guiManager.CreateOverlay("https://google.com", new Size(800, 600));
				selectedOverlay = created;
			}

			ImGui.PopStyleVar(2);

			ImGui.EndGroup();
		}

		internal static void RenderOverlayPane() {
			ImGui.SameLine();
			ImGui.BeginChild("details");

			RenderOverlayConfig();

			ImGui.EndChild();
		}

		internal static void RenderOverlayConfig() {
			if (selectedOverlay == null) {
				return;
			}

			var overlay = selectedOverlay.overlay;
			ImGui.PushID(overlay.Guid.ToString());

			var ovName = overlay.Name;
			if (ImGui.InputText("Name", ref ovName, 150)) {
				overlay.Name = ovName;
			}

			// ImGui.SameLine();

			var ovUrl = overlay.Url;
			ImGui.InputText("URL", ref ovUrl, 1000);
			if (ImGui.IsItemDeactivatedAfterEdit()) {
				selectedOverlay.overlay.Url = ovUrl;
				selectedOverlay.Navigate(ovUrl);
			}


			ImGui.SetNextItemWidth(140);
			ImGui.Columns(2, "overlayOptions", false);

			// Position
			var posX = overlay.Position.X;
			if (ImGui.DragInt("Position X", ref posX, 1f)) {
				overlay.Position = new Point(posX, overlay.Position.Y);
			}

			ImGui.NextColumn();

			var posY = overlay.Position.Y;
			if (ImGui.DragInt("Position Y", ref posY, 1f)) {
				overlay.Position = new Point(overlay.Position.X, posY);
			}

			ImGui.NextColumn();

			// Size
			var sizeW = overlay.Size.Width;
			if (ImGui.DragInt("Width", ref sizeW, 1f)) {
				overlay.Size = new Size(sizeW, overlay.Size.Height);
			}

			ImGui.NextColumn();

			var sizeH = overlay.Size.Height;
			if (ImGui.DragInt("Height", ref sizeH, 1f)) {
				overlay.Size = new Size(overlay.Size.Width, sizeH);
			}

			ImGui.NextColumn();

			var ovLocked = overlay.Locked;
			if (ImGui.Checkbox("Locked", ref ovLocked)) {
				overlay.Locked = ovLocked;
			}

			if (ImGui.IsItemHovered()) {
				ImGui.SetTooltip("Prevent the overlay from being resized or moved.");
			}

			ImGui.NextColumn();

			var ovHidden = overlay.Hidden;
			if (ImGui.Checkbox("Hidden", ref ovHidden)) {
				overlay.Hidden = ovHidden;
			}

			if (ImGui.IsItemHovered()) {
				ImGui.SetTooltip("This does not stop the overlay from executing, only from being displayed.");
			}

			ImGui.NextColumn();

			var ovTypeThrough = overlay.TypeThrough;
			if (ImGui.Checkbox("Type Through", ref ovTypeThrough)) {
				overlay.TypeThrough = ovTypeThrough;
			}

			if (ImGui.IsItemHovered()) {
				ImGui.SetTooltip("Prevent the overlay from intercepting any keyboard events.");
			}

			ImGui.NextColumn();

			var ovClickThrough = overlay.ClickThrough;
			if (ImGui.Checkbox("Click Through", ref ovClickThrough)) {
				overlay.ClickThrough = ovClickThrough;
			}

			if (ImGui.IsItemHovered()) {
				ImGui.SetTooltip("Prevent the inlay from intercepting any mouse events.");
			}

			ImGui.NextColumn();

			var ovFullScreen = overlay.FullScreen;
			if (ImGui.Checkbox("Fullscreen", ref ovFullScreen)) {
				overlay.FullScreen = ovFullScreen;
			}

			if (ImGui.IsItemHovered()) {
				ImGui.SetTooltip("Makes overlay over entire screen");
			}

			ImGui.Columns(1);

			var visFlags = Enum.GetValues(typeof(OverlayVisibility)).Cast<OverlayVisibility>();

			var ovShow = overlay.VisibilityShow;
			var showFlagString = ovShow == 0 ? "" : ovShow.ToString();
			if (ImGui.BeginCombo("Show When", showFlagString)) {
				foreach (var flag in visFlags) {
					var isSelected = ovShow.HasFlag(flag);
					if (ImGui.Checkbox(flag.ToString(), ref isSelected)) {
						if (isSelected) {
							overlay.VisibilityShow |= flag;
						}
						else {
							overlay.VisibilityShow ^= flag;
						}
					}
				}

				ImGui.EndCombo();
			}

			var ovHide = overlay.VisibilityHide;
			var flagString = ovHide == 0 ? "" : ovHide.ToString();
			if (ImGui.BeginCombo("Hide When", flagString)) {
				foreach (var flag in visFlags) {
					var isSelected = ovHide.HasFlag(flag);
					if (ImGui.Checkbox(flag.ToString(), ref isSelected)) {
						if (isSelected) {
							overlay.VisibilityHide |= flag;
						}
						else {
							overlay.VisibilityHide ^= flag;
						}
					}
				}

				ImGui.EndCombo();
			}

			if (ImGui.Button("Reload")) {
				selectedOverlay.Reload();
			}

			ImGui.SameLine();
			if (ImGui.Button("Open Dev Tools")) {
				selectedOverlay.Debug();
			}

			ImGui.SameLine();
			ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(1f, 0, 0, 1));
			if (ImGui.Button("Delete")) {
				PluginLog.Log("Start remove");
				selectedOverlay.Dispose();
			}

			ImGui.PopStyleColor();

			ImGui.PopID();
		}
	}
}