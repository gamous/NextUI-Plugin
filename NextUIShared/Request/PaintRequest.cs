using System;

namespace NextUIShared.Request {
	// Have to translate, can't reference any of the cef stuff
	public enum PaintType {
		View,
		Popup
	}

	// Translated rect
	[Serializable]
	public class XRect {
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
	}

	[Serializable]
	public class PaintRequest {
		public PaintType type;
		public XRect dirtyRect = null!;
		public IntPtr buffer;
		public int width;
		public int height;
	}
}