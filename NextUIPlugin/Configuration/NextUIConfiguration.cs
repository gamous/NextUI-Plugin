using System;
using System.Collections.Generic;
using System.Drawing;
using Dalamud.Configuration;
using Dalamud.Logging;
using ImGuiNET;
using NextUIShared.Model;

namespace NextUIPlugin.Configuration {
	[Serializable]
	// ReSharper disable once InconsistentNaming
	public class NextUIConfiguration : IPluginConfiguration {
		public int Version { get; set; } = 3;
		public int socketPort = 32805;
		public bool firstInstalled = true;
		public List<OverlayConfig> overlays = new();

		public void PrepareConfiguration() {
			if (socketPort is <= 1024 or > short.MaxValue) {
				PluginLog.Log("Resetting port to 32805");
				socketPort = 32805;
			}

			if (firstInstalled) {
				var ov = new Overlay(
					"https://kaminaris.github.io/Next-UI/?OVERLAY_WS=ws://127.0.0.1:10501/ws",
					new Size(800, 600)
				);
				var fsSize = ImGui.GetMainViewport().Size;
				ov.FullScreenSize = new Size((int)fsSize.X, (int)fsSize.Y);
				ov.FullScreen = true;
				ov.Name = "NextUI";
				overlays.Add(OverlayConfig.FromOverlay(ov));

				firstInstalled = false;
			}
		}
	}
}