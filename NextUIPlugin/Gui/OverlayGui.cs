using ImGuiNET;
using System;
using System.Collections.Concurrent;
using System.Drawing;
using System.Numerics;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Logging;
using ImGuiScene;
using NextUIShared;
using NextUIShared.Data;
using NextUIShared.Model;
using NextUIShared.Request;
using SharpDX;
using D3D11 = SharpDX.Direct3D11;
using D3D = SharpDX.Direct3D;
using DXGI = SharpDX.DXGI;

namespace NextUIPlugin.Gui {
	public class OverlayGui : IDisposable {
		protected const byte BytesPerPixel = 4;

		public readonly Overlay overlay;

		protected bool mouseInWindow;
		protected bool windowFocused;
		protected InputModifier modifier;
		protected ImGuiMouseCursor cursor;
		protected bool captureCursor;
		public bool acceptFocus;
		protected TextureWrap? textureWrap;

		protected D3D11.Texture2D? texture;
		protected D3D11.Texture2D? popupTexture;
		protected ConcurrentBag<D3D11.Texture2D> obsoleteTextures = new();

		protected bool popupVisible;
		protected XRect? popupRect;


		public OverlayGui(
			Overlay overlay
		) {
			this.overlay = overlay;
			BuildTextureWrap();
			overlay.CursorChange += SetCursor;
			overlay.Paint += OnPaint;
			overlay.PopupSize += OnPopupSize;
			overlay.PopupShow += OnPopupShow;
			overlay.SizeChange += OnSizeChange;
		}

		protected void OnPopupShow(object? sender, bool show) {
			popupVisible = show;
		}

		protected void OnPopupSize(object? sender, PopupSizeRequest r) {
			return;
			if (texture == null) {
				return;
			}
			popupRect = r.rect;

			// I'm really not sure if this happens. If it does,
			// frequently - will probably need 2x shared textures and some jazz.
			var texDesc = texture.Description;
			PluginLog.Log("POP " + r.rect + " " + texDesc.Width + " " + texDesc.Height);
			if (r.rect.width > texDesc.Width || r.rect.height > texDesc.Height) {
				PluginLog.Warning(
					$"Trying to build popup layer ({r.rect.width}x{r.rect.height}) larger than primary surface ({texDesc.Width}x{texDesc.Height})."
				);
				return;
			}

			// Get a reference to the old texture, we'll make sure to assign a new texture before disposing the old one.
			var oldTexture = popupTexture;

			//This operation takes time, have to make sure nobody uses that tex before we rebuild it
			popupTexture = null;
			// Build a texture for the new sized popup
			popupTexture = new D3D11.Texture2D(texture.Device, new D3D11.Texture2DDescription() {
				Width = r.rect.width,
				Height = r.rect.height,
				MipLevels = 1,
				ArraySize = 1,
				Format = DXGI.Format.B8G8R8A8_UNorm,
				SampleDescription = new DXGI.SampleDescription(1, 0),
				Usage = D3D11.ResourceUsage.Dynamic,
				BindFlags = D3D11.BindFlags.ShaderResource,
				CpuAccessFlags = D3D11.CpuAccessFlags.Write,
				OptionFlags = D3D11.ResourceOptionFlags.None,
			});

			oldTexture?.Dispose();
		}

		protected void OnSizeChange(object? sender, Size obj) {
			// var oldTexture = texture;
			// return;
			BuildTextureWrap();

			// if (oldTexture != null) {
			// 	obsoleteTextures.Add(oldTexture);
			// }
		}

