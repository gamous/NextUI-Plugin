using System;
using System.Runtime.InteropServices;
using NextUIPlugin.Service;

namespace NextUIPlugin.Gui {
	public static class WndProcHandler {
		public delegate (bool, long) WndProcMessageDelegate(WindowsMessageS msg, ulong wParam, long lParam);

		public static event WndProcMessageDelegate? WndProcMessage;

		public delegate long WndProcDelegate(IntPtr hWnd, uint msg, ulong wParam, long lParam);

		internal static WndProcDelegate? wndProcDelegate;

		internal static IntPtr hWnd;
		internal static IntPtr oldWndProcPtr;
		internal static IntPtr detourPtr;

		public static void Initialize(IntPtr newHWnd) {
			hWnd = newHWnd;

			wndProcDelegate = WndProcDetour;
			detourPtr = Marshal.GetFunctionPointerForDelegate(wndProcDelegate);
			oldWndProcPtr = NativeMethods.SetWindowLongPtr(newHWnd, WindowLongType.GwlWndProc, detourPtr);
		}

		public static void Shutdown() {
			// If the current pointer doesn't match our detour, something swapped the pointer out from under us -
			// likely the InterfaceManager doing its own cleanup. Don't reset in that case, we'll trust the cleanup
			// is accurate.
			var curWndProcPtr = NativeMethods.GetWindowLongPtr(hWnd, WindowLongType.GwlWndProc);
			if (oldWndProcPtr == IntPtr.Zero || curWndProcPtr != detourPtr) {
				return;
			}

			NativeMethods.SetWindowLongPtr(hWnd, WindowLongType.GwlWndProc, oldWndProcPtr);
			oldWndProcPtr = IntPtr.Zero;
		}

		static long WndProcDetour(IntPtr nhWnd, uint msg, ulong wParam, long lParam) {
			// Ignore things not targeting the current window handle
			if (nhWnd != hWnd) {
				return NativeMethods.CallWindowProc(oldWndProcPtr, nhWnd, msg, wParam, lParam);
			}

			var resp = WndProcMessage?.Invoke((WindowsMessageS)msg, wParam, lParam);

			// Item1 is a bool, where true == capture event. If false, we're falling through default handling.
			if (resp.HasValue && resp.Value.Item1) {
				return resp.Value.Item2;
			}

			return NativeMethods.CallWindowProc(oldWndProcPtr, nhWnd, msg, wParam, lParam);
		}
	}
}