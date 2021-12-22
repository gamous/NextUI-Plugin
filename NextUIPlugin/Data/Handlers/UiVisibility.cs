using FFXIVClientStructs.FFXIV.Component.GUI;

namespace NextUIPlugin.Data.Handlers {
	public static unsafe class UiVisibility {
		internal const string EventName = "uiVisibilityChanged";

		internal static AtkUnitBase* fadeMiddleWidget;
		internal static AtkUnitBase* parameterWidget;

		internal static bool uiVisible;

		/**
		 * Initialize after login
		 */
		public static void Initialize() {
			parameterWidget = (AtkUnitBase*)NextUIPlugin.gameGui.GetAddonByName("_ParameterWidget", 1);
			fadeMiddleWidget = (AtkUnitBase*)NextUIPlugin.gameGui.GetAddonByName("FadeMiddle", 1);

			uiVisible = GetUiVisibility();
		}

		public static bool GetUiVisibility() {
			if (NextUIPlugin.clientState.LocalPlayer == null) {
				return false;
			}

			var parameterVisible = parameterWidget != null && parameterWidget->IsVisible;
			var fadeMiddleVisible = fadeMiddleWidget != null && fadeMiddleWidget->IsVisible;

			return parameterVisible && !fadeMiddleVisible;
		}

		public static void Watch() {
			var sockets = NextUIPlugin.socketServer.GetEventSubscriptions(EventName);
			if (sockets == null || sockets.Count == 0) {
				return;
			}

			var currentUiVisibility = GetUiVisibility();

			if (uiVisible == currentUiVisibility) {
				return;
			}

			uiVisible = currentUiVisibility;
			NextUIPlugin.socketServer.BroadcastTo(new {
				@event = EventName,
				data = uiVisible,
			}, sockets);
		}
	}
}