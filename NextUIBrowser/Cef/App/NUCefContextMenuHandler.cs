using Xilium.CefGlue;

namespace NextUIBrowser.Cef.App {
	public class NUCefContextMenuHandler : CefContextMenuHandler {
		protected override void OnBeforeContextMenu(
			CefBrowser browser, 
			CefFrame frame, 
			CefContextMenuParams state, 
			CefMenuModel model
		) {
			model.Clear();
		}

		protected override bool OnContextMenuCommand(
			CefBrowser browser, 
			CefFrame frame, 
			CefContextMenuParams state, 
			int commandId,
			CefEventFlags eventFlags
		) {
			return false;
		}

		protected override void OnContextMenuDismissed(CefBrowser browser, CefFrame frame) {
		}

		protected override bool RunContextMenu(
			CefBrowser browser, 
			CefFrame frame, 
			CefContextMenuParams parameters, 
			CefMenuModel model,
			CefRunContextMenuCallback callback
		) {
			return false;
		}
	}
}