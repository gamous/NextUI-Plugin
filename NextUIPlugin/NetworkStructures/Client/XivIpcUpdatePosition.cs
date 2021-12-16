using System;
using System.Runtime.InteropServices;
using NextUIPlugin.NetworkStructures.Common;

namespace NextUIPlugin.NetworkStructures.Client {
	/**
	 * Opcode: 0x0346
	 */
	[Serializable]
	[StructLayout(LayoutKind.Sequential)]
	public struct XivIpcUpdatePosition {
		public float rotation;
		public byte animationType;
		public byte animationState;
		public byte clientAnimationType;
		public byte headPosition;
		public Position position;

		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
		public byte[] unk;
	};
}