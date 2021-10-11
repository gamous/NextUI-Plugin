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

namespace RendererProcess {
	class Program {
		// protected static string cefAssemblyDir;
		// protected static string dalamudAssemblyDir;

		protected static Thread parentWatchThread;
		protected static EventWaitHandle waitHandle;
		protected static Overlay? overlay;

		protected static IpcBuffer<DownstreamIpcRequest, UpstreamIpcRequest> ipcBuffer;

		protected static void Main(string[] rawArgs) {
			Console.WriteLine("Render process running.");
			// var args = RenderProcessArguments.Deserialise(rawArgs[0]);

			// Need to pull these out before Run() so the resolver can access.
			// cefAssemblyDir = args.CefAssemblyDir;
			// dalamudAssemblyDir = args.DalamudAssemblyDir;

			// AppDomain.CurrentDomain.AssemblyResolve += CustomAssemblyResolver;

			Run(
				int.Parse(rawArgs[0]),
				int.Parse(rawArgs[1]),
				rawArgs[2],
				rawArgs[3]
			);
		}

		// Main process logic. Seperated to ensure assembly resolution is configured.
		[MethodImpl(MethodImplOptions.NoInlining)]
		protected static void Run(
			int parentPid,
			int adapterLuid,
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

			ipcBuffer = new IpcBuffer<DownstreamIpcRequest, UpstreamIpcRequest>(ipcChannelName, HandleIpcRequest);

			Console.WriteLine("Notifying on ready state.");

			// Send info that renderer is ready
			ipcBuffer.RemoteRequest<object>(new UpstreamIpcRequest {
				type = "ready"
			});

			Console.WriteLine("Waiting...");

			waitHandle.WaitOne();
			waitHandle.Dispose();

			Console.WriteLine("Render process shutting down.");

			ipcBuffer.Dispose();

			DxHandler.Shutdown();
			CefHandler.Shutdown();

			parentWatchThread.Abort();
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

		protected static object? HandleIpcRequest(DownstreamIpcRequest request) {
			switch (request.type) {
				case "newInlayRequest":
					return OnNewInlayRequest();

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
					return null;


				// case RemoveInlayRequest removeInlayRequest: {
				// 	var inlay = inlays[removeInlayRequest.Guid];
				// 	inlays.Remove(removeInlayRequest.Guid);
				// 	inlay.Dispose();
				// 	return null;
				// }

				case "mouseMove":
					overlay?.HandleMouseEvent((MouseEventRequest)request);
					return null;


				case "keyEvent":
					overlay?.HandleKeyEvent((KeyEventRequest)request);
					return null;


				default:
					throw new Exception($"Unknown IPC request type {request?.type} received.");
			}
		}

		protected static object OnNewInlayRequest() {
			// TODO: Fix this?
			Size size = new(1920, 1080);
			TextureRenderHandler renderHandler = new(size);

			overlay = new(
				"http://localhost:4200?OVERLAY_WS=ws://127.0.0.1:10501/ws",
				renderHandler
			);
			overlay.Initialize();
			//inlays.Add(request.Guid, inlay);

			renderHandler.CursorChanged += (sender, cursor) => {
				ipcBuffer.RemoteRequest<object>(new UpstreamIpcRequest {
					type = "setCursor",
					cursor = cursor
				});
			};

			return BuildRenderHandlerResponse(renderHandler);
		}

		protected static object BuildRenderHandlerResponse(TextureRenderHandler renderHandler) {
			return new TextureHandleResponse {
				TextureHandle = renderHandler.SharedTextureHandle
			};
		}

		// protected static Assembly CustomAssemblyResolver(object sender, ResolveEventArgs args) {
		// 	string? assemblyName = args.Name.Split(new[] { ',' }, 2)[0] + ".dll";
		//
		// 	string assemblyPath = null;
		// 	if (assemblyName.StartsWith("CefSharp")) {
		// 		assemblyPath = Path.Combine(cefAssemblyDir, assemblyName);
		// 	}
		// 	else if (assemblyName.StartsWith("SharpDX")) {
		// 		assemblyPath = Path.Combine(dalamudAssemblyDir, assemblyName);
		// 	}
		//
		// 	if (assemblyPath == null) {
		// 		return null;
		// 	}
		//
		// 	if (!File.Exists(assemblyPath)) {
		// 		Console.Error.WriteLine($"Could not find assembly `{assemblyName}` at search path `{assemblyPath}`");
		// 		return null;
		// 	}
		//
		// 	return Assembly.LoadFile(assemblyPath);
		// }
	}
}