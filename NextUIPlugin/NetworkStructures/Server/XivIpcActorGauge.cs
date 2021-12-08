using System;
using System.Runtime.InteropServices;
using Newtonsoft.Json;

namespace NextUIPlugin.NetworkStructures.Server {
	[Serializable]
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct XivIpcActorGauge {
		public byte classJobId;

		[JsonConverter(typeof(ByteArrayConverter))]
		[MarshalAs(UnmanagedType.ByValArray, SizeConst=15)]
		public byte[] data; // depends on classJobId
	}
}