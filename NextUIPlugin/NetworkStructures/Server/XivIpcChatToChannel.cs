using System;
using System.Runtime.InteropServices;
using Newtonsoft.Json;

namespace NextUIPlugin.NetworkStructures.Server {
	[Serializable]
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct XivIpcChatToChannel {
		// TODO: figure this out, size does match
		public uint channelId;
		public uint speakerCharacterId;
		public uint speakerEntityId;
		public uint type;

		[JsonIgnore]
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
		public byte[] speakerNameRaw;

		// ReSharper disable once InconsistentNaming
		public string speakerName {
			get { return StructUtil.FixedUTF8String(speakerNameRaw); }
		}

		[JsonIgnore]
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 1024)]
		public byte[] messageRaw;

		// ReSharper disable once InconsistentNaming
		public string message {
			get { return StructUtil.FixedUTF8String(messageRaw); }
		}
	}
}