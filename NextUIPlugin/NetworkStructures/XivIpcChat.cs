using System;
using System.Runtime.InteropServices;

namespace NextUIPlugin.NetworkStructures {
	[Serializable]
	[StructLayout(LayoutKind.Sequential)]
	public struct XivIpcChat {
		public unsafe fixed byte padding[14]; //Maybe this is SubCode, or some kind of talker ID...
		public ushort chatType;
		public unsafe fixed byte name[32];
		public unsafe fixed byte msg[1012];
	};
}