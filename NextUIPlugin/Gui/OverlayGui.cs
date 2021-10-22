using ImGuiNET;
using System;
using System.Collections.Concurrent;
using System.Drawing;
using System.Numerics;
using System.Reactive.Linq;
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

		protected IDisposable sizeChangeSub;

		public OverlayGui(
			Overlay overlay
		) {
			this.overlay = overlay;
			BuildTextureWrap();
			overlay.CursorChange += SetCursor;
			overlay.Paint += OnPaint;
			// overlay.PopupSize += OnPopupSize;
			// overlay.PopupShow += OnPopupShow;

			sizeChangeSub = overlay.SizeChange.AsObservable()
				.Throttle(TimeSpan.FromMilliseconds(300)).Subscribe(OnSizeChange);
		}

		protected void OnSizeChange(Size obj) {
			BuildTextureWrap();
		}

		protected bool paiting;
		protected PaintRequest paintRequest;
		protected void OnPaint(object? sender, PaintRequest r) {
			// if (paiting) {
			// 	return;
			// }
			paintRequest = r;
		}

		// protected static D3D11.Device? customDevice;
		// protected  D3D11.Texture2D textureClone;
		public void BuildTextureWrap() {
			PluginLog.Log("0 BUILDING " + overlay.Size);
			var oldTexture = texture;

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
				oldTexture.Dispose();
			}

			overlay.Resizing = false;
			PluginLog.Log("1 BUILT");
		}

		public void Dispose() {
			overlay.CursorChange -= SetCursor;
			overlay.Paint -= OnPaint;
			// overlay.PopupSize -= OnPopupSize;
			// overlay.PopupShow -= OnPopupShow;
			sizeChangeSub?.Dispose();
			overlay.Dispose();
			textureWrap?.Dispose();
			texture?.Dispose();
		}


		public void Navigate(string newUrl) {
			overlay.Navigate(newUrl);
		}

		public void Debug() {
			overlay.Debug();
		}

		public void SetCursor(object? sender, Cursor newCursor) {
			captureCursor = newCursor != Cursor.BrowserHostNoCapture;
			cursor = DecodeCursor(newCursor);
			// overlay.SetCursor();
		}

		public (bool, long) WndProcMessage(WindowsMessage msg, ulong wParam, long lParam) {
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
			if (overlay.Hidden || overlay.Toggled || overlay.Resizing) {
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