using System;
using System.Runtime.InteropServices;
using Newtonsoft.Json;
using NextUIPlugin.NetworkStructures.Common;

namespace NextUIPlugin.NetworkStructures.Server {
	[Serializable]
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct XivIpcPlayerSpawn {
		public ushort title;
		public ushort u1b;
		public ushort currentWorldId;
		public ushort homeWorldId;

		public byte gmRank;
		public byte u3c;
		public byte u4;
		public byte onlineStatus;

		public byte pose;
		public byte u5a;
		public byte u5b;
		public byte u5c;

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
		public uint u18;
		public uint u19;
		public uint directorId;
		public uint ownerId;
		public uint u22;
		public uint hPMax;
		public uint hPCurr;
		public uint displayFlags;
		public ushort fateID;
		public ushort mPCurr;
		public ushort mPMax;
		public ushort unk; // == 0
		public ushort modelChara;
		public ushort rotation;
		public ushort currentMount;
		public ushort activeMinion;
		public byte spawnIndex;
		public byte state;
		public byte persistentEmote;
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
		
		public Position pos;
		
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)]
		public uint[] models;
		
		[JsonIgnore]
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
		public byte[] nameRaw;

		// ReSharper disable once InconsistentNaming
		public string name {
			get { return StructUtil.FixedUTF8String(nameRaw); }
		}
		
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 26)]
		public byte[] look;
		
		[JsonIgnore]
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
		public byte[] fcTagRaw;

		// ReSharper disable once InconsistentNaming
		public string fcTag {
			get { return StructUtil.FixedUTF8String(fcTagRaw); }
		}

		[JsonIgnore]
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
		public uint[] unk30;
	}
}