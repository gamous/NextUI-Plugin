using ImGuiScene;
using D3D = SharpDX.Direct3D;
using D3D11 = SharpDX.Direct3D11;
using System.Numerics;
using ImGuiNET;
using NextUIPlugin.Overlay;
using RendererProcess.RenderHandlers;

namespace RendererProcess.Texture {
	public class SharedTextureHandler {
		protected readonly TextureWrap? textureWrap;

		public SharedTextureHandler(TextureHandleResponse response) {
			D3D11.Texture2D? texture = DxHandler.Device?.OpenSharedResource<D3D11.Texture2D>(response.TextureHandle);
			if (texture == null) {
				return;
			}

			D3D11.ShaderResourceView? view = new(
				DxHandler.Device,
				texture,
				new D3D11.ShaderResourceViewDescription {
					Format = texture.Description.Format,
					Dimension = D3D.ShaderResourceViewDimension.Texture2D,
					Texture2D = { MipLevels = texture.Description.MipLevels },
				}
			);

			textureWrap = new D3DTextureWrap(view, texture.Description.Width, texture.Description.Height);
		}

		public void Dispose() {
			textureWrap?.Dispose();
		}

		public void Render() {
			if (textureWrap == null) {
				return;
			}

			ImGui.Image(textureWrap.ImGuiHandle, new Vector2(textureWrap.Width, textureWrap.Height));
		}
	}
}