using System;
using System.Runtime.InteropServices;
using NextUIPlugin.NetworkStructures.Common;

namespace NextUIPlugin.NetworkStructures.Server {
	[Serializable]
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct XivIpcPlaceFieldMarkerPreset {
		/*! which fieldmarks to show */
		public byte markerId;

		/*! A coordinates would be (float)xInts[0]/1000.0, (float)yInts[0]/1000.0, (float)zInts[0]/1000.0 */

		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
		public int[] xInts;

		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
		public int[] yInts;

		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
		public int[] zInts;

		public IntPosition[] positions {
			get {
				var output = new IntPosition[8];
				for (var i = 0; i < 8; i++) {
					output[i] = new IntPosition{
						xRaw = xInts[i],
						yRaw = yInts[i],
						zRaw = zInts[i],
					};
				}

				return output;
			}
		}
	}
}