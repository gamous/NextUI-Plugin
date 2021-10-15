using System;
using System.Runtime.InteropServices;

namespace NextUIPlugin.NetworkStructures {
	[Serializable]
	[StructLayout(LayoutKind.Sequential)]
	public struct XivIpcPlayerStateFlags {
		public unsafe fixed byte flags[12];
		uint padding;
	}
}