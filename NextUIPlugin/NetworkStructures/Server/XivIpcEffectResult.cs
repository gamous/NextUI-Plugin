using System;
using System.Runtime.InteropServices;
using Newtonsoft.Json;

namespace NextUIPlugin.NetworkStructures.Server {
	[Serializable]
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct StatusEntry {
		public byte index; // which position do i display this
		[JsonIgnore]
		public byte unknown1;
		public ushort id;
		[JsonIgnore]
		public ushort unknown2;
		[JsonIgnore]
		public ushort unknown3; // Sort this out (old right half of power/param property)
		public float duration;
		public uint sourceActorId;
	}

	[Serializable]
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct XivIpcEffectResult {
		public uint globalSequence;
		public uint relatedActionSequence;
		public uint actorId;
		public uint currentHp;
		public uint maxHp;
		public ushort currentMp;
		[JsonIgnore]
		public ushort unknown3;
		public byte damageShield;
		public byte effectCount;
		[JsonIgnore]
		public ushort unknown4;

		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
		public StatusEntry[] statusEntries;
	}
}