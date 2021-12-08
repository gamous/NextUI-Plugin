using System;
using System.Runtime.InteropServices;
using Newtonsoft.Json;

namespace NextUIPlugin.NetworkStructures.Common {
	[Serializable]
	[StructLayout(LayoutKind.Sequential)]
	public struct IntPosition {
		[JsonIgnore]
		public int xRaw;
		[JsonIgnore]
		public int yRaw;
		[JsonIgnore]
		public int zRaw;

		// ReSharper disable once InconsistentNaming
		public float x {
			get { return xRaw / 1000f; }
		}

		// ReSharper disable once InconsistentNaming
		public float y {
			get { return yRaw / 1000f; }
		}

		// ReSharper disable once InconsistentNaming
		public float z {
			get { return zRaw / 1000f; }
		}
	}
}