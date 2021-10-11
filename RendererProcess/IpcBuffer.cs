using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;
using RendererProcess.Ipc;
using SharedMemory;

namespace RendererProcess {
	public class IpcResponse<TResponse> {
		public bool Success;
		public TResponse Data;
	}

	public class IpcBuffer<TIncoming, TOutgoing> : RpcBuffer {
		protected static readonly BinaryFormatter formatter = new();

		// Handle conversion between wire's byte[] and nicer clr types
		protected static Func<ulong, byte[], byte[]> CallbackFactory(Func<TIncoming, object> callback) {
			return (messageId, rawRequest) => {
				TIncoming? request = Decode<TIncoming>(rawRequest);
				object? response = callback(request);

				return response == null ? null : Encode(response);
			};
		}

		public IpcBuffer(string name, Func<TIncoming, object?> callback) : base(name, CallbackFactory(callback)) {
		}

		public IpcResponse<TResponse?> RemoteRequest<TResponse>(TOutgoing request, int timeout = 5000) {
			byte[] rawRequest = Encode(request);
			RpcResponse? rawResponse = RemoteRequest(rawRequest, timeout);
			return new IpcResponse<TResponse?> {
				Success = rawResponse.Success,
				Data = rawResponse.Success ? Decode<TResponse>(rawResponse.Data) : default,
			};
		}

		public async Task<IpcResponse<TResponse?>> RemoteRequestAsync<TResponse>(TOutgoing request, int timeout = 5000) {
			byte[] rawRequest = Encode(request);
			RpcResponse? rawResponse = await RemoteRequestAsync(rawRequest, timeout);
			return new IpcResponse<TResponse?> {
				Success = rawResponse.Success,
				Data = rawResponse.Success ? Decode<TResponse>(rawResponse.Data) : default,
			};
		}

		protected static byte[] Encode<T>(T value) {
			byte[] encoded;
			using (MemoryStream stream = new()) {
				formatter.Serialize(stream, value);
				encoded = stream.ToArray();
			}

			return encoded;
		}

		protected static T? Decode<T>(byte[]? encoded) {
			if (encoded == null) {
				return default;
			}

			T value;
			using (MemoryStream stream = new(encoded)) {
				value = (T)formatter.Deserialize(stream);
			}

			return value;
		}
	}
}