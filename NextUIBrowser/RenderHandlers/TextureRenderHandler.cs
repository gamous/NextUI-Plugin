using CefSharp;
using CefSharp.Structs;
using System;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Security;
using Dalamud.Logging;
using NextUIShared.Model;
using NextUIShared.Request;
using Size = System.Drawing.Size;

namespace NextUIBrowser.RenderHandlers {
	public class TextureRenderHandler : BaseRenderHandler {
		// CEF buffers are 32-bit BGRA
		protected const byte BytesPerPixel = 4;

		protected readonly Overlay overlay;

		// Transparent background click-through state
		protected IntPtr internalBuffer;
		protected int bufferWidth;
		protected int bufferHeight;
		protected int bufferSize;

		public TextureRenderHandler(Overlay overlay) {
			this.overlay = overlay;
		}

		public override void Dispose() {
			// are even need to dispose anything aside from freeing memory?
			Marshal.FreeHGlobal(internalBuffer);
		}

		// We only need to keep GetAlphaAt away from reading pointer while browser resizes itself
		public override void Resize(Size size) {

		}

		// Nasty shit needs nasty attributes.
		[HandleProcessCorruptedStateExceptions, SecurityCritical]
		protected override byte GetAlphaAt(int x, int y) {
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

		// TEST
		public override Rect GetViewRect() {
			return new Rect(0, 0, overlay.Size.Width, overlay.Size.Height);
			// There's a very small chance that OnPaint's cleanup will delete the current texture midway through this
			// function. Try a few times just in case before failing out with an obviously-wrong value
			// hi adam
			for (var i = 0; i < 5; i++) {
				try {
					return GetViewRectInternal();
				}
				catch (NullReferenceException) {
				}
			}

			return new Rect(0, 0, 1, 1);
		}

		private Rect GetViewRectInternal() {
			// var texDesc = texture.Description;
			return new Rect(0, 0, overlay.Size.Width, overlay.Size.Height);
		}

		public override unsafe void OnPaint(
			PaintElementType type,
			Rect dirtyRect,
			IntPtr buffer,
			int width,
			int height
		) {
			var newSize = width * height * BytesPerPixel;
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
			var newRect = new XRect(dirtyRect.X, dirtyRect.Y, dirtyRect.Width, dirtyRect.Height);

			overlay.PaintRequest(new PaintRequest() {
				buffer = internalBuffer,
				height = height,
				width = width,
				dirtyRect = newRect
			});
		}

		public override void OnPopupShow(bool show) {
			overlay.ShowPopup(show);
		}

		public override void OnPopupSize(Rect rect) {
			overlay.PopupSizeChange(new PopupSizeRequest() {
				rect = new XRect(rect.X, rect.Y, rect.Width, rect.Height)
			});
		}
	}
}