using Dalamud.Logging;
using ImGuiNET;
using System;
using System.Numerics;
using Newtonsoft.Json;
using RendererProcess;
using RendererProcess.Data;
using RendererProcess.Ipc;
using RendererProcess.RenderHandlers;
using RendererProcess.Texture;
using SharedMemory;

namespace NextUIPlugin.Overlay {
	public class Overlay : IDisposable {
		private bool resizing = false;
		private Vector2 size;

		private RenderProcess renderProcess;
		private SharedTextureHandler textureHandler;
		private Exception textureRenderException;

		private bool mouseInWindow;
		private bool windowFocused;
		private InputModifier modifier;
		private ImGuiMouseCursor cursor;
		private bool captureCursor;

		public Overlay(RenderProcess renderProcess) {
			this.renderProcess = renderProcess;
		}

		public void Dispose() {
			textureHandler?.Dispose();
			renderProcess.Send(new RemoveInlayRequest());
		}

		public void Navigate(string newUrl) {
			renderProcess.Send(new NavigateInlayRequest() { Url = newUrl });
		}

		public void Debug() {
			renderProcess.Send(new DebugInlayRequest());
		}

		public void InvalidateTransport() {
			// Get old refs so we can clean up later
			SharedTextureHandler? oldTextureHandler = textureHandler;
			// var oldRenderGuid = RenderGuid;

			// Invalidate the handler, and reset the size to trigger a rebuild
			// Also need to generate a new renderer guid so we don't have a collision during the hand over
			// TODO: Might be able to tweak the logic in resize alongside this to shore up (re)builds
			textureHandler = null;
			size = Vector2.Zero;
			// RenderGuid = Guid.NewGuid();

			// Clean up
			oldTextureHandler.Dispose();
			// renderProcess.Send(new RemoveInlayRequest() { Guid = oldRenderGuid });
		}

		public void SetCursor(Cursor cursor) {
			captureCursor = cursor != Cursor.BrowserHostNoCapture;
			this.cursor = DecodeCursor(cursor);
		}

		public (bool, long) WndProcMessage(WindowsMessage msg, ulong wParam, long lParam) {
			// Check if there was a click, and use it to set the window focused state
			// We're avoiding ImGui for this, as we want to check for clicks entirely outside
			// ImGui's pervue to defocus inlays
			if (msg == WindowsMessage.WM_LBUTTONDOWN) {
				windowFocused = mouseInWindow && captureCursor;
			}

			// Bail if we're not focused or we're typethrough
			// TODO: Revisit the focus check for UI stuff, might not hold
			if (!windowFocused) {
				//  || inlayConfig.TypeThrough
				return (false, 0);
			}

			KeyEventType? eventType = msg switch {
				WindowsMessage.WM_KEYDOWN => KeyEventType.KeyDown,
				WindowsMessage.WM_SYSKEYDOWN => KeyEventType.KeyDown,
				WindowsMessage.WM_KEYUP => KeyEventType.KeyUp,
				WindowsMessage.WM_SYSKEYUP => KeyEventType.KeyUp,
				WindowsMessage.WM_CHAR => KeyEventType.Character,
				WindowsMessage.WM_SYSCHAR => KeyEventType.Character,
				_ => (KeyEventType?)null,
			};

			// If the event isn't something we're tracking, bail early with no capture
			if (eventType == null) {
				return (false, 0);
			}

			bool isSystemKey =
				false
				|| msg == WindowsMessage.WM_SYSKEYDOWN
				|| msg == WindowsMessage.WM_SYSKEYUP
				|| msg == WindowsMessage.WM_SYSCHAR;

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

			renderProcess.Send(new KeyEventRequest() {
				// Guid = RenderGuid,
				keyEventType = eventType.Value,
				systemKey = isSystemKey,
				userKeyCode = (int)wParam,
				nativeKeyCode = (int)lParam,
				modifier = modifier,
			});

			// We've handled the input, signal. For these message types, `0` signals a capture.
			return (true, 0);
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

		private ImGuiWindowFlags GetWindowFlags() {
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

			if ((!captureCursor && locked)) { // inlayConfig.ClickThrough || 
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

			MouseButton down = EncodeMouseButtons(io.MouseClicked);
			MouseButton double_ = EncodeMouseButtons(io.MouseDoubleClicked);
			MouseButton up = EncodeMouseButtons(io.MouseReleased);
			float wheelX = io.MouseWheelH;
			float wheelY = io.MouseWheel;

			// If the event boils down to no change, bail before sending
			if (io.MouseDelta == Vector2.Zero && down == MouseButton.None && double_ == MouseButton.None &&
			    up == MouseButton.None && wheelX == 0 && wheelY == 0) {
				return;
			}

			InputModifier modifier = InputModifier.None;
			if (io.KeyShift) {
				modifier |= InputModifier.Shift;
			}

			if (io.KeyCtrl) {
				modifier |= InputModifier.Control;
			}

			if (io.KeyAlt) {
				modifier |= InputModifier.Alt;
			}

			// TODO: Either this or the entire handler function should be asynchronous so we're not blocking the entire draw thread
			renderProcess.Send(new MouseEventRequest() {
				// Guid = RenderGuid,
				x = mousePos.X,
				y = mousePos.Y,
				mouseDown = down,
				mouseDouble = double_,
				mouseUp = up,
				wheelX = wheelX,
				wheelY = wheelY,
				modifier = modifier,
			});
		}

		protected async void HandleWindowSize() {
			// Vector2 currentSize = ImGui.GetWindowContentRegionMax() - ImGui.GetWindowContentRegionMin();
			if (size != Vector2.Zero || resizing) {
				return;
			}

			var request = new NewInlayRequest() {
				Url = "",
				Width = (int)size.X,
				Height = (int)size.Y,
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
			var oldTextureHandler = textureHandler;
			try {
				string data = System.Text.Encoding.UTF8.GetString(response.Data);
				PluginLog.Log("th " + data);
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

			if (oldTextureHandler != null) {
				oldTextureHandler.Dispose();
			}
		}

		#region serde

		private MouseButton EncodeMouseButtons(RangeAccessor<bool> buttons) {
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

		private ImGuiMouseCursor DecodeCursor(Cursor cursor) {
			// ngl kinda disappointed at the lack of options here
			switch (cursor) {
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