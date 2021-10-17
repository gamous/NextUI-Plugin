using CefSharp;
using CefSharp.Structs;
using D3D11 = SharpDX.Direct3D11;
using DXGI = SharpDX.DXGI;
using System;
using System.Collections.Concurrent;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using Dalamud.Logging;
using Size = System.Drawing.Size;

namespace NextUIBrowser.RenderHandlers {
	class TextureRenderHandler : BaseRenderHandler {
		// CEF buffers are 32-bit BGRA
		protected const byte BytesPerPixel = 4;

		protected readonly D3D11.Device device;
		protected D3D11.Texture2D? texture;
		protected D3D11.Texture2D? popupTexture;
		protected ConcurrentBag<D3D11.Texture2D> obsoleteTextures = new();

		protected bool popupVisible;
		protected Rect popupRect;

		protected IntPtr sharedTextureHandle = IntPtr.Zero;
		public event Action<IntPtr>? TexturePointerChange;

		public IntPtr SharedTextureHandle {
			get {
				return sharedTextureHandle;
			}
			protected set {
				if (value == sharedTextureHandle) {
					return;
				}
				sharedTextureHandle = value;
				TexturePointerChange?.Invoke(sharedTextureHandle);
			}
		}

		// Transparent background click-through state
		protected IntPtr bufferPtr;
		protected int bufferWidth;
		protected int bufferHeight;

		public TextureRenderHandler(D3D11.Device device, Size size) {
			this.device = device;
			texture = BuildViewTexture(size);
		}

		public override void Dispose() {
			texture?.Dispose();
			popupTexture?.Dispose();

			foreach (D3D11.Texture2D tex in obsoleteTextures) {
				tex.Dispose();
			}
		}

		public override void Resize(Size size) {
			var oldTexture = texture;
			texture = BuildViewTexture(size);
			if (oldTexture != null) {
				obsoleteTextures.Add(oldTexture);
			}
		}

		// Nasty shit needs nasty attributes.
		[HandleProcessCorruptedStateExceptions]
		protected override byte GetAlphaAt(int x, int y) {
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
				Console.Error.WriteLine("Failed to read alpha value from cef buffer.");
				return 255;
			}

			return alpha;
		}

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
				OptionFlags = D3D11.ResourceOptionFlags.Shared,
			});
			IntPtr texHandle;
			
			using (var resource = newTexture.QueryInterface<DXGI.Resource>()) {
				texHandle = resource.SharedHandle;
			}

			SharedTextureHandle = texHandle;
			return newTexture;
		}

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
			var texDesc = texture.Description;
			return new Rect(0, 0, texDesc.Width, texDesc.Height);
		}

		public override void OnPaint(PaintElementType type, Rect dirtyRect, IntPtr buffer, int width, int height) {
			var targetTexture = type switch {
				PaintElementType.View => texture,
				PaintElementType.Popup => popupTexture,
				_ => throw new Exception($"Unknown paint type {type}"),
			};

			// Nasty hack; we're keeping a ref to the view buffer for pixel lookups without going through DX
			if (type == PaintElementType.View) {
				bufferPtr = buffer;
				bufferWidth = width;
				bufferHeight = height;
			}

			// Calculate offset multipliers for the current buffer
			var rowPitch = width * BytesPerPixel;
			var depthPitch = rowPitch * height;

			// Build the destination region for the dirty rect that we'll draw to
			var texDesc = targetTexture.Description;
			var sourceRegionPtr = buffer + (dirtyRect.X * BytesPerPixel) + (dirtyRect.Y * rowPitch);
			D3D11.ResourceRegion destinationRegion = new() {
				Top = Math.Min(dirtyRect.Y, texDesc.Height),
				Bottom = Math.Min(dirtyRect.Y + dirtyRect.Height, texDesc.Height),
				Left = Math.Min(dirtyRect.X, texDesc.Width),
				Right = Math.Min(dirtyRect.X + dirtyRect.Width, texDesc.Width),
				Front = 0,
				Back = 1,
			};

			// Draw to the target
			var context = targetTexture.Device.ImmediateContext;
			context.UpdateSubresource(targetTexture, 0, destinationRegion, sourceRegionPtr, rowPitch, depthPitch);

			// Only need to do composition + flush on primary texture
			if (type != PaintElementType.View) {
				return;
			}

			// Intersect with dirty?
			if (popupVisible) {
				context.CopySubresourceRegion(popupTexture, 0, null, targetTexture, 0, popupRect.X, popupRect.Y);
			}

			context.Flush();

			// Rendering is complete, clean up any obsolete textures
			var textures = obsoleteTextures;
			obsoleteTextures = new ConcurrentBag<D3D11.Texture2D>();
			foreach (var tex in textures) {
				tex.Dispose();
			}
		}

		public override void OnPopupShow(bool show) {
			popupVisible = show;
		}

		public override void OnPopupSize(Rect rect) {
			popupRect = rect;

			// I'm really not sure if this happens. If it does, frequently - will probably need 2x shared textures and some jazz.
			var texDesc = texture.Description;
			if (rect.Width > texDesc.Width || rect.Height > texDesc.Height) {
				Console.Error.WriteLine(
					$"Trying to build popup layer ({rect.Width}x{rect.Height}) larger than primary surface ({texDesc.Width}x{texDesc.Height})."
				);
			}

			// Get a reference to the old texture, we'll make sure to assign a new texture before disposing the old one.
			var oldTexture = popupTexture;

			// Build a texture for the new sized popup
			popupTexture = new D3D11.Texture2D(texture.Device, new D3D11.Texture2DDescription() {
				Width = rect.Width,
				Height = rect.Height,
				MipLevels = 1,
				ArraySize = 1,
				Format = DXGI.Format.B8G8R8A8_UNorm,
				SampleDescription = new DXGI.SampleDescription(1, 0),
				Usage = D3D11.ResourceUsage.Default,
				BindFlags = D3D11.BindFlags.ShaderResource,
				CpuAccessFlags = D3D11.CpuAccessFlags.None,
				OptionFlags = D3D11.ResourceOptionFlags.None,
			});

			oldTexture?.Dispose();
		}
	}
}