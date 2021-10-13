using Dalamud.Logging;
using ImGuiNET;
using System;
using System.Numerics;
using Newtonsoft.Json;
using RendererProcess.Data;
using RendererProcess.Ipc;
using RendererProcess.RenderHandlers;
using RendererProcess.Texture;
using SharedMemory;

namespace NextUIPlugin.Overlay {
	public class Overlay : IDisposable {
		protected bool resizing;
		protected Vector2 size;

		protected readonly RenderProcess? renderProcess;
		protected SharedTextureHandler? textureHandler;
		protected Exception? textureRenderException;

		protected bool mouseInWindow;
		protected bool windowFocused;
		protected InputModifier modifier;
		protected ImGuiMouseCursor cursor;
		protected bool captureCursor;
		protected bool captureEvents;

		public Overlay(RenderProcess? renderProcess) {
			this.renderProcess = renderProcess;
		}

		public void Dispose() {
			textureHandler?.Dispose();
			renderProcess?.Send(new RemoveInlayRequest());
		}

		public void Navigate(string newUrl) {
			renderProcess?.Send(new NavigateInlayRequest() { url = newUrl });
		}

		public void Debug() {
			renderProcess?.Send(new DebugInlayRequest());
		}

		public void SetCursor(Cursor newCursor) {
			captureCursor = newCursor != Cursor.BrowserHostNoCapture;
			cursor = DecodeCursor(newCursor);
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
			if (!windowFocused) {
				//  || inlayConfig.TypeThrough
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

			bool isSystemKey =
				msg == WindowsMessage.WmSysKeyDown
				|| msg == WindowsMessage.WmSysKeyUp
				|| msg == WindowsMessage.WmSysChar;

			// TODO: Technically this is only firing once, because we're checking focused before this point,
			// but having this logic essentially duped per-inlay is a bit eh. Dedupe at higher point?
			InputModifier modifierAdjust = InputModifier.None;
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

			renderProcess?.Send(new KeyEventRequest() {
				// Guid = RenderGuid,
				keyEventType = eventType.Value,
				systemKey = isSystemKey,
				userKeyCode = (int)wParam,
				nativeKeyCode = (int)lParam,
				modifier = modifier,
			});

			// We've handled the input, signal. For these message types, `0` signals a capture.
			return (!captureEvents, 0);
		}

		public void Render() {
			// if (inlayConfig.Hidden) {
			// 	mouseInWindow = false;
			// 	return;
			// }
			ImGui.SetNextWindowPos(new Vector2(0, 0), ImGuiCond.Always);
			ImGui.SetNextWindowSize(size, ImGuiCond.Always);
			ImGui.Begin("NUOverlay", GetWindowFlags());

			HandleWindowSize();

			// TODO: Renderer can take some time to spin up properly, should add a loading state.
			if (textureHandler != null) {
				HandleMouseEvent();

				textureHandler.Render();
			}
			else if (textureRenderException != null) {
				ImGui.PushStyleColor(ImGuiCol.Text, 0xFF0000FF);
				ImGui.Text("An error occured while building the browser inlay texture:");
				ImGui.Text(textureRenderException.ToString());
				ImGui.PopStyleColor();
			}

			ImGui.End();
		}

		protected ImGuiWindowFlags GetWindowFlags() {
			ImGuiWindowFlags flags =
				ImGuiWindowFlags.None
				| ImGuiWindowFlags.NoTitleBar
				| ImGuiWindowFlags.NoCollapse
				| ImGuiWindowFlags.NoScrollbar
				| ImGuiWindowFlags.NoScrollWithMouse
				| ImGuiWindowFlags.NoBringToFrontOnFocus
				| ImGuiWindowFlags.NoFocusOnAppearing;

			// ClickThrough is implicitly locked
			// var locked = inlayConfig.Locked || inlayConfig.ClickThrough;
			bool locked = true;

			if (locked) {
				flags |=
					ImGuiWindowFlags.None
					| ImGuiWindowFlags.NoMove
					| ImGuiWindowFlags.NoResize
					| ImGuiWindowFlags.NoBackground; //TODO: Change it
			}

			if ((!captureCursor && locked)) {
				// inlayConfig.ClickThrough || 
				flags |= ImGuiWindowFlags.NoMouseInputs | ImGuiWindowFlags.NoNav;
			}

			return flags;
		}

		private void HandleMouseEvent() {
			// Render proc won't be ready on first boot
			// Totally skip mouse handling for click through inlays, as well
			if (renderProcess == null) {
				//  || inlayConfig.ClickThrough
				return;
			}

			ImGuiIOPtr io = ImGui.GetIO();
			Vector2 windowPos = ImGui.GetWindowPos();
			Vector2 mousePos = io.MousePos - windowPos - ImGui.GetWindowContentRegionMin();

			// Generally we want to use IsWindowHovered for hit checking, as it takes z-stacking into account -
			// but when cursor isn't being actively captured, imgui will always return false - so fall back
			// so a slightly more naive hover check, just to maintain a bit of flood prevention.
			// TODO: Need to test how this will handle overlaps... fully transparent _shouldn't_ be accepting
			//       clicks so shouuulllddd beee fineee???
			bool hovered = captureCursor
				? ImGui.IsWindowHovered()
				: ImGui.IsMouseHoveringRect(windowPos, windowPos + ImGui.GetWindowSize());

			// If the cursor is outside the window, send a final mouse leave then noop
			if (!hovered) {
				if (mouseInWindow) {
					mouseInWindow = false;
					renderProcess.Send(new MouseEventRequest() {
						// Guid = RenderGuid,
						x = mousePos.X,
						y = mousePos.Y,
						leaving = true,
					});
				}

				return;
			}

			mouseInWindow = true;

			ImGui.SetMouseCursor(cursor);

			MouseButton mouseDown = EncodeMouseButtons(io.MouseClicked);
			MouseButton mouseDouble = EncodeMouseButtons(io.MouseDoubleClicked);
			MouseButton mouseUp = EncodeMouseButtons(io.MouseReleased);
			float wheelX = io.MouseWheelH;
			float wheelY = io.MouseWheel;

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

			InputModifier inputModifier = InputModifier.None;
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
			renderProcess.Send(new MouseEventRequest() {
				// Guid = RenderGuid,
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

		protected async void HandleWindowSize() {
			// Vector2 currentSize = ImGui.GetWindowContentRegionMax() - ImGui.GetWindowContentRegionMin();
			if (renderProcess == null || size != Vector2.Zero || resizing) {
				return;
			}

			var request = new NewInlayRequest() {
				url = "",
				width = (int)size.X,
				height = (int)size.Y,
			};

			resizing = true;

			RpcResponse response = await renderProcess.SendAsync(request);
			if (!response.Success) {
				PluginLog.LogError("Texture build failure, retrying...");
				resizing = false;
				return;
			}

			if (size == Vector2.Zero) {
				Vector2 vpSize = ImGui.GetMainViewport().Size;
				size = new Vector2(vpSize.X, vpSize.Y);
			}

			resizing = false;

			PluginLog.Log("Setting textureHandler ");
			SharedTextureHandler? oldTextureHandler = textureHandler;
			try {
				string data = System.Text.Encoding.UTF8.GetString(response.Data);
				TextureHandleResponse? obj = JsonConvert.DeserializeObject<TextureHandleResponse>(data);
				if (obj == null) {
					PluginLog.Log("Setting textureHandler FAILED " + data);
				}
				else {
					textureHandler = new SharedTextureHandler(obj);
					PluginLog.Log("Setting textureHandler OK");
				}
			}
			catch (Exception e) {
				textureRenderException = e;
			}

			oldTextureHandler?.Dispose();
		}

		#region serde

		protected MouseButton EncodeMouseButtons(RangeAccessor<bool> buttons) {
			MouseButton result = MouseButton.None;
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