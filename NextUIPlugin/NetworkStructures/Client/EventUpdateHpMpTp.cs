using System;
using System.Runtime.InteropServices;

namespace NextUIPlugin.NetworkStructures.Client {
	[Serializable]
	[StructLayout(LayoutKind.Sequential)]
	public struct EventUpdateHpMpTp {
		/* 0000 */
		public ulong hp;

		/* 0004 */
		public ushort mp;

		/* 0006 */
		public ushort tp;

		/* 0008 */
		public ushort gp;

		/* 0010 */
		public ushort unknown_10;

		/* 0012 */
		public ulong unknown_12;
	}
}