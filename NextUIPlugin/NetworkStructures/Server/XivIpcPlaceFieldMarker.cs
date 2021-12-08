using System;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;
using NextUIPlugin.NetworkStructures.Common;

namespace NextUIPlugin.NetworkStructures.Server {
	
	[Serializable]
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct XivIpcPlaceFieldMarker {
		/*! which fieldmarks to show */
		public byte markerId;
		public byte set;
		[JsonIgnore]
		public ushort unknown;
		/*! A coordinates would be (float)xInts[0]/1000.0, (float)yInts[0]/1000.0, (float)zInts[0]/1000.0 */

		public IntPosition position;
		// public uint x;
		// public uint y;
		// public uint z;
	}
}