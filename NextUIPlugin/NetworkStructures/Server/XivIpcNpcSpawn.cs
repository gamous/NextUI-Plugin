using System;
using System.Runtime.InteropServices;
using Newtonsoft.Json;
using NextUIPlugin.NetworkStructures.Common;

namespace NextUIPlugin.NetworkStructures.Server {

	[Serializable]
	[StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Unicode)]
	public struct XivIpcNpcSpawn {
		public uint gimmickId; // needs to be existing in the map, mob will snap to it
		public byte u2b;
		public byte u2ab;
		public byte gmRank;
		public byte u3b;

		public byte aggressionMode; // 1 passive, 2 aggressive
		public byte onlineStatus;
		public byte u3c;
		public byte pose;

		public uint u4;

		public ulong targetId;
		public uint u6;
		public uint u7;
		public ulong mainWeaponModel;
		public ulong secWeaponModel;
		public ulong craftToolModel;

		public uint u14;
		public uint u15;
		public uint bNPCBase;
		public uint bNPCName;
		public uint levelId;
		public uint u19;
		public uint directorId;
		public uint spawnerId;
		public uint parentActorId;
		public uint hpMax;
		public uint hp;
		public uint displayFlags;
		public ushort fateId;
		public ushort mPCurr;

		[JsonIgnore]
		public ushort unknown1;

		[JsonIgnore]
		public ushort unknown2;

		public ushort modelChara;
		public ushort currentMount;
		public ushort rotation;
		public ushort activeMinion;
		public byte spawnIndex;
		public byte state;
		public byte persistantEmote;
		public byte modelType;
		public byte subtype;
		public byte voice;
		public ushort u25c;
		public byte enemyType;
		public byte level;
		public byte classJob;
		public byte u26d;
		public ushort u27a;
		public byte mountHead;
		public byte mountBody;
		public byte mountFeet;
		public byte mountColor;
		public byte scale;

		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
		public byte[] elementData;

		[JsonIgnore]
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
		public byte[] unknown5;

		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 30)]
		public StatusEffect[] effects;

		public Position position;

		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)]
		public uint[] models;

		[JsonIgnore]
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
		public byte[] nameRaw;

		// ReSharper disable once InconsistentNaming
		public string name {
			get { return StructUtil.FixedUTF8String(nameRaw); }
		}

		[MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.U1, SizeConst = 26)]
		public byte[] look;

		[JsonIgnore]
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
		public byte[] fcTagRaw;

		// ReSharper disable once InconsistentNaming
		public string fcTag {
			get { return StructUtil.FixedUTF8String(fcTagRaw); }
		}

		[JsonIgnore] public uint unk30;
		[JsonIgnore] public uint unk31;
		public byte bNPCPartSlot;
		[JsonIgnore] public byte unk32;
		[JsonIgnore] public ushort unk33;

		[JsonIgnore]
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
		public uint[] unk34;
	}
}