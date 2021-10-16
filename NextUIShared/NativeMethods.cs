using System;
using System.Runtime.InteropServices;

namespace NextUIShared {
	// Enums are not comprehensive for the sake of omitting stuff I won't use.
	public enum WindowLongType {
		GwlWndProc = -4
	}

	public enum WindowsMessage {
		WmKeyDown = 0x0100,
		WmKeyUp = 0x0101,
		WmChar = 0x0102,
		WmSysKeyDown = 0x0104,
		WmSysKeyUp = 0x0105,
		WmSysChar = 0x0106,

		WmLButtonDown = 0x0201
	}

	public enum VirtualKey {
		Shift = 0x10,
		Control = 0x11
	}

	public static class NativeMethods {
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
	}
}