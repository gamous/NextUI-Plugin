﻿using System;
using System.Runtime.InteropServices;

namespace NextUIPlugin.Overlay {
	public static class WndProcHandler {
		public delegate (bool, long) WndProcMessageDelegate(WindowsMessage msg, ulong wParam, long lParam);

		public static event WndProcMessageDelegate? WndProcMessage;

		public delegate long WndProcDelegate(IntPtr hWnd, uint msg, ulong wParam, long lParam);

		static WndProcDelegate? wndProcDelegate;

		static IntPtr hWnd;
		static IntPtr oldWndProcPtr;
		static IntPtr detourPtr;

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
			IntPtr curWndProcPtr = NativeMethods.GetWindowLongPtr(hWnd, WindowLongType.GwlWndProc);
			if (oldWndProcPtr == IntPtr.Zero || curWndProcPtr != detourPtr) {
				return;
			}

			NativeMethods.SetWindowLongPtr(hWnd, WindowLongType.GwlWndProc, oldWndProcPtr);
			oldWndProcPtr = IntPtr.Zero;
		}

		static long WndProcDetour(IntPtr nhWnd, uint msg, ulong wParam, long lParam) {
			// Ignore things not targeting the current window handle
			if (nhWnd == hWnd) {
				(bool, long)? resp = WndProcMessage?.Invoke((WindowsMessage)msg, wParam, lParam);

				// Item1 is a bool, where true == capture event. If false, we're falling through default handling.
				if (resp.HasValue && resp.Value.Item1) {
					return resp.Value.Item2;
				}
			}

			return NativeMethods.CallWindowProc(oldWndProcPtr, nhWnd, msg, wParam, lParam);
		}
	}
}