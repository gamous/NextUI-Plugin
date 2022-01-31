/*
 * Borrowed from: https://github.com/Styr1x/Browsingway/blob/main/Browsingway.Renderer/DpiScaling.cs
 */
using System;
using System.Drawing;
using System.Runtime.InteropServices;
using Xilium.CefGlue;

namespace NextUIPlugin.Cef {
	public static class DpiScaling {
		internal static float cachedDeviceScale;

		[DllImport("shcore.dll")]
		public static extern void GetScaleFactorForMonitor(IntPtr hMon, out uint pScale);

		[DllImport("user32.dll")]
		public static extern IntPtr MonitorFromWindow(IntPtr handle, uint dwFlags);

		public static float GetDeviceScale() {
			if (cachedDeviceScale != 0) {
				return cachedDeviceScale;
			}

			var hMon = MonitorFromWindow(IntPtr.Zero, 0x1);
			GetScaleFactorForMonitor(hMon, out var scale);
			// GetScaleFactorForMonitor returns an enum, however someone was nice enough to set the enum's values to match the scaling.
			cachedDeviceScale = scale / 100f;

			return cachedDeviceScale;
		}

		public static CefRectangle ScaleViewRect(CefRectangle rect) {
			return new CefRectangle(
				rect.X,
				rect.Y,
				(int)Math.Ceiling(rect.Width * (1 / GetDeviceScale())),
				(int)Math.Ceiling(rect.Height * (1 / GetDeviceScale()))
			);
		}

		public static CefRectangle ScaleScreenRect(CefRectangle rect) {
			return new CefRectangle(
				rect.X,
				rect.Y,
				(int)Math.Ceiling(rect.Width * GetDeviceScale()),
				(int)Math.Ceiling(rect.Height * GetDeviceScale())
			);
		}

		public static Point ScaleViewPoint(float x, float y) {
			return new Point(
				(int)Math.Ceiling(x * (1 / GetDeviceScale())),
				(int)Math.Ceiling(y * (1 / GetDeviceScale()))
			);
		}

		public static Point ScaleScreenPoint(float x, float y) {
			return new Point(
				(int)Math.Ceiling(x * GetDeviceScale()),
				(int)Math.Ceiling(y * GetDeviceScale())
			);
		}
	}
}