		protected bool paiting;
		protected PaintRequest paintRequest;
		protected void OnPaint(object? sender, PaintRequest r) {
			if (paiting) {
				return;
			}
			if (r.type != PaintType.View) {
				return;
			}
			paintRequest = r;
			return;

			paiting = true;
			PluginLog.Log("0 PAINT");
			// Calculate offset multipliers for the current buffer
			var rowPitch = r.width * BytesPerPixel;
			var depthPitch = rowPitch * r.height;

			var targetTexture = r.type == PaintType.View ? texture : popupTexture;
			if (r.type != PaintType.View) {
				return;
			}
			if (targetTexture == null) {
				paiting = false;
				return;
			}

			// targetTexture = textureClone;
			PluginLog.Log("1 PAINT " + r.buffer.ToInt64());
			// Build the destination region for the dirty rect that we'll draw to
			var texDesc = targetTexture.Description;
			var sourceRegionPtr = r.buffer + (r.dirtyRect.x * BytesPerPixel) + (r.dirtyRect.y * rowPitch);
			var destinationRegion = new D3D11.ResourceRegion {
				Top = Math.Min(r.dirtyRect.y, texDesc.Height),
				Bottom = Math.Min(r.dirtyRect.y + r.dirtyRect.height, texDesc.Height),
				Left = Math.Min(r.dirtyRect.x, texDesc.Width),
				Right = Math.Min(r.dirtyRect.x + r.dirtyRect.width, texDesc.Width),
				Front = 0,
				Back = 1,
			};

			// Draw to the target

			var context = targetTexture.Device.ImmediateContext;
			context.UpdateSubresource(targetTexture, 0, destinationRegion, sourceRegionPtr, rowPitch, depthPitch);

			// Only need to do composition + flush on primary texture
			if (r.type != PaintType.View) {
				paiting = false;
				return;
			}

			// Intersect with dirty?
			if (popupVisible && popupTexture != null && popupRect != null) {
				//context.CopySubresourceRegion(popupTexture, 0, null, targetTexture, 0, popupRect.x, popupRect.y);
			}

			// No idea why this dies, no idea if it's needed
			context.Flush();

			// Rendering is complete, clean up any obsolete textures
			var textures = obsoleteTextures;
			obsoleteTextures = new ConcurrentBag<D3D11.Texture2D>();
			foreach (var tex in textures) {
				tex.Dispose();
			}
			paiting = false;
		}

		// protected static D3D11.Device? customDevice;
		// protected  D3D11.Texture2D textureClone;
		public void BuildTextureWrap() {
			PluginLog.Log("0 BUILDING " + overlay.Size);
			var oldTexture = texture;

			// if (customDevice == null) {
			// 	var flags = D3D11.DeviceCreationFlags.BgraSupport;
			// 	flags |= D3D11.DeviceCreationFlags.Debug;
			//
			// 	var dxgiDevice = DxHandler.Device.QueryInterface<DXGI.Device>();
			// 	customDevice = new D3D11.Device(dxgiDevice.Adapter, flags);
			// }
			//
			// textureClone = new D3D11.Texture2D(customDevice, new D3D11.Texture2DDescription() {
			// 	Width = overlay.Size.Width,
			// 	Height = overlay.Size.Height,
			// 	MipLevels = 1,
			// 	ArraySize = 1,
			// 	Format = DXGI.Format.B8G8R8A8_UNorm,
			// 	SampleDescription = new DXGI.SampleDescription(1, 0),
			// 	Usage = D3D11.ResourceUsage.Default,
			// 	BindFlags = D3D11.BindFlags.ShaderResource,
			// 	CpuAccessFlags = D3D11.CpuAccessFlags.None,
			// 	OptionFlags = D3D11.ResourceOptionFlags.Shared,
			// });

			texture = new D3D11.Texture2D(DxHandler.Device, new D3D11.Texture2DDescription() {
				Width = overlay.Size.Width,
				Height = overlay.Size.Height,
				MipLevels = 1,
				ArraySize = 1,
				Format = DXGI.Format.B8G8R8A8_UNorm,
				SampleDescription = new DXGI.SampleDescription(1, 0),
				Usage = D3D11.ResourceUsage.Default,
				BindFlags = D3D11.BindFlags.ShaderResource,
				CpuAccessFlags = D3D11.CpuAccessFlags.Write,
				OptionFlags = D3D11.ResourceOptionFlags.None,
			});

			var view = new D3D11.ShaderResourceView(
				DxHandler.Device,
				texture,
				new D3D11.ShaderResourceViewDescription {
					Format = texture.Description.Format,
					Dimension = D3D.ShaderResourceViewDimension.Texture2D,
					Texture2D = { MipLevels = texture.Description.MipLevels },
				}
			);

			textureWrap = new D3DTextureWrap(view, texture.Description.Width, texture.Description.Height);

			if (oldTexture != null) {
				obsoleteTextures.Add(oldTexture);
			}
			PluginLog.Log("1 BUILT");
		}

