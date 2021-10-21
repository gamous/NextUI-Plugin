using CefSharp;
using CefSharp.Structs;
using D3D11 = SharpDX.Direct3D11;
using DXGI = SharpDX.DXGI;
using System;
using System.Collections.Concurrent;
using System.Reactive.Subjects;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Security;
using Dalamud.Logging;
using NextUIShared.Model;
using NextUIShared.Request;
using SharpDX.Direct3D;
using Size = System.Drawing.Size;

namespace NextUIBrowser.RenderHandlers {
	public class TextureRenderHandler : BaseRenderHandler {
		// CEF buffers are 32-bit BGRA
		protected const byte BytesPerPixel = 4;

		// protected readonly D3D11.Device device;
		// protected D3D11.Texture2D? popupTexture;
		// protected ConcurrentBag<D3D11.Texture2D> obsoleteTextures = new();

		protected Overlay overlay;

		//
		// protected D3D11.Texture2D? texture;
		//
		// protected D3DTextureWrap? textureWrap;
		//
		// // ReSharper disable once InconsistentNaming
		// public Subject<D3DTextureWrap?> TextureWrapChange = new();
		// public D3DTextureWrap? TextureWrap {
		// 	get { return textureWrap; }
		// 	set {
		// 		textureWrap = value;
		// 		TextureWrapChange.OnNext(value);
		// 	}
		// }
		// protected IntPtr sharedTextureHandle = IntPtr.Zero;
		// public event Action<IntPtr>? TexturePointerChange;

		// public IntPtr SharedTextureHandle {
		// 	get { return sharedTextureHandle; }
		// 	protected set {
		// 		if (value == sharedTextureHandle) {
		// 			return;
		// 		}
		//
		// 		sharedTextureHandle = value;
		// 		TexturePointerChange?.Invoke(sharedTextureHandle);
		// 	}
		// }

		// Transparent background click-through state
		protected IntPtr bufferPtr;
		protected int bufferWidth;
		protected int bufferHeight;

		public TextureRenderHandler(Overlay overlay) {
			this.overlay = overlay;
			// this.device = device;
			// texture = BuildViewTexture(size);
		}

		public override void Dispose() {
			// texture?.Dispose();
			// popupTexture?.Dispose();

			// TextureWrap = null;

			// foreach (D3D11.Texture2D tex in obsoleteTextures) {
				// tex.Dispose();
			// }
		}

		protected bool resizing;

		// We only need to keep GetAlphaAt away from reading pointer while browser resizes itself
		public override void Resize(Size size) {
			resizing = true;
			// var oldTexture = texture;
			// texture = BuildViewTexture(size);
			// if (oldTexture != null) {
			// 	obsoleteTextures.Add(oldTexture);
			// }
		}

		// Nasty shit needs nasty attributes.
		[HandleProcessCorruptedStateExceptions, SecurityCritical]
		protected override byte GetAlphaAt(int x, int y) {
			if (resizing || bufferPtr == IntPtr.Zero) {
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
				alpha = Marshal.ReadByte(bufferPtr + cursorAlphaOffset);
			}
			catch {
				// Console.Error.WriteLine("Failed to read alpha value from cef buffer.");
				return 255;
			}

			return alpha;
		}

		/*
		private D3D11.Texture2D BuildViewTexture(Size size) {
			// Build texture. Most of these properties are defined to match how CEF exposes the render buffer.
			var newTexture = new D3D11.Texture2D(device, new D3D11.Texture2DDescription() {
				Width = size.Width,
				Height = size.Height,
				MipLevels = 1,
				ArraySize = 1,
				Format = DXGI.Format.B8G8R8A8_UNorm,
				SampleDescription = new DXGI.SampleDescription(1, 0),
				Usage = D3D11.ResourceUsage.Default,
				BindFlags = D3D11.BindFlags.ShaderResource,
				CpuAccessFlags = D3D11.CpuAccessFlags.None,
				// TODO: Look into getting SharedKeyedMutex working without a CTD from the plugin side.
				OptionFlags = D3D11.ResourceOptionFlags.None,
			});

			var view = new D3D11.ShaderResourceView(
				device,
				newTexture,
				new D3D11.ShaderResourceViewDescription {
					Format = newTexture.Description.Format,
					Dimension = ShaderResourceViewDimension.Texture2D,
					Texture2D = { MipLevels = newTexture.Description.MipLevels },
				}
			);

			TextureWrap = new D3DTextureWrap(view, newTexture.Description.Width, newTexture.Description.Height);
			// IntPtr texHandle;
			//
			// using (var resource = newTexture.QueryInterface<DXGI.Resource>()) {
			// 	texHandle = resource.SharedHandle;
			// }
			//
			// SharedTextureHandle = texHandle;
			return newTexture;
		}
		*/

		public override Rect GetViewRect() {
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


		public override void OnPaint(
			PaintElementType type,
			Rect dirtyRect,
			IntPtr buffer,
			int width,
			int height
		) {
			// Nasty hack; we're keeping a ref to the view buffer for pixel lookups without going through DX
			if (type == PaintElementType.View) {
				bufferPtr = buffer;
				bufferWidth = width;
				bufferHeight = height;

				// Nasty hack fixed with resizing lock which eliminates race conditions
				resizing = false;
			}

			var requestType = type == PaintElementType.View ? PaintType.View : PaintType.Popup;
			var newRect = new XRect(dirtyRect.X, dirtyRect.Y, dirtyRect.Width, dirtyRect.Height);

			overlay.Paint.OnNext(new PaintRequest() {
				buffer = buffer,
				height = height,
				width = width,
				type = requestType,
				dirtyRect = newRect
			});
		}

		public override void OnPopupShow(bool show) {
			overlay.PopupShow.OnNext(show);
		}

		public override void OnPopupSize(Rect rect) {
			overlay.PopupSize.OnNext(new PopupSizeRequest() {
				rect = new XRect(rect.X, rect.Y, rect.Width, rect.Height)
			});
		}
	}
}