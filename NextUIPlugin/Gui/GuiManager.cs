﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using Dalamud.Logging;
using Dalamud.Plugin;
using ImGuiNET;
using NextUIPlugin.Overlay;
using NextUIShared;
using D3D11 = SharpDX.Direct3D11;

namespace NextUIPlugin.Gui {
	public class GuiManager : IDisposable, IGuiManager {
		public readonly List<OverlayGui> overlays = new();

		public long AdapterLuid { get; set; }
		public bool MicroPluginFullyLoaded { get; set; }

		public event Action<Guid, NextUIShared.Overlay.Overlay>? RequestNewOverlay;

		public void Initialize(DalamudPluginInterface pluginInterface) {
			// Spin up DX handling from the plugin interface
			DxHandler.Initialize(pluginInterface);
			AdapterLuid = DxHandler.AdapterLuid;

			// Spin up WndProc hook
			WndProcHandler.Initialize(DxHandler.WindowHandle);
			WndProcHandler.WndProcMessage += OnWndProc;
		}

		// Overlay initialization code here, we need to wait till plugin fully loads
		public void MicroPluginLoaded() {
			MicroPluginFullyLoaded = true;
			// loading ov
			PluginLog.Log("OnMicroPluginFullyLoaded");
			CreateOverlay("http://localhost:4200?OVERLAY_WS=ws://127.0.0.1:10501/ws", new Size(1920, 1080));
		}

		protected (bool, long) OnWndProc(WindowsMessage msg, ulong wParam, long lParam) {
			// Notify all the inlays of the wndproc, respond with the first capturing response (if any)
			// TODO: Yeah this ain't great but realistically only one will capture at any one time for now.
			// Revisit if shit breaks or something idfk.
			var responses = overlays.Select(ov => ov.WndProcMessage(msg, wParam, lParam));
			return responses.FirstOrDefault(ov => ov.Item1);
		}

		public OverlayGui? CreateOverlay(string url, Size size) {
			if (!MicroPluginFullyLoaded) {
				PluginLog.Warning("Overlay not created, MicroPlugin not ready");
				return null;
			}

			var overlay = new NextUIShared.Overlay.Overlay(url, size);
			// Data should be populated here ie texture pointer
			RequestNewOverlay?.Invoke(overlay.Guid, overlay);

			var overlayGui = new OverlayGui(overlay);
			overlays.Add(overlayGui);

			return overlayGui;
		}

		public void Render() {
			ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(0, 0));

			foreach (var ov in overlays) {
				ov.Render();
			}

			ImGui.PopStyleVar();
		}

		public void Dispose() {
			foreach (var ov in overlays) {
				ov.Dispose();
			}

			WndProcHandler.Shutdown();
			DxHandler.Shutdown();
		}
	}
}