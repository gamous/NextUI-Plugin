using System;
using System.Runtime.InteropServices;

namespace NextUIPlugin.NetworkStructures.Server {
	public enum XivIpcActorControlCategory : ushort {
		OverTime = 0x17,
		CancelAbility = 0x0f,
		Death = 0x06,
		TargetIcon = 0x22,
		Tether = 0x23,
		GainEffect = 0x14,
		LoseEffect = 0x15,
		UpdateEffect = 0x16,
		Targetable = 0x36,
		DirectorUpdate = 0x6d,
		SetTargetSign = 0x1f6,
		LimitBreak = 0x1f9
	};

	[Serializable]
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct XivIpcActorControl {
		public XivIpcActorControlCategory category;
		public ushort padding;
		public uint param1;
		public uint param2;
		public uint param3;
		public uint param4;
		public uint padding1;
	}

	[Serializable]
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct XivIpcActorControlSelf {
		public XivIpcActorControlCategory category;
		public ushort padding;
		public uint param1;
		public uint param2;
		public uint param3;
		public uint param4;
		public uint param5;
		public uint param6;
		public uint padding1;
	}

	[Serializable]
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct XivIpcActorControlTarget {
		public XivIpcActorControlCategory category;
		public ushort padding;
		public uint param1;
		public uint param2;
		public uint param3;
		public uint param4;
		public uint padding1;
		public uint targetId;
		public uint padding2;
	}
}