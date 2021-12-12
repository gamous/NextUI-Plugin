using System;
using System.Runtime.InteropServices;
using Newtonsoft.Json;
using NextUIPlugin.NetworkStructures.Common;

namespace NextUIPlugin.NetworkStructures.Server {
	[Serializable]
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct XivIpcActorCast {
		public ushort actionId;

		public byte actionType;

		[JsonIgnore]
		public byte unknown1;

		[JsonIgnore]
		public uint unknown2; // action id or mount id

		public float castTime;
		public uint targetId;

		public float rotation;

		// TODO: No idea about this
		public ushort flag; // 1 = interruptible blinking cast bar

		[JsonIgnore]
		public ushort unknown3;

		public ShortPosition position;

		// public ushort posX;
		// public ushort posY;
		// public ushort posZ;
		[JsonIgnore]
		public ushort unknown4;
	}
}