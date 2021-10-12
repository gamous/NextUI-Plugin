using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using RendererProcess.Ipc;
using RendererProcess.RenderHandlers;
using SharedMemory;
using Newtonsoft.Json;

namespace RendererProcess {
	class Program {
		// protected static string cefAssemblyDir;
		// protected static string dalamudAssemblyDir;

		protected static Thread parentWatchThread;
		protected static EventWaitHandle waitHandle;
		protected static Overlay? overlay;

		// protected static IpcBuffer<DownstreamIpcRequest, UpstreamIpcRequest> ipcBuffer;
		protected static RpcBuffer rpcBuffer = null!;

		protected static void Main(string[] rawArgs) {
			Console.WriteLine("Render process running.");
			AppDomain.CurrentDomain.AssemblyResolve += CustomAssemblyResolver;

			Run(
				int.Parse(rawArgs[0]),
				long.Parse(rawArgs[1]),
				rawArgs[2],
				rawArgs[3]
			);
		}

		// Main process logic. Seperated to ensure assembly resolution is configured.
		[MethodImpl(MethodImplOptions.NoInlining)]
		protected static void Run(
			int parentPid,
			long adapterLuid,
			string ipcChannelName,
			string keepAliveHandleName
		) {
			waitHandle = new EventWaitHandle(false, EventResetMode.ManualReset, keepAliveHandleName);

			// Boot up a thread to make sure we shut down if parent dies
			parentWatchThread = new Thread(WatchParentStatus);
			parentWatchThread.Start(parentPid);

#if DEBUG
			AppDomain.CurrentDomain.FirstChanceException += (obj, e) => Console.Error.WriteLine(e.Exception.ToString());
#endif
			string cacheDir = Path.Combine(
				Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
				"NUCefSharp\\Cache"
			);
			bool dxRunning = DxHandler.Initialize(adapterLuid);
			CefHandler.Initialize(cacheDir);

			// ipcBuffer = new IpcBuffer<DownstreamIpcRequest, UpstreamIpcRequest>(ipcChannelName, HandleIpcRequest);
			rpcBuffer = new RpcBuffer(ipcChannelName + "z", (msgId, data) => {
				var str = System.Text.Encoding.UTF8.GetString(data);
				var decoded = DownstreamIpcRequest.FromJson(str);
				if (decoded == null) {
					return Array.Empty<byte>();
				}

				Console.WriteLine("SUB received" + str);
				return HandleIpcRequest(decoded);
			});

			Console.WriteLine($"Render starting PPID: {parentPid}, LUID: {adapterLuid}, IPCC: {ipcChannelName} KAHN: {keepAliveHandleName}");
			Console.WriteLine("Notifying on ready state.");

			// Send info that renderer is ready
			// ipcBuffer.RemoteRequest<object>(new ReadyNotificationRequest());
			var response = rpcBuffer.RemoteRequest(
				System.Text.Encoding.UTF8.GetBytes(
					JsonConvert.SerializeObject(new ReadyNotificationRequest())
				)
			);
			Console.WriteLine("RPC REQ SENT." ); // + System.Text.Encoding.UTF8.GetString(response.Data)
			
			Console.WriteLine("Waiting...");

			waitHandle.WaitOne();
			Console.WriteLine("Waiting2");
			waitHandle.Dispose();
			Console.WriteLine("Waiting3");

			Console.WriteLine("Render process shutting down.");

			// ipcBuffer.Dispose();
			rpcBuffer.Dispose();

			DxHandler.Shutdown();
			CefHandler.Shutdown();

			// TODO: what?
			// parentWatchThread.Abort();
		}

		protected static void WatchParentStatus(object? pid) {
			if (pid == null) {
				throw new Exception("Parent PID not set!");
			}

			Console.WriteLine($"Watching parent PID {pid}");
			Process process = Process.GetProcessById((int)pid);
			process.WaitForExit();
			waitHandle.Set();

			Process self = Process.GetCurrentProcess();
			self.WaitForExit(1000);
			try {
				self.Kill();
			}
			catch (InvalidOperationException) {
			}
		}

