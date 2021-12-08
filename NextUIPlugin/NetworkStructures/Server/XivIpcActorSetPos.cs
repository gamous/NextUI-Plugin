using System;
using System.Runtime.InteropServices;
using NextUIPlugin.NetworkStructures.Common;

namespace NextUIPlugin.NetworkStructures.Server {
	[Serializable]
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct XivIpcActorSetPos {
		public ushort r16;
		public byte waitForLoad;
		public byte unknown1;
		public uint unknown2;

		public Position position;

		// public float x;
		// public float y;
		// public float z;
		public uint unknown3;
	}
}