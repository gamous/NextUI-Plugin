﻿using System;
using System.Runtime.InteropServices;

namespace NextUIPlugin.Overlay {
	// Enums are not comprehensive for the sake of omitting stuff I won't use.
	public enum WindowLongType : int {
		GWL_WNDPROC = -4,
	}

	public enum WindowsMessage {
		WM_KEYDOWN = 0x0100,
		WM_KEYUP = 0x0101,
		WM_CHAR = 0x0102,
		WM_SYSKEYDOWN = 0x0104,
		WM_SYSKEYUP = 0x0105,
		WM_SYSCHAR = 0x0106,

		WM_LBUTTONDOWN = 0x0201,
	}

	enum VirtualKey : int {
		Shift = 0x10,
		Control = 0x11,
	}

	class NativeMethods {
		public static bool IsKeyActive(VirtualKey key) {
			return (GetKeyState((int)key) & 1) == 1;
		}

		[DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
		public static extern IntPtr GetWindowLongPtr(IntPtr hWnd, WindowLongType nIndex);

		[DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")]
		public static extern IntPtr SetWindowLongPtr(IntPtr hWnd, WindowLongType nIndex, IntPtr dwNewLong);

		[DllImport("user32.dll", EntryPoint = "CallWindowProcW")]
		public static extern long CallWindowProc(
			IntPtr lpPrevWndFunc,
			IntPtr hWnd, uint msg,
			ulong wParam,
			long lParam
		);

		[DllImport("user32.dll")]
		private static extern short GetKeyState(int nVirtKey);
	}
}