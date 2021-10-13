using System;
using Newtonsoft.Json;

namespace RendererProcess {
	public static class JSerializer {
		public static byte[] Serialize(object? obj) {
			if (obj == null) {
				return Array.Empty<byte>();
			}

			string serialized = JsonConvert.SerializeObject(obj);
			return System.Text.Encoding.UTF8.GetBytes(serialized);
		}

		public static object? Deserialize<T>(byte[] data) {
			if (data.Length == 0) {
				return null;
			}

			string obj = System.Text.Encoding.UTF8.GetString(data);
			return JsonConvert.DeserializeObject<T>(obj);
		}
	}
}