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
		public uint someTargetId;
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
	public struct XivIpcActionEffect1 {
		public XivIpcActionEffectHeader header;
		public uint padding1;
		public ushort padding2;

		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
		public uint[] effects;

		public ushort padding3;
		public uint padding4;

		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
		public ulong[] targetIds;

		public uint padding5;
	}

	[Serializable]
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct XivIpcActionEffect8 {
		public XivIpcActionEffectHeader header;
		public uint padding1;
		public ushort padding2;

		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 128)]
		public uint[] effects;

		public ushort padding3;
		public uint padding4;

		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
		public ulong[] targetIds;

		public uint effectFlags1;
		public ushort effectFlags2;
		public ushort padding5;
		public uint padding6;
	}

	[Serializable]
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct XivIpcActionEffect16 {
		public XivIpcActionEffectHeader header;
		public uint padding1;
		public ushort padding2;

		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
		public uint[] effects;

		public ushort padding3;
		public uint padding4;

		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
		public ulong[] targetIds;

		public uint effectFlags1;
		public ushort effectFlags2;
		public ushort padding5;
		public uint padding6;
	}

	[Serializable]
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct XivIpcActionEffect24 {
		public XivIpcActionEffectHeader header;
		public uint padding1;
		public ushort padding2;

		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 384)]
		public uint[] effects;

		public ushort padding3;
		public uint padding4;

		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 24)]
		public ulong[] targetIds;

		public uint effectFlags1;
		public ushort effectFlags2;
		public ushort padding5;
		public uint padding6;
	}

	[Serializable]
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct XivIpcActionEffect32 {
		public XivIpcActionEffectHeader Header;
		public uint padding1;
		public ushort padding2;

		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 512)]
		public uint[] effects;

		public ushort padding3;
		public uint padding4;

		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
		public ulong[] targetIds;

		public uint effectFlags1;
		public ushort effectFlags2;
		public ushort padding5;
		public uint padding6;
	}
}