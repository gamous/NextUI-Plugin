using Dalamud.Logging;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RendererProcess.Ipc;
using SharedMemory;

namespace NextUIPlugin.Overlay {
	public class RenderProcess : IDisposable {
		public delegate byte[] ReceiveEventHandler(UpstreamIpcRequest request);

		public event ReceiveEventHandler Receive;

		protected Process process;
		// protected IpcBuffer<UpstreamIpcRequest, DownstreamIpcRequest> ipc;
		protected RpcBuffer rpc;
		protected bool running;

		protected string keepAliveHandleName;
		protected string ipcChannelName;
		protected ThreadStart ths;

		public RenderProcess(
			int pid,
			string dir
		) {
			keepAliveHandleName = $"NURendererKeepAlive{pid}";
			ipcChannelName = $"NURendererIpcChannel{pid}";

			// ipc = new IpcBuffer<UpstreamIpcRequest, DownstreamIpcRequest>(
			// 	ipcChannelName,
			// 	request => Receive?.Invoke(this, request)
			// );
			rpc = new RpcBuffer(ipcChannelName + "z", (msgId, data) => {
				var str = System.Text.Encoding.UTF8.GetString(data);
				var decoded = UpstreamIpcRequest.FromJson(str);
				if (decoded == null) {
					return Array.Empty<byte>();
				}

				PluginLog.Log("MAIN received" + str + " " + decoded.reqType + " " + decoded.GetType());
				var resp = Receive?.Invoke(decoded);
				PluginLog.Log("MAIN did" + resp);

				return resp;
			});
			

			// var cefAssemblyDir = dependencyManager.GetDependencyPathFor("cef");
			// var processArgs = new RenderProcessArguments() {
			// 	ParentPid = pid,
			// 	DalamudAssemblyDir = Path.GetDirectoryName(typeof(PluginLog).Assembly.Location),
			// 	CefAssemblyDir = cefAssemblyDir,
			// 	CefCacheDir = Path.Combine(configDir, "cef-cache"),
			// 	DxgiAdapterLuid = DxHandler.AdapterLuid,
			// 	KeepAliveHandleName = keepAliveHandleName,
			// 	IpcChannelName = ipcChannelName,
			// };

			// int parentPid,
			// long adapterLuid,
			// string ipcChannelName,
			// string keepAliveHandleName
			
			string[] renderArgs = new[] {
				pid.ToString(),
				DxHandler.AdapterLuid.ToString(),
				ipcChannelName,
				keepAliveHandleName
			};

			process = new Process();
			PluginLog.Log("CWD " + dir);
			PluginLog.Log("ARGS " + string.Join(' ', renderArgs));
			process.StartInfo = new ProcessStartInfo() {
				FileName = Path.Combine(dir, "NextUIRenderer.exe"),
				Arguments = string.Join(' ', renderArgs),
				UseShellExecute = false,
				CreateNoWindow = true,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
			};

			process.OutputDataReceived += (sender, args) => PluginLog.Log($"[NuRender]: {args.Data}");
			process.ErrorDataReceived += (sender, args) => PluginLog.LogError($"[NuRender]: {args.Data}");
			
		}
		//
		// protected byte[] HandleRpcRequest(ulong msgId, byte[] data) {
		// 	Console.WriteLine("REC MASTER: " + msgId + " " + System.Text.Encoding.UTF8.GetString(data));
		// 	// rpc.RemoteRequest(System.Text.Encoding.UTF8.GetBytes("valuex"));
		// 	return System.Text.Encoding.UTF8.GetBytes("valuex master");
		// }

		public void Start() {
			if (running) {
				return;
			}

			running = true;

			process.Start();
			process.BeginOutputReadLine();
			process.BeginErrorReadLine();
			PluginLog.Log("Process started");
		}

		public void Send(DownstreamIpcRequest request) {
			SendAsync(request);
		}

		// TODO: Option to wrap this func in an async version?
		public Task<RpcResponse> SendAsync(DownstreamIpcRequest request) {
			var serialized = JsonConvert.SerializeObject(request);
			var data = System.Text.Encoding.UTF8.GetBytes(serialized);
			return rpc.RemoteRequestAsync(data);
		}

		public void Stop() {
			if (!running) {
				return;
			}

			running = false;

			// Grab the handle the process is waiting on and open it up
			EventWaitHandle handle = new(false, EventResetMode.ManualReset, keepAliveHandleName);
			handle.Set();
			handle.Dispose();

			// Give the process a sec to gracefully shut down, then kill it
			process.WaitForExit(1000);
			try {
				process.Kill();
			}
			catch (InvalidOperationException) {
			}
		}

		public void Dispose() {
			Stop();

			process.Dispose();
			// ipc.Dispose();
			rpc.Dispose();
		}
	}
}