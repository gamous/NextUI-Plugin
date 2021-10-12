﻿using System;
using RendererProcess.Data;

namespace RendererProcess.Ipc {
	[Serializable]
	public class MouseEventRequest : DownstreamIpcRequest {
		public float x;
		public float y;

		public bool leaving;

		// The following button fields represent changes since the previous event, not current state
		// TODO: May be approaching being advantageous for button->fields map
		public MouseButton mouseDown;
		public MouseButton mouseDouble;
		public MouseButton mouseUp;
		public float wheelX;
		public float wheelY;
		public InputModifier modifier;
		
		
		public MouseEventRequest() {
			reqType = "mouseEvent";
		}
	}
}