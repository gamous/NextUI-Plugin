using System;
using System.Runtime.InteropServices;

namespace NextUIPlugin.NetworkStructures.Common {
	[Serializable]
	[StructLayout(LayoutKind.Sequential)]
	public struct Position {
		public float x;
		public float y;
		public float z;
	}
}