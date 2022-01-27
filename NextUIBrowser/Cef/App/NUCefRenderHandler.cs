using System;
using System.Drawing;
using Dalamud.Logging;
using NextUIShared.Model;
using NextUIShared.Request;
using Xilium.CefGlue;

namespace NextUIBrowser.Cef.App {
	// ReSharper disable once InconsistentNaming
	public class NUCefRenderHandler : CefRenderHandler, IDisposable {
		// CEF buffers are 32-bit BGRA
		protected const byte BytesPerPixel = 4;

		protected readonly Overlay overlay;

		// Transparent background click-through state
		protected byte[] internalBuffer = Array.Empty<byte>();
		protected int bufferWidth;
		protected int bufferHeight;

		protected int width;
		protected int height;

		public NUCefRenderHandler(Overlay overlay) {
			this.overlay = overlay;
			width = overlay.Size.Width;
			height = overlay.Size.Height;
		}

		protected override CefAccessibilityHandler GetAccessibilityHandler() {
			return null!;
		}

		public void Resize(Size size) {
			// lock (overlay.renderLock) {
			width = size.Width;
			height = size.Height;
			PluginLog.Log($"Resize {width} x {height}");
			// }
		}

		public byte GetAlphaAt(int x, int y) {
			lock (overlay.renderLock) {
				var rowPitch = bufferWidth * BytesPerPixel;

				// Get the offset for the alpha of the cursor's current position.
				// Bitmap buffer is BGRA, so +3 to get alpha byte
				var cursorAlphaOffset =
					0
					+ (Math.Min(Math.Max(x, 0), bufferWidth - 1) * BytesPerPixel)
					+ (Math.Min(Math.Max(y, 0), bufferHeight - 1) * rowPitch)
					+ 3;

				if (cursorAlphaOffset < internalBuffer.Length) {
					try {
						return internalBuffer[cursorAlphaOffset];
					}
					catch {
						return 255;
					}
				}

				Console.WriteLine("Could not determine alpha value");
				return 255;
			}
		}

		protected override bool GetRootScreenRect(CefBrowser browser, ref CefRectangle rect) {
			PluginLog.Log($"GetRootScreenRect {width} x {height}");
			rect.X = 0;
			rect.Y = 0;
			rect.Width = width;
			rect.Height = height;
			return true;
		}

		protected override void GetViewRect(CefBrowser browser, out CefRectangle rect) {
			PluginLog.Log($"GetViewRect {width} x {height}");
			rect = new CefRectangle(0, 0, width, height);
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
			return true;
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
			if (type == CefPaintElementType.Popup) {
				return;
			}

			lock (overlay.renderLock) {
				PluginLog.Log($"OnPaint {width} x {height}");
				// check if lookup buffer is big enough
				var requiredBufferSize = width * height * BytesPerPixel;
				bufferWidth = width;
				bufferHeight = height;

				if (internalBuffer.Length != requiredBufferSize) {
					internalBuffer = new byte[bufferWidth * bufferHeight * BytesPerPixel];
				}

				fixed (void* dstBuffer = internalBuffer) {
					Buffer.MemoryCopy(
						buffer.ToPointer(),
						dstBuffer,
						internalBuffer.Length,
						requiredBufferSize
					);

					// Nasty hack fixed with resizing lock which eliminates race conditions
					overlay.Resizing = false;

					// var requestType = type == PaintElementType.View ? PaintType.View : PaintType.Popup;
					// var newRect = new XRect(dirtyRect.X, dirtyRect.Y, dirtyRect.Width, dirtyRect.Height);

					overlay.PaintRequest(new PaintRequest() {
						buffer = (IntPtr)dstBuffer,
						height = height,
						width = width,
						// dirtyRect = newRect
					});
				}
			}
		}

		protected override void OnAcceleratedPaint(
			CefBrowser browser,
			CefPaintElementType type,
			CefRectangle[] dirtyRects,
			IntPtr sharedHandle
		) {
		}

		protected override void OnScrollOffsetChanged(CefBrowser browser, double x, double y) {
		}

		protected override void OnImeCompositionRangeChanged(
			CefBrowser browser,
			CefRange selectedRange,
			CefRectangle[] characterBounds
		) {
		}

		public void Dispose() {
			GC.SuppressFinalize(this);
		}
	}
}