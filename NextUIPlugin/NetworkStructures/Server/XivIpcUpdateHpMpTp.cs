using System;
using System.Runtime.InteropServices;

namespace NextUIPlugin.NetworkStructures.Server {
	[Serializable]
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct XivIpcUpdateHpMpTp {
		public uint currentHp;
		public ushort currentMp;
		public ushort unknown1;
	}
}