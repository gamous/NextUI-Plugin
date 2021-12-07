using System;
using System.Runtime.InteropServices;
using NextUIPlugin.NetworkStructures.Common;
using Newtonsoft.Json;

namespace NextUIPlugin.NetworkStructures.Client {
	/**
	 * Opcode: 0x01CC
	 */
	[Serializable]
	[StructLayout(LayoutKind.Sequential)]
	public struct XIVIpcChatHandler {
		[JsonIgnore]
		public uint unknown1;

		public uint sourceId;
		
		public Position position;
		public uint chatType;
		public ushort unknown2;

		[JsonIgnore]
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 1024)]
		public byte[] messageRaw;

		// ReSharper disable once InconsistentNaming
		public string message {
			get { return StructUtil.FixedUTF8String(messageRaw); }
		}
	};
}