		protected static byte[] HandleIpcRequest(DownstreamIpcRequest request) {
			Console.WriteLine($"Got request {request.GetType()}.");
			switch (request.reqType) {
				case "new":
					var resp = OnNewInlayRequest();
					var serialized = JsonConvert.SerializeObject(resp);
					return System.Text.Encoding.UTF8.GetBytes(serialized);

				// don't need this
				// case "resizeInlayRequest": 
				// 	var inlay = inlays[resizeInlayRequest.Guid];
				// 	if (inlay == null) {
				// 		return null;
				// 	}
				//
				// 	inlay.Resize(new Size(resizeInlayRequest.Width, resizeInlayRequest.Height));
				//
				// 	return BuildRenderHandlerResponse(inlay.RenderHandler);
				//

				// dont need this
				// case NavigateInlayRequest navigateInlayRequest: {
				// 	var inlay = inlays[navigateInlayRequest.Guid];
				// 	inlay.Navigate(navigateInlayRequest.Url);
				// 	return null;
				// }

				case "debug":
					overlay?.Debug();
					return Array.Empty<byte>();


				// case RemoveInlayRequest removeInlayRequest: {
				// 	var inlay = inlays[removeInlayRequest.Guid];
				// 	inlays.Remove(removeInlayRequest.Guid);
				// 	inlay.Dispose();
				// 	return null;
				// }

				case "mouseEvent": // MouseEventRequest mouseEventRequest
					overlay?.HandleMouseEvent((MouseEventRequest)request);
					return Array.Empty<byte>();

				case "keyEvent": // KeyEventRequest keyEventRequest
					overlay?.HandleKeyEvent((KeyEventRequest)request);
					return Array.Empty<byte>();

				default:
					throw new Exception($"Unknown IPC request type {request.reqType} received.");
			}
		}

		protected static TextureHandleResponse OnNewInlayRequest() {
			// TODO: Fix this?
			Size size = new(1920, 1080);
			TextureRenderHandler renderHandler = new(size);

			string url = "http://localhost:4200?OVERLAY_WS=ws://127.0.0.1:10501/ws";
			// string url = "https://www.google.com/";
			overlay = new(
				url,
				renderHandler
			);
			Console.WriteLine("SET URL TO " + url);
			overlay.Initialize();
			Console.WriteLine("Overlay initialized ");
			//inlays.Add(request.Guid, inlay);
			
			renderHandler.CursorChanged += (sender, cursor) => {
				var req = new SetCursorRequest { cursor = cursor };
				var des = JsonConvert.SerializeObject(req);
				var rr = System.Text.Encoding.UTF8.GetBytes(des);
				rpcBuffer.RemoteRequest(rr);
				// ipcBuffer.RemoteRequest<object>(new SetCursorRequest {
				// 	cursor = cursor
				// });
			};

			return BuildRenderHandlerResponse(renderHandler);
		}

		protected static TextureHandleResponse BuildRenderHandlerResponse(TextureRenderHandler renderHandler) {
			return new TextureHandleResponse {
				TextureHandle = renderHandler.SharedTextureHandle
			};
		}

		protected static Assembly CustomAssemblyResolver(object sender, ResolveEventArgs args) {
			string? assemblyName = args.Name.Split(new[] { ',' }, 2)[0] + ".dll";

			string assemblyPath = null;
			// if (assemblyName.StartsWith("CefSharp")) {
			// 	assemblyPath = Path.Combine(cefAssemblyDir, assemblyName);
			// }
			// else 
			if (
				assemblyName.StartsWith("SharpDX") ||
				assemblyName.StartsWith("Newtonsoft")
			) {
				string dalamudAssemblyDir = Path.Combine(
					Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
					"XIVLauncher",
					"addon",
					"Hooks",
					"dev"
				);

				assemblyPath = Path.Combine(dalamudAssemblyDir, assemblyName);
			}

			if (assemblyPath == null) {
				return null;
			}

			if (!File.Exists(assemblyPath)) {
				Console.Error.WriteLine($"Could not find assembly `{assemblyName}` at search path `{assemblyPath}`");
				return null;
			}

			return Assembly.LoadFile(assemblyPath);
		}
	}
}