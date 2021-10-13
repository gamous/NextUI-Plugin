using System;
using System.Numerics;
using Dalamud.Logging;
using Dalamud.Plugin;
using ImGuiNET;
using RendererProcess.Ipc;

namespace NextUIPlugin.Overlay {
	public class OverlayManager : IDisposable {
		protected RenderProcess? renderProcess;
		protected Overlay? overlay;

		public void Initialize(DalamudPluginInterface pluginInterface) {
			// Spin up DX handling from the plugin interface
			DxHandler.Initialize(pluginInterface);

			// Spin up WndProc hook
			WndProcHandler.Initialize(DxHandler.WindowHandle);
			WndProcHandler.WndProcMessage += OnWndProc;

			// Boot the render process. This has to be done before initialising settings to prevent a
			// race conditions on inlays receiving a null reference.
			// var pid = Process.GetCurrentProcess().Id;
			int pid = Environment.ProcessId;
			string? dir = pluginInterface.AssemblyLocation.DirectoryName;
			if (dir == null) {
				PluginLog.Log("AssemblyLocation not available!");
				return;
			}

			renderProcess = new RenderProcess(pid, dir);
			renderProcess.Receive += HandleIpcRequest;
			renderProcess.Start();
		}

		protected (bool, long) OnWndProc(WindowsMessage msg, ulong wParam, long lParam) {
			return overlay?.WndProcMessage(msg, wParam, lParam) ?? default;
		}

		protected byte[] HandleIpcRequest(UpstreamIpcRequest request) {
			switch (request.reqType) {
				case "ready":
					CreateOverlay();
					return Array.Empty<byte>();

				case "setCursor":
					overlay?.SetCursor(((SetCursorRequest)request).cursor);
					return Array.Empty<byte>();

				default:
					throw new Exception($"Unknown IPC request type {request.GetType().Name} received.");
			}
		}

		protected void CreateOverlay() {
			overlay = new Overlay(renderProcess);
		}

		public void Render() {
			ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(0, 0));

			overlay?.Render();

			ImGui.PopStyleVar();
		}

		public void Debug() {
			overlay?.Debug();
		}

		public void Navigate(string url) {
			overlay?.Navigate(url);
		}

		public void SetAcceptFocus(bool accept) {
			if (overlay != null) {
				overlay.acceptFocus = accept;
			}
		}

		public void Dispose() {
			overlay?.Dispose();
			renderProcess?.Dispose();

			WndProcHandler.Shutdown();
			DxHandler.Shutdown();
		}
	}
}