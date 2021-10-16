using System;
using System.Collections.Generic;
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
		protected Dictionary<Guid, OverlayGui> overlays = new();

		public long AdapterLuid { get; set; }

		public Action<Guid, NextUIShared.Overlay.Overlay> RequestNewOverlay { get; set; }

		public void Initialize(DalamudPluginInterface pluginInterface) {
			// Spin up DX handling from the plugin interface
			DxHandler.Initialize(pluginInterface);
			AdapterLuid = DxHandler.AdapterLuid;

			// Spin up WndProc hook
			WndProcHandler.Initialize(DxHandler.WindowHandle);
			WndProcHandler.WndProcMessage += OnWndProc;
			
			// TODO: REMOVE, FOR TEST
			CreateOverlay("http://localhost:4200?OVERLAY_WS=ws://127.0.0.1:10501/ws");
		}

		protected (bool, long) OnWndProc(WindowsMessage msg, ulong wParam, long lParam) {
			// Notify all the inlays of the wndproc, respond with the first capturing response (if any)
			// TODO: Yeah this ain't great but realistically only one will capture at any one time for now.
			// Revisit if shit breaks or something idfk.
			var responses = overlays.Select(pair => pair.Value.WndProcMessage(msg, wParam, lParam));
			return responses.FirstOrDefault(pair => pair.Item1);
		}

		protected void CreateOverlay(string url) {
			var overlay = new NextUIShared.Overlay.Overlay(url);
			// Data should be populated here ie texture pointer
			RequestNewOverlay?.Invoke(overlay.Guid, overlay);

			var overlayGui = new OverlayGui(overlay);
			overlays.Add(overlayGui.overlay.Guid, overlayGui);
		}

		public void Render() {
			ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(0, 0));

			foreach (var valuePair in overlays) {
				valuePair.Value.Render();
			}

			ImGui.PopStyleVar();
		}

		public void Dispose() {
			foreach (var valuePair in overlays) {
				valuePair.Value.Dispose();
			}

			WndProcHandler.Shutdown();
			DxHandler.Shutdown();
		}
	}
}