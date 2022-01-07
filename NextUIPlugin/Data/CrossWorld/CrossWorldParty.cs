using System.Runtime.InteropServices;

namespace NextUIPlugin.Data.CrossWorld {
	[StructLayout(LayoutKind.Explicit, Size = 8 + 80 * 8)]
	public struct CrossWorldParty {
		[FieldOffset(0)]
		public uint PartySize;
		[FieldOffset(8)]
		public unsafe fixed byte PartyMemberList[80 * 8];
	}
	
	[StructLayout(LayoutKind.Explicit, Size = 80)]
	public struct CrossWorldPartyMember {
		[FieldOffset(0)]
		public ulong ContentId;
		[FieldOffset(24)]
		public byte Level;
		[FieldOffset(26)]
		public ushort HomeWorld;
		[FieldOffset(28)]
		public ushort CurrentWorld;
		[FieldOffset(30)]
		public byte JobId;
		[FieldOffset(34)]
		public unsafe fixed byte Name[46];
	}
}