using System;
using System.Runtime.InteropServices;
using NextUIPlugin.NetworkStructures.Common;

namespace NextUIPlugin.NetworkStructures.Server {
	[Serializable]
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct XivIpcActorMove {
		public byte headRotation;
		public byte rotation;
		public byte animationType;
		public byte animationState;
		public byte animationSpeed;
		public byte unknownRotation;
		public ShortPosition position;
		// public ushort posX;
		// public ushort posY;
		// public ushort posZ;
		
		
		
		public uint unknown12;
	}
}