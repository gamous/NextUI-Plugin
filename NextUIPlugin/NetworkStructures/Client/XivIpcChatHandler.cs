using System;
using System.Runtime.InteropServices;

namespace NextUIPlugin.NetworkStructures.Client {
	/**
	 * Opcode: 0x03B0
	 */
	[Serializable]
	[StructLayout(LayoutKind.Sequential)]
	public struct XivIpcChatHandler {
		public unsafe fixed byte pad_0000[4];
		public uint sourceId;
		public unsafe fixed byte pad_0008[16];
		public ushort chatType;
		public unsafe fixed byte message[1012];
	}
}