using System;
using System.Runtime.InteropServices;

namespace NextUIPlugin.NetworkStructures.Common {
	[Serializable]
	[StructLayout(LayoutKind.Sequential)]
	public struct StatusEffect {
		public ushort effectId;
		public ushort unknown1;
		public float duration;
		public uint sourceActorId;
	}
}