		public void Dispose() {
			overlay.CursorChange -= SetCursor;
			overlay.Paint -= OnPaint;
			overlay.PopupSize -= OnPopupSize;
			overlay.PopupShow -= OnPopupShow;
			overlay.SizeChange -= OnSizeChange;
			overlay.Dispose();
			textureWrap?.Dispose();
		}


		public void Navigate(string newUrl) {
			overlay.Navigate(newUrl);
		}

		public void Debug() {
			overlay.Debug();
		}

		public void SetCursor(object? sender, Cursor newCursor) {
			return;
			captureCursor = newCursor != Cursor.BrowserHostNoCapture;
			cursor = DecodeCursor(newCursor);
			// overlay.SetCursor();
		}

		public (bool, long) WndProcMessage(WindowsMessage msg, ulong wParam, long lParam) {
			return (false, 0);
			// Check if there was a click, and use it to set the window focused state
			// We're avoiding ImGui for this, as we want to check for clicks entirely outside
			// ImGui's pervue to defocus inlays
			if (msg == WindowsMessage.WmLButtonDown) {
				windowFocused = mouseInWindow && captureCursor;
			}

			// Bail if we're not focused or we're typethrough
			// TODO: Revisit the focus check for UI stuff, might not hold
			if (!windowFocused || overlay.TypeThrough) {
				return (false, 0);
			}

			KeyEventType? eventType = msg switch {
				WindowsMessage.WmKeyDown => KeyEventType.KeyDown,
				WindowsMessage.WmSysKeyDown => KeyEventType.KeyDown,
				WindowsMessage.WmKeyUp => KeyEventType.KeyUp,
				WindowsMessage.WmSysKeyUp => KeyEventType.KeyUp,
				WindowsMessage.WmChar => KeyEventType.Character,
				WindowsMessage.WmSysChar => KeyEventType.Character,
				_ => null,
			};

			// If the event isn't something we're tracking, bail early with no capture
			if (eventType == null) {
				return (false, 0);
			}

			var isSystemKey =
				msg is WindowsMessage.WmSysKeyDown or WindowsMessage.WmSysKeyUp or WindowsMessage.WmSysChar;

			// TODO: Technically this is only firing once, because we're checking focused before this point,
			// but having this logic essentially duped per-inlay is a bit eh. Dedupe at higher point?
			var modifierAdjust = InputModifier.None;
			if (wParam == (int)VirtualKey.Shift) {
				modifierAdjust |= InputModifier.Shift;
			}

			if (wParam == (int)VirtualKey.Control) {
				modifierAdjust |= InputModifier.Control;
			}

			// SYS* messages signal alt is held (really?)
			if (isSystemKey) {
				modifierAdjust |= InputModifier.Alt;
			}

			if (eventType == KeyEventType.KeyDown) {
				modifier |= modifierAdjust;
			}
			else if (eventType == KeyEventType.KeyUp) {
				modifier &= ~modifierAdjust;
			}

			overlay.RequestKeyEvent(new KeyEventRequest() {
				// Guid = RenderGuid,
				keyEventType = eventType.Value,
				systemKey = isSystemKey,
				userKeyCode = (int)wParam,
				nativeKeyCode = (int)lParam,
				modifier = modifier,
			});

			// We've handled the input, signal. For these message types, `0` signals a capture.
			return (acceptFocus, 0);
		}

