using System;
using System.Collections.Generic;
using Dalamud.Configuration;
using Dalamud.Logging;
using NextUIShared.Model;

namespace NextUIPlugin.Configuration {
	[Serializable]
	// ReSharper disable once InconsistentNaming
	public class NextUIConfiguration : IPluginConfiguration {
		public int Version { get; set; } = 3;
		public int socketPort = 32805;
		public List<Overlay> overlays = new();

		public void PrepareConfiguration() {
			if (socketPort <= 1024 || socketPort > short.MaxValue) {
				PluginLog.Log("Resetting port to 32805");
				socketPort = 32805;
			}

			// if (overlayUrl == "") {
			// 	// FOR LOCAL V
			// 	overlayUrl = "http://localhost:4200?OVERLAY_WS=ws://127.0.0.1:10501/ws";
			// }
		}
	}
}