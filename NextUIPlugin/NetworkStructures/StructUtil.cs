using System.Runtime.InteropServices;
using System.Text;

namespace NextUIPlugin.NetworkStructures {
	public static class StructUtil {
		public static unsafe ulong[] FixedArray(ulong* source, uint length) {
			var output = new ulong[length];
			for (var i = 0; i < length; i++) {
				output[i] = source[i];
			}

			return output;
		}

		// ReSharper disable once InconsistentNaming
		public static string FixedUTF8String(byte[] source) {
			return Marshal.PtrToStringUTF8(Marshal.UnsafeAddrOfPinnedArrayElement(source, 0)) ?? "";
		}

		public static ushort FloatToUShort(float val) {
			return (ushort)(0x8000 + val * 32.767f);
		}

		public static float UShortToFloat(ushort val) {
			return (val - 0x8000) / 32.767f;
		}
	}
}