using System;
using System.Runtime.InteropServices;
using NextUIPlugin.NetworkStructures.Common;

namespace NextUIPlugin.NetworkStructures.Client {
	/**
	 * Opcode: 0x0163
	 */
	[Serializable]
	[StructLayout(LayoutKind.Sequential)]
	public struct XivIpcUpdatePositionInstance {
		public float rotation;
		public float interpolateRotation;
		public uint flags;
		public Position position;
		public Position interpolatePosition;
		public uint unknown;
	};
}