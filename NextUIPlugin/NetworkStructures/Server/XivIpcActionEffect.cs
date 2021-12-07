using System;
using System.Runtime.InteropServices;
using Newtonsoft.Json;

namespace NextUIPlugin.NetworkStructures.Server {
	public enum XivIpcActionEffectDisplayType : byte {
		HideActionName = 0,
		ShowActionName = 1,
		ShowItemName = 2,
		MountName = 0x0d
	};

	[Serializable]
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct XivIpcActionEffectHeader {
		public uint animationTargetId; // who the animation targets
		public uint unknown;
		public uint actionId; // what the casting player casts, shown in battle log / ui
		public uint globalEffectCounter;
		public float animationLockTime;
		public uint SomeTargetID;
		public ushort hiddenAnimation; // 0 = show animation, otherwise hide animation.
		public ushort rotation;
		public ushort actionAnimationId;
		public byte variation; // animation
		public XivIpcActionEffectDisplayType effectDisplayType; // is this also item id / mount id?
		public byte unknown20;
		public byte effectCount;
		public ushort padding21;
	}

	[Serializable]
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public unsafe struct XivIpcActionEffect1 {
		public XivIpcActionEffectHeader Header;
		public uint padding1;
		public ushort padding2;
		[MarshalAs(UnmanagedType.ByValArray, SizeConst=16)]
		public uint[] Effects;
		public ushort padding3;
		public uint padding4;
		public ulong targetId;
		public uint padding5;
	}

	[Serializable]
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public unsafe struct XivIpcActionEffect8 {
		public XivIpcActionEffectHeader Header;
		public uint padding1;
		public ushort padding2;

		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 128)]
		public uint[] effects;

		public ushort padding3;
		public uint padding4;

		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
		public ulong[] targetId;

		public uint effectFlags1;
		public ushort effectFlags2;
		public ushort padding5;
		public uint padding6;

		// public ulong[] targetIds {
		// 	get { return StructUtil.FixedArray(targetId, 8); }
		// }
	}

	[Serializable]
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct XivIpcActionEffect16 {
		public XivIpcActionEffectHeader Header;
		public uint padding1;
		public ushort padding2;
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
		public uint[] effects;
		public ushort padding3;
		public uint padding4;

		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
		public ulong[] targetId;

		public uint effectFlags1;
		public ushort effectFlags2;
		public ushort padding5;
		public uint padding6;
	}

	[Serializable]
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public unsafe struct XivIpcActionEffect24 {
		public XivIpcActionEffectHeader Header;
		public uint padding1;
		public ushort padding2;
		public fixed uint Effects[384];
		public ushort padding3;
		public uint padding4;

		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 24)]
		public ulong[] targetId;

		public uint effectFlags1;
		public ushort effectFlags2;
		public ushort padding5;
		public uint padding6;
	}

	[Serializable]
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public unsafe struct XivIpcActionEffect32 {
		public XivIpcActionEffectHeader Header;
		public uint padding1;
		public ushort padding2;
		public fixed uint Effects[512];
		public ushort padding3;
		public uint padding4;

		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
		public ulong[] targetId;

		public uint effectFlags1;
		public ushort effectFlags2;
		public ushort padding5;
		public uint padding6;
	}
}