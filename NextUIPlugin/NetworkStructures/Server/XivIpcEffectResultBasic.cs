using System;
using System.Runtime.InteropServices;
using Newtonsoft.Json;

namespace NextUIPlugin.NetworkStructures.Server {
	[Serializable]
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct XivIpcEffectResultBasic {
		public uint unknown1;
		public uint relatedActionSequence;
		public uint actorId;
		public uint currentHp;
		public uint unknown2;
		public ushort unknown3;
		public ushort unknown4;
	}
}