		public void Render() {
			if (overlay.Hidden || overlay.Toggled) {
				mouseInWindow = false;
				return;
			}

			if (textureWrap == null || texture == null) {
				return;
			}

			/*
			var conditions = NextUIPlugin.Condition;
			if (
				!overlay.Visibility.HasFlag(OverlayVisibility.DuringCutscene) &&
				(
					conditions[ConditionFlag.OccupiedInCutSceneEvent] ||
					conditions[ConditionFlag.WatchingCutscene78]
				)
			) {
				mouseInWindow = false;
				return;
			}

			if (
				!overlay.Visibility.HasFlag(OverlayVisibility.InCombat) &&
				conditions[ConditionFlag.InCombat]
			) {
				mouseInWindow = false;
				return;
			}

			if (
				!overlay.Visibility.HasFlag(OverlayVisibility.InDeepDungeon) &&
				conditions[ConditionFlag.InDeepDungeon]
			) {
				mouseInWindow = false;
				return;
			}

			if (
				!overlay.Visibility.HasFlag(OverlayVisibility.InPVP) &&
				conditions[ConditionFlag.PvPDisplayActive]
			) {
				mouseInWindow = false;
				return;
			}

			if (
				!overlay.Visibility.HasFlag(OverlayVisibility.InGroup) &&
				NextUIPlugin.PartyList.Length > 0
			) {
				mouseInWindow = false;
				return;
			}
*/
			ImGui.SetNextWindowPos(new Vector2(overlay.Position.X, overlay.Position.Y), ImGuiCond.Always);
			ImGui.SetNextWindowSize(new Vector2(overlay.Size.Width, overlay.Size.Height), ImGuiCond.Always);
			ImGui.Begin($"NUOverlay-{overlay.Guid}", GetWindowFlags());

			var rowPitch = paintRequest.width * BytesPerPixel;
			var depthPitch = rowPitch * paintRequest.height;

			var texDesc = texture.Description;
			var sourceRegionPtr = paintRequest.buffer + (paintRequest.dirtyRect.x * BytesPerPixel) + (paintRequest.dirtyRect.y * rowPitch);
			var destinationRegion = new D3D11.ResourceRegion {
				Top = Math.Min(paintRequest.dirtyRect.y, texDesc.Height),
				Bottom = Math.Min(paintRequest.dirtyRect.y + paintRequest.dirtyRect.height, texDesc.Height),
				Left = Math.Min(paintRequest.dirtyRect.x, texDesc.Width),
				Right = Math.Min(paintRequest.dirtyRect.x + paintRequest.dirtyRect.width, texDesc.Width),
				Front = 0,
				Back = 1,
			};

			// Draw to the target

			var context = texture.Device.ImmediateContext;
			context.UpdateSubresource(texture, 0, destinationRegion, sourceRegionPtr, rowPitch, depthPitch);

			ImGui.Image(textureWrap.ImGuiHandle, new Vector2(textureWrap.Width, textureWrap.Height));

			HandleMouseEvent();

			// Handle dynamic resize, if size is the same value won't change
			var wSize = ImGui.GetWindowSize();
			var newSize = new Size((int)wSize.X, (int)wSize.Y);
			overlay.Size = newSize;

			ImGui.End();
		}

		protected ImGuiWindowFlags GetWindowFlags() {
			var flags =
				ImGuiWindowFlags.None
				| ImGuiWindowFlags.NoTitleBar
				| ImGuiWindowFlags.NoCollapse
				| ImGuiWindowFlags.NoScrollbar
				| ImGuiWindowFlags.NoScrollWithMouse
				| ImGuiWindowFlags.NoBringToFrontOnFocus
				| ImGuiWindowFlags.NoFocusOnAppearing;

			// ClickThrough is implicitly locked
			var locked = overlay.Locked || overlay.ClickThrough || overlay.FullScreen;

			if (locked) {
				flags |=
					ImGuiWindowFlags.None
					| ImGuiWindowFlags.NoMove
					| ImGuiWindowFlags.NoResize
					| ImGuiWindowFlags.NoBackground; //TODO: Change it
			}

			if (overlay.ClickThrough || (!captureCursor && locked)) {
				flags |= ImGuiWindowFlags.NoMouseInputs | ImGuiWindowFlags.NoNav;
			}

			return flags;
		}

