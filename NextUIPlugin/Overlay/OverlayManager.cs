using System;
using System.Numerics;
using Dalamud.Logging;
using Dalamud.Plugin;
using ImGuiNET;
using RendererProcess.Ipc;

namespace NextUIPlugin.Overlay {
	public class OverlayManager : IDisposable {
		protected RenderProcess renderProcess;
		protected Overlay? overlay;

		public void Initialize(DalamudPluginInterface pluginInterface) {
			// Spin up DX handling from the plugin interface
			DxHandler.Initialize(pluginInterface);

			// Spin up WndProc hook
			WndProcHandler.Initialize(DxHandler.WindowHandle);
			WndProcHandler.WndProcMessage += OnWndProc;

			// Boot the render process. This has to be done before initialising settings to prevent a
			// race conditionson inlays recieving a null reference.
			// var pid = Process.GetCurrentProcess().Id;
			int pid = Environment.ProcessId;
			string dir = pluginInterface.GetPluginLocDirectory();
			PluginLog.Log("TOCOP " + dir);
			renderProcess = new RenderProcess(pid,
				"A:\\Projects\\Kaminaris\\ffxiv\\NextUIPlug\\NextUIPlugin\\RendererProcess\\bin\\x64\\Release\\win-x64");
			renderProcess.Receive += HandleIpcRequest;
			renderProcess.Start();

			// Prep settings
			// settings = pluginInterface.Create<Settings>();
			// settings.InlayAdded += OnInlayAdded;
			// settings.InlayNavigated += OnInlayNavigated;
			// settings.InlayDebugged += OnInlayDebugged;
			// settings.InlayRemoved += OnInlayRemoved;
			// settings.TransportChanged += OnTransportChanged;
			// settings.Initialise();
		}

		protected (bool, long) OnWndProc(WindowsMessage msg, ulong wParam, long lParam) {
			// Notify all the inlays of the wndproc, respond with the first capturing response (if any)
			// TODO: Yeah this ain't great but realistically only one will capture at any one time for now. Revisit if shit breaks or something idfk.
			// var responses = inlays.Select(pair => pair.Value.WndProcMessage(msg, wParam, lParam));
			// return responses.FirstOrDefault(pair => pair.Item1);
			return overlay?.WndProcMessage(msg, wParam, lParam) ?? default;
		}

		protected byte[] HandleIpcRequest(UpstreamIpcRequest request) {
			PluginLog.Log($"Got SOME Request {request.reqType}");
			switch (request.reqType) {
				case "ready": // ReadyNotificationRequest readyNotificationRequest
					PluginLog.Log("Got Ready Ev");
					//settings.SetAvailableTransports(readyNotificationRequest.availableTransports);
					// settings.HydrateInlays();
					CreateOverlay();
					return Array.Empty<byte>();


				case "setCursor" : //SetCursorRequest setCursorRequest
					// TODO: Integrate ideas from Bridge re: SoC between widget and inlay
					// var inlay = inlays.Values.Where(inlay => inlay.RenderGuid == setCursorRequest.Guid)
					// 	.FirstOrDefault();
					// if (inlay == null) {
					// 	return null;
					// }

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

		public void Dispose() {
			overlay?.Dispose();
			renderProcess?.Dispose();

			WndProcHandler.Shutdown();
			DxHandler.Shutdown();
		}
	}
}