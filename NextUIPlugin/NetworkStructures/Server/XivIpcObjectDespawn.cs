using System;
using System.Runtime.InteropServices;

namespace NextUIPlugin.NetworkStructures.Server {
	/**
	 * Sent when actor is suppose to be despawned ex sending away summoner pet
	 */
	[Serializable]
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct XivIpcObjectDespawn {
		public byte spawnId;
		public byte unknown1;
		public ushort unknown2;
		public uint actorId;
	}
}