		private void HandleMouseEvent() {
			// Totally skip mouse handling for click through inlays, as well
			if (overlay.ClickThrough) {
				//  || inlayConfig.ClickThrough
				return;
			}

			var io = ImGui.GetIO();
			var windowPos = ImGui.GetWindowPos();
			var mousePos = io.MousePos - windowPos - ImGui.GetWindowContentRegionMin();

			// Generally we want to use IsWindowHovered for hit checking, as it takes z-stacking into account -
			// but when cursor isn't being actively captured, imgui will always return false - so fall back
			// so a slightly more naive hover check, just to maintain a bit of flood prevention.
			// TODO: Need to test how this will handle overlaps... fully transparent _shouldn't_ be accepting
			//       clicks so shouuulllddd beee fineee???
			var hovered = captureCursor
				? ImGui.IsWindowHovered()
				: ImGui.IsMouseHoveringRect(windowPos, windowPos + ImGui.GetWindowSize());

			// If the cursor is outside the window, send a final mouse leave then noop
			if (!hovered) {
				if (mouseInWindow) {
					mouseInWindow = false;
					overlay.RequestMouseEvent(new MouseEventRequest {
						x = mousePos.X,
						y = mousePos.Y,
						leaving = true,
					});
				}

				return;
			}

			mouseInWindow = true;

			ImGui.SetMouseCursor(cursor);

			var mouseDown = EncodeMouseButtons(io.MouseClicked);
			var mouseDouble = EncodeMouseButtons(io.MouseDoubleClicked);
			var mouseUp = EncodeMouseButtons(io.MouseReleased);
			var wheelX = io.MouseWheelH;
			var wheelY = io.MouseWheel;

			// If the event boils down to no change, bail before sending
			if (
				io.MouseDelta == Vector2.Zero &&
				mouseDown == MouseButton.None &&
				mouseDouble == MouseButton.None &&
				mouseUp == MouseButton.None &&
				wheelX == 0 &&
				wheelY == 0
			) {
				return;
			}

			var inputModifier = InputModifier.None;
			if (io.KeyShift) {
				inputModifier |= InputModifier.Shift;
			}

			if (io.KeyCtrl) {
				inputModifier |= InputModifier.Control;
			}

			if (io.KeyAlt) {
				inputModifier |= InputModifier.Alt;
			}

			// TODO: Either this or the entire handler function should be asynchronous so we're not blocking the entire draw thread
			overlay.RequestMouseEvent(new MouseEventRequest() {
				x = mousePos.X,
				y = mousePos.Y,
				mouseDown = mouseDown,
				mouseDouble = mouseDouble,
				mouseUp = mouseUp,
				wheelX = wheelX,
				wheelY = wheelY,
				modifier = inputModifier,
			});
		}

		#region serde

		protected MouseButton EncodeMouseButtons(RangeAccessor<bool> buttons) {
			var result = MouseButton.None;
			if (buttons[0]) {
				result |= MouseButton.Primary;
			}

			if (buttons[1]) {
				result |= MouseButton.Secondary;
			}

			if (buttons[2]) {
				result |= MouseButton.Tertiary;
			}

			if (buttons[3]) {
				result |= MouseButton.Fourth;
			}

			if (buttons[4]) {
				result |= MouseButton.Fifth;
			}

			return result;
		}

		public ImGuiMouseCursor DecodeCursor(Cursor newCursor) {
			// ngl kinda disappointed at the lack of options here
			switch (newCursor) {
				case Cursor.Default: return ImGuiMouseCursor.Arrow;
				case Cursor.None: return ImGuiMouseCursor.None;
				case Cursor.Pointer: return ImGuiMouseCursor.Hand;

				case Cursor.Text:
				case Cursor.VerticalText:
					return ImGuiMouseCursor.TextInput;

				case Cursor.NResize:
				case Cursor.SResize:
				case Cursor.NSResize:
					return ImGuiMouseCursor.ResizeNS;

				case Cursor.EResize:
				case Cursor.WResize:
				case Cursor.EWResize:
					return ImGuiMouseCursor.ResizeEW;

				case Cursor.NEResize:
				case Cursor.SWResize:
				case Cursor.NESWResize:
					return ImGuiMouseCursor.ResizeNESW;

				case Cursor.NWResize:
				case Cursor.SEResize:
				case Cursor.NWSEResize:
					return ImGuiMouseCursor.ResizeNWSE;
			}

			return ImGuiMouseCursor.Arrow;
		}

		#endregion
	}
}