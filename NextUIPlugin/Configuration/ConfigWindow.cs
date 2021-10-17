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

			var selectorWidth = 200;
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

			var buttonWidth = selectorWidth; //  / 2
			if (ImGui.Button("Add", new Vector2(buttonWidth, 0))) {
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


		private static void RenderOverlayConfig() {
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
			
			// Position
			var posX = overlay.Position.X;
			if (ImGui.DragInt("Position X", ref posX, 0.1f)) {
				overlay.Position = new Point(posX, overlay.Position.Y);
			}
			
			// ImGui.SameLine();
			
			var posY = overlay.Position.Y;
			if (ImGui.DragInt("Position Y", ref posY, 0.1f)) {
				overlay.Position = new Point(overlay.Position.X, posY);
			}
			
			// Size
			var sizeW = overlay.Size.Width;
			if (ImGui.DragInt("Width", ref sizeW, 0.1f)) {
				overlay.Size = new Size(sizeW, overlay.Size.Height);
			}
			
			// ImGui.SameLine();
			
			var sizeH = overlay.Size.Height;
			if (ImGui.DragInt("Height", ref sizeH, 0.1f)) {
				overlay.Size = new Size(overlay.Size.Width, sizeH);
			}

			var ovLocked = overlay.Locked;
			if (ImGui.Checkbox("Locked", ref ovLocked)) {
				overlay.Locked = ovLocked;
			}

			if (ImGui.IsItemHovered()) {
				ImGui.SetTooltip("Prevent the overlay from being resized or moved.");
			}

			var ovHidden = overlay.Hidden;
			if (ImGui.Checkbox("Hidden", ref ovHidden)) {
				overlay.Hidden = ovHidden;
			}

			if (ImGui.IsItemHovered()) {
				ImGui.SetTooltip("This does not stop the overlay from executing, only from being displayed.");
			}

			var ovTypeThrough = overlay.TypeThrough;
			if (ImGui.Checkbox("Type Through", ref ovTypeThrough)) {
				overlay.TypeThrough = ovTypeThrough;
			}

			if (ImGui.IsItemHovered()) {
				ImGui.SetTooltip("Prevent the overlay from intercepting any keyboard events.");
			}

			var ovClickThrough = overlay.ClickThrough;
			if (ImGui.Checkbox("Click Through", ref ovClickThrough)) {
				overlay.ClickThrough = ovClickThrough;
			}

			if (ImGui.IsItemHovered()) {
				ImGui.SetTooltip("Prevent the inlay from intercepting any mouse events.");
			}

			if (ImGui.Button("Reload")) {
				selectedOverlay.overlay.Reload();
			}

			ImGui.SameLine();
			if (ImGui.Button("Open Dev Tools")) {
				selectedOverlay.overlay.Debug();
			}

			ImGui.PopID();
		}
	}
}