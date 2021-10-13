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
		protected RpcBuffer rpc;
		protected bool running;

		protected string keepAliveHandleName;
		protected string ipcChannelName;

		public RenderProcess(
			int pid,
			string dir
		) {
			keepAliveHandleName = $"NURendererKeepAlive{pid}";
			ipcChannelName = $"NURendererIpcChannel{pid}";

			rpc = new RpcBuffer(ipcChannelName + "z", (_, data) => {
				string str = System.Text.Encoding.UTF8.GetString(data);
				UpstreamIpcRequest? decoded = UpstreamIpcRequest.FromJson(str);
				if (decoded == null) {
					return Array.Empty<byte>();
				}

				byte[]? resp = Receive?.Invoke(decoded);
				return resp ?? Array.Empty<byte>();
			});

			string[] renderArgs = {
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

			process.OutputDataReceived += (_, args) => PluginLog.Log($"[NuRender]: {args.Data}");
			process.ErrorDataReceived += (_, args) => PluginLog.LogError($"[NuRender]: {args.Data}");
		}

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
			string serialized = JsonConvert.SerializeObject(request);
			byte[] data = System.Text.Encoding.UTF8.GetBytes(serialized);
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
			rpc.Dispose();
		}
	}
}