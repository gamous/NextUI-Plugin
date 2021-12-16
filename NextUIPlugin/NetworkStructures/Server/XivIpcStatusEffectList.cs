using System;
using System.Linq;
using System.Runtime.InteropServices;
using Newtonsoft.Json;
using NextUIPlugin.NetworkStructures.Common;

namespace NextUIPlugin.NetworkStructures.Server {
	[Serializable]
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct XivIpcStatusEffectList {
		public byte jobId;
		public byte level1;
		public byte level2;
		public byte level3;
		public uint hp;
		public uint hpMax;
		public ushort mana;
		public ushort manaMax;
		public byte damageShield;
		public ushort unknown1; // used to be TP
		public byte unknown2;

		[JsonIgnore]
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 30)]
		public StatusEffect[] effectsRaw; // [30 * 3 * 4]
		// 4 bytes padding at end?

		// ReSharper disable once InconsistentNaming
		public StatusEffect[] effects {
			get { return effectsRaw.Where(e => e.effectId > 0).ToArray(); }
		}
	}

	[Serializable]
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct XivIpcStatusEffectList2 {
		public uint unknown3;
		public byte jobId;
		public byte level1;
		public byte level2;
		public byte level3;
		public uint hp;
		public uint hpMax;
		public ushort mana;
		public ushort manaMax;
		public byte damageShield;
		public ushort unknown1; // used to be TP
		public byte unknown2;

		[JsonIgnore]
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 30)]
		public StatusEffect[] effectsRaw; // [30 * 3 * 4]
		// 4 bytes padding at end?

		// ReSharper disable once InconsistentNaming
		public StatusEffect[] effects {
			get { return effectsRaw.Where(e => e.effectId > 0).ToArray(); }
		}
	}

	[Serializable]
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct XivIpcStatusEffectList3 {
		[JsonIgnore]
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 30)]
		public StatusEffect[] effectsRaw; // [30 * 3 * 4]
		// 4 bytes padding at end?

		// ReSharper disable once InconsistentNaming
		public StatusEffect[] effects {
			get { return effectsRaw.Where(e => e.effectId > 0).ToArray(); }
		}
	}
}