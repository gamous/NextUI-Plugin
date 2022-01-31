using System;
using Dalamud.Logging;
using NextUIPlugin.Data.Input;
using Xilium.CefGlue;

namespace NextUIPlugin.Cef.App {
	// ReSharper disable once InconsistentNaming
	public class NUCefDisplayHandler : CefDisplayHandler {
		protected NUCefRenderHandler renderHandler;

		// Transparent background click-through state
		protected bool cursorOnBackground;
		protected Cursor cursor;

		public event EventHandler<Cursor>? CursorChanged;

		public NUCefDisplayHandler(NUCefRenderHandler renderHandler) {
			this.renderHandler = renderHandler;
		}

		public void SetMousePosition(int x, int y) {
			var alpha = renderHandler.GetAlphaAt(x, y);

			// We treat 0 alpha as click through - if changed, fire off the event
			var currentlyOnBackground = alpha == 0;
			if (currentlyOnBackground == cursorOnBackground) {
				return;
			}

			cursorOnBackground = currentlyOnBackground;

			// EDGE CASE: if cursor transitions onto alpha:0 _and_ between two native cursor types, I guess this will be a race cond.
			// Not sure if should have two seperate upstreams for them, or try and prevent the race. consider.
			CursorChanged?.Invoke(this, currentlyOnBackground ? Cursor.BrowserHostNoCapture : cursor);
		}

		protected override bool OnAutoResize(CefBrowser browser, ref CefSize newSize) {
			PluginLog.Log($"RESIZE FINISHED {newSize.ToString()}");
			return false;
		}

		protected override bool OnCursorChange(
			CefBrowser browser,
			IntPtr cursorHandle,
			CefCursorType type,
			CefCursorInfo customCursorInfo
		) {
			cursor = EncodeCursor(type);

			// If we're on background, don't flag a cursor change
			if (!cursorOnBackground) {
				CursorChanged?.Invoke(this, cursor);
			}

			return false;
		}

		protected static Cursor EncodeCursor(CefCursorType cefCursor) {
			// ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
			switch (cefCursor) {
				// CEF calls default "pointer", and pointer "hand".
				case CefCursorType.Pointer: return Cursor.Default;
				case CefCursorType.Cross: return Cursor.Crosshair;
				case CefCursorType.Hand: return Cursor.Pointer;
				case CefCursorType.IBeam: return Cursor.Text;
				case CefCursorType.Wait: return Cursor.Wait;
				case CefCursorType.Help: return Cursor.Help;
				case CefCursorType.EastResize: return Cursor.EResize;
				case CefCursorType.NorthResize: return Cursor.NResize;
				case CefCursorType.NorthEastResize: return Cursor.NEResize;
				case CefCursorType.NorthWestResize: return Cursor.NWResize;
				case CefCursorType.SouthResize: return Cursor.SResize;
				case CefCursorType.SouthEastResize: return Cursor.SEResize;
				case CefCursorType.SouthWestResize: return Cursor.SWResize;
				case CefCursorType.WestResize: return Cursor.WResize;
				case CefCursorType.NorthSouthResize: return Cursor.NSResize;
				case CefCursorType.EastWestResize: return Cursor.EWResize;
				case CefCursorType.NorthEastSouthWestResize: return Cursor.NESWResize;
				case CefCursorType.NorthWestSouthEastResize: return Cursor.NWSEResize;
				case CefCursorType.ColumnResize: return Cursor.ColResize;
				case CefCursorType.RowResize: return Cursor.RowResize;

				// There isn't really support for panning right now. Default to all-scroll.
				case CefCursorType.MiddlePanning:
				case CefCursorType.EastPanning:
				case CefCursorType.NorthPanning:
				case CefCursorType.NorthEastPanning:
				case CefCursorType.NorthWestPanning:
				case CefCursorType.SouthPanning:
				case CefCursorType.SouthEastPanning:
				case CefCursorType.SouthWestPanning:
				case CefCursorType.WestPanning:
					return Cursor.AllScroll;

				case CefCursorType.Move: return Cursor.Move;
				case CefCursorType.VerticalText: return Cursor.VerticalText;
				case CefCursorType.Cell: return Cursor.Cell;
				case CefCursorType.ContextMenu: return Cursor.ContextMenu;
				case CefCursorType.Alias: return Cursor.Alias;
				case CefCursorType.Progress: return Cursor.Progress;
				case CefCursorType.NoDrop: return Cursor.NoDrop;
				case CefCursorType.Copy: return Cursor.Copy;
				case CefCursorType.None: return Cursor.None;
				case CefCursorType.NotAllowed: return Cursor.NotAllowed;
				case CefCursorType.ZoomIn: return Cursor.ZoomIn;
				case CefCursorType.ZoomOut: return Cursor.ZoomOut;
				case CefCursorType.Grab: return Cursor.Grab;
				case CefCursorType.Grabbing: return Cursor.Grabbing;

				// Not handling custom for now
				case CefCursorType.Custom: return Cursor.Default;
			}

			// Unmapped cursor, log and default
			PluginLog.Warning($"Switching to unmapped cursor type {cefCursor}.");
			return Cursor.Default;
		}
	}
}