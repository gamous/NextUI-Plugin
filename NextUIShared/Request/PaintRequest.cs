using System;

namespace NextUIShared.Request {
	// Have to translate, can't reference any of the cef stuff
	public enum PaintType {
		View,
		Popup
	}

	// Translated rect
	[Serializable]
	public struct XRect {
		public int x;
		public int y;
		public int width;
		public int height;

		public XRect(int x, int y, int width, int height) {
			this.x = x;
			this.y = y;
			this.width = width;
			this.height = height;
		}

		public override string ToString() {
			return $"XRect({x}, {y}, {width}, {height})";
		}
	}

	[Serializable]
	public struct PaintRequest {
		public XRect dirtyRect;
		public IntPtr buffer;
		public int width;
		public int height;
	}
}