using System;
using NextUIShared.Data;

namespace NextUIShared.Request {
	[Serializable]
	public struct MouseMoveEventRequest {
		public float x;
		public float y;

		public InputModifier modifier;
	}

	[Serializable]
	public struct MouseClickEventRequest {
		public float x;
		public float y;

		public MouseButtonType mouseButtonType;
		public bool isUp;
		public int clickCount;
		public InputModifier modifier;
	}

	[Serializable]
	public struct MouseLeaveEventRequest {
		public float x;
		public float y;
	}

	[Serializable]
	public struct MouseWheelEventRequest {
		public float x;
		public float y;

		public float wheelX;
		public float wheelY;

		public InputModifier modifier;
	}
}