using System;
using System.Drawing;
using System.Runtime.InteropServices;
using Dalamud.Logging;
using NextUIShared.Data;
using NextUIShared.Model;
using NextUIShared.Request;
using Xilium.CefGlue;

namespace NextUIBrowser.Cef.App {
	// ReSharper disable once InconsistentNaming
	public class NUCefRenderHandler : CefRenderHandler {
		// CEF buffers are 32-bit BGRA
		protected const byte BytesPerPixel = 4;

		protected readonly Overlay overlay;

		// Transparent background click-through state
		protected IntPtr internalBuffer;
		protected int bufferWidth;
		protected int bufferHeight;
		protected int bufferSize;

		protected int width;
		protected int height;
		
		public event EventHandler<Cursor>? CursorChanged;

		// Transparent background click-through state
		protected bool cursorOnBackground;
		protected Cursor cursor;

		public NUCefRenderHandler(Overlay overlay) {
			this.overlay = overlay;
			width = overlay.Size.Width;
			height = overlay.Size.Height;
		}

		protected override CefAccessibilityHandler GetAccessibilityHandler() {
			return null;
		}

		protected override bool GetRootScreenRect(CefBrowser browser, ref CefRectangle rect) {
			GetViewRect(browser, out rect);
			return true;
		}

		public void Resize(Size size) {
			width = size.Width;
			height = size.Height;
		}
		
		public void SetMousePosition(int x, int y) {
			var alpha = GetAlphaAt(x, y);

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

		protected byte GetAlphaAt(int x, int y) {
			if (overlay.Resizing || internalBuffer == IntPtr.Zero) {
				return 255;
			}

			var rowPitch = bufferWidth * BytesPerPixel;

			// Get the offset for the alpha of the cursor's current position.
			// Bitmap buffer is BGRA, so +3 to get alpha byte
			var cursorAlphaOffset =
				0
				+ (Math.Min(Math.Max(x, 0), bufferWidth - 1) * BytesPerPixel)
				+ (Math.Min(Math.Max(y, 0), bufferHeight - 1) * rowPitch)
				+ 3;

			byte alpha;
			try {
				alpha = Marshal.ReadByte(internalBuffer + cursorAlphaOffset);
			}
			catch {
				// Console.Error.WriteLine("Failed to read alpha value from cef buffer.");
				return 255;
			}

			return alpha;
		}

		protected override void GetViewRect(CefBrowser browser, out CefRectangle rect) {
			rect = new CefRectangle(0, 0, width, height);
			// rect.X = 0;
			// rect.Y = 0;
			// rect.Width = _windowWidth;
			// rect.Height = _windowHeight;
		}

		protected override bool GetScreenPoint(
			CefBrowser browser,
			int viewX,
			int viewY,
			ref int screenX,
			ref int screenY
		) {
			screenX = viewX;
			screenY = viewY;
			return true;
		}

		protected override bool GetScreenInfo(CefBrowser browser, CefScreenInfo screenInfo) {
			return false;
		}

		protected override void OnPopupSize(CefBrowser browser, CefRectangle rect) {
		}

		protected override unsafe void OnPaint(
			CefBrowser browser,
			CefPaintElementType type,
			CefRectangle[] dirtyRects,
			IntPtr buffer,
			int width,
			int height
		) {
			var newSize = width * height * BytesPerPixel;
			PluginLog.Log($"PAINT REQ {newSize}");
			// No buffer yet
			if (internalBuffer == IntPtr.Zero) {
				internalBuffer = Marshal.AllocHGlobal(newSize);
				bufferSize = newSize;
			}

			if (bufferSize != newSize) {
				// our buffer changed size
				Marshal.FreeHGlobal(internalBuffer);
				internalBuffer = Marshal.AllocHGlobal(newSize);
				bufferSize = newSize;
			}

			bufferWidth = width;
			bufferHeight = height;
			bufferSize = newSize;

			// var rowPitch = bufferWidth * BytesPerPixel;
			// var offset = (dirtyRect.X * BytesPerPixel) + (dirtyRect.Y * BytesPerPixel * rowPitch);

			// This is probably faster than trying to calculate exact rects to update
			Buffer.MemoryCopy(buffer.ToPointer(), internalBuffer.ToPointer(), bufferSize, bufferSize);

			// Nasty hack fixed with resizing lock which eliminates race conditions
			overlay.Resizing = false;

			// var requestType = type == PaintElementType.View ? PaintType.View : PaintType.Popup;
			// var newRect = new XRect(dirtyRect.X, dirtyRect.Y, dirtyRect.Width, dirtyRect.Height);

			overlay.PaintRequest(new PaintRequest() {
				buffer = internalBuffer,
				height = height,
				width = width,
				// dirtyRect = newRect
			});

			// Save the provided buffer (a bitmap image) as a PNG.
			// var bitmap = new Bitmap(width, height, width * 4, PixelFormat.Format32bppArgb, buffer);
			// bitmap.Save("LastOnPaint.png", ImageFormat.Png);
		}

		protected override void OnAcceleratedPaint(
			CefBrowser browser,
			CefPaintElementType type,
			CefRectangle[] dirtyRects,
			IntPtr sharedHandle
		) {
			throw new NotImplementedException();
		}

		protected override void OnScrollOffsetChanged(CefBrowser browser, double x, double y) {
			throw new NotImplementedException();
		}

		protected override void OnImeCompositionRangeChanged(
			CefBrowser browser,
			CefRange selectedRange,
			CefRectangle[] characterBounds
		) {
			throw new NotImplementedException();
		}

		public void Dispose() {
			Marshal.FreeHGlobal(internalBuffer);
		}
	}
}