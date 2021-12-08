using System;
using System.Runtime.InteropServices;
using Newtonsoft.Json;

namespace NextUIPlugin.NetworkStructures.Common {
	[Serializable]
	[StructLayout(LayoutKind.Sequential)]
	public struct ShortPosition {
		[JsonIgnore]
		public ushort xRaw;
		[JsonIgnore]
		public ushort yRaw;
		[JsonIgnore]
		public ushort zRaw;

		// ReSharper disable once InconsistentNaming
		public float x {
			get { return StructUtil.UShortToFloat(xRaw); }
		}

		// ReSharper disable once InconsistentNaming
		public float y {
			get { return StructUtil.UShortToFloat(yRaw); }
		}

		// ReSharper disable once InconsistentNaming
		public float z {
			get { return StructUtil.UShortToFloat(zRaw); }
		}
	}
}