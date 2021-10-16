﻿using ImGuiNET;
using System;
using System.Numerics;
using Dalamud.Logging;
using ImGuiScene;
using NextUIShared;
using NextUIShared.Data;
using NextUIShared.Model;
using NextUIShared.Request;
using D3D11 = SharpDX.Direct3D11;
using D3D = SharpDX.Direct3D;

namespace NextUIPlugin.Gui {
	public class OverlayGui : IDisposable {
		public NextUIShared.Model.Overlay overlay;

		protected bool mouseInWindow;
		protected bool windowFocused;
		protected InputModifier modifier;
		protected ImGuiMouseCursor cursor;
		protected bool captureCursor;
		public bool acceptFocus;
		protected TextureWrap? textureWrap;

		public OverlayGui(
			Overlay overlay
		) {
			this.overlay = overlay;
			BuildTextureWrap();
			overlay.TexturePointerChange += TexturePointerChange;
			overlay.CursorChange += SetCursor;
		}

		protected void TexturePointerChange(IntPtr obj) {
			BuildTextureWrap();
		}

		public void BuildTextureWrap() {
			PluginLog.Log("BuildTextureWrap " + overlay.TexturePointer.ToString());
			if (overlay.TexturePointer == IntPtr.Zero) {
				return;
			}

			D3D11.Texture2D? texture = DxHandler.Device?.OpenSharedResource<D3D11.Texture2D>(overlay.TexturePointer);
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
			overlay.TexturePointerChange -= TexturePointerChange;
			overlay.CursorChange -= SetCursor;
			overlay.Dispose();
			textureWrap?.Dispose();
		}

		public void Navigate(string newUrl) {
			overlay.Navigate(newUrl);
		}

		public void Debug() {
			overlay.Debug();
		}

		public void SetCursor(Cursor newCursor) {
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
			// if (inlayConfig.Hidden) {
			// 	mouseInWindow = false;
			// 	return;
			// }

			if (textureWrap == null) {
				return;
			}

			ImGui.SetNextWindowPos(new Vector2(0, 0), ImGuiCond.Always);
			ImGui.SetNextWindowSize(new Vector2(overlay.Size.Width, overlay.Size.Height), ImGuiCond.Always);
			ImGui.Begin($"NUOverlay-{overlay.Guid}", GetWindowFlags());

			ImGui.Image(textureWrap.ImGuiHandle, new Vector2(textureWrap.Width, textureWrap.Height));

			HandleMouseEvent();

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

			// ClickThrough is implicitly locked\
			var locked = overlay.Locked || overlay.ClickThrough;

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
			// Render proc won't be ready on first boot
			// Totally skip mouse handling for click through inlays, as well
			// if (renderProcess == null) {
			// 	//  || inlayConfig.ClickThrough
			// 	return;
			// }

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