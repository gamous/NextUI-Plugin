using ImGuiNET;
using System;
using System.Collections.Concurrent;
using System.Drawing;
using System.IO;
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
		protected bool disposing;
		protected TextureWrap? textureWrap;

		protected D3D11.Texture2D? texture;

		// protected IDisposable sizeChangeSub;

		public OverlayGui(
			Overlay overlay
		) {
			this.overlay = overlay;
			BuildTextureWrap(overlay.Size);
			overlay.CursorChange += SetCursor;
			overlay.Paint += OnPaint;
			overlay.Remove += OnRemove;

			// sizeChangeSub = overlay.SizeChange.AsObservable().Subscribe(OnSizeChange);
			// .Throttle(TimeSpan.FromMilliseconds(300))
		}

		protected void OnRemove(object? sender, EventArgs e) {
			Dispose();
		}
		//
		// protected void OnSizeChange(Size obj) {
		// 	BuildTextureWrap();
		// }

		public void BuildTextureWrap(Size size) {
			PluginLog.Log("0 BUILDING " + size);
			var oldTexture = texture;

			texture = new D3D11.Texture2D(DxHandler.Device, new D3D11.Texture2DDescription() {
				Width = size.Width,
				Height = size.Height,
				MipLevels = 1,
				ArraySize = 1,
				Format = DXGI.Format.B8G8R8A8_UNorm,
				SampleDescription = new DXGI.SampleDescription(1, 0),
				Usage = D3D11.ResourceUsage.Dynamic,
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

			//overlay.Resizing = false;
			PluginLog.Log("1 BUILT");
		}

		public void Dispose() {
			PluginLog.Log("Disposing overlay GUI");
			disposing = true;
			overlay.CursorChange -= SetCursor;
			overlay.Paint -= OnPaint;
			overlay.Remove -= OnRemove;
			// sizeChangeSub?.Dispose();
			textureWrap?.Dispose();
			texture?.Dispose();
			PluginLog.Log("Disposed overlay GUI");

			NextUIPlugin.guiManager.RemoveOverlay(this);

			// After gui has been disposed, dispose browser
			overlay.BrowserDisposeRequest();
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
			if (disposing) {
				return (false, 0);
			}

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

		protected bool? ShouldShow(OverlayVisibility visibility) {
			// If there is nothing selected
			if (visibility == 0) {
				return null;
			}

			var conditions = NextUIPlugin.condition;
			if (
				visibility.HasFlag(OverlayVisibility.DuringCutscene) &&
				(
					conditions[ConditionFlag.OccupiedInCutSceneEvent] ||
					conditions[ConditionFlag.WatchingCutscene78]
				)
			) {
				return true;
			}

			if (
				visibility.HasFlag(OverlayVisibility.InCombat) &&
				conditions[ConditionFlag.InCombat]
			) {
				return true;
			}

			if (
				visibility.HasFlag(OverlayVisibility.InDeepDungeon) &&
				conditions[ConditionFlag.InDeepDungeon]
			) {
				return true;
			}

			if (
				visibility.HasFlag(OverlayVisibility.InPVP) &&
				conditions[ConditionFlag.PvPDisplayActive]
			) {
				return true;
			}

			if (
				visibility.HasFlag(OverlayVisibility.InGroup) &&
				NextUIPlugin.partyList.Length > 0
			) {
				return true;
			}

			return false;
		}

		public void Render() {
			if (overlay.Hidden || overlay.Toggled || overlay.Resizing || disposing) {
				mouseInWindow = false;
				return;
			}

			if (textureWrap == null || texture == null) {
				return;
			}

			var shouldShow = ShouldShow(overlay.VisibilityShow);
			var shouldHide = ShouldShow(overlay.VisibilityHide);
			if (shouldShow is false || shouldHide is true) {
				mouseInWindow = false;
				return;
			}

			ImGui.SetNextWindowPos(new Vector2(overlay.Position.X, overlay.Position.Y), ImGuiCond.Always);
			ImGui.SetNextWindowSize(new Vector2(overlay.Size.Width, overlay.Size.Height), ImGuiCond.Always);
			ImGui.Begin($"NUOverlay-{overlay.Guid}", GetWindowFlags());

			RenderBuffer();
			HandleMouseEvents();

			// Handle dynamic resize, if size is the same value won't change
			var wSize = ImGui.GetWindowSize();
			var newSize = new Size((int)wSize.X, (int)wSize.Y);
			overlay.Size = newSize;

			ImGui.End();
		}

		protected IntPtr lastBuffer;
		protected bool needRepaint;
		protected bool sizeChanged;
		protected int bufferWidth;
		protected int bufferHeight;

		protected void OnPaint(object? sender, PaintRequest r) {
			if (disposing) {
				return;
			}

			needRepaint = true;
			lastBuffer = r.buffer;
			if (r.width != bufferWidth || r.height != bufferHeight) {
				sizeChanged = true;
				bufferWidth = r.width;
				bufferHeight = r.height;
			}
		}

		protected unsafe void RenderBuffer() {
			lock (overlay.renderLock) {
				if (sizeChanged) {
					BuildTextureWrap(new Size(bufferWidth, bufferHeight));
					sizeChanged = false;
				}

				if (needRepaint && lastBuffer != IntPtr.Zero) {
					// // we know texture is not null
					var texDesc = texture!.Description;
					var rowPitch = texDesc.Width * BytesPerPixel;
					var depthPitch = rowPitch * texDesc.Height;

					var context = texture.Device.ImmediateContext;

					var box = context.MapSubresource(texture, 0, D3D11.MapMode.WriteDiscard, 0, out DataStream mapped);

					if (box.RowPitch == rowPitch) {
						Buffer.MemoryCopy((void*)lastBuffer, (void*)mapped.DataPointer, mapped.Length, depthPitch);
					}
					else {
						for (var i = 0; i < texDesc.Height; i++) {
							Buffer.MemoryCopy(
								(void*)(lastBuffer + (i * rowPitch)),
								(void*)(mapped.DataPointer + (i * box.RowPitch)),
								box.RowPitch,
								rowPitch
							);
						}
					}

					context.UnmapSubresource(texture, 0);

					needRepaint = false;
				}
			}

			ImGui.Image(textureWrap!.ImGuiHandle, new Vector2(textureWrap.Width, textureWrap.Height));
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

		protected void HandleMouseEvents() {
			// Totally skip mouse handling for click through inlays, as well
			if (overlay.ClickThrough) {
				return;
			}

			var io = ImGui.GetIO();
			var windowPos = ImGui.GetWindowPos();
			var mousePos = io.MousePos - windowPos - ImGui.GetWindowContentRegionMin();

			var hovered = HandleMouseLeave(windowPos, mousePos);

			// If we are outside of window do not process other events
			if (!hovered) {
				return;
			}

			ImGui.SetMouseCursor(cursor);

			var inputModifier = GetInputModifier(io);

			HandleMouseMoveEvent(io, mousePos, inputModifier);
			HandleMouseWheelEvent(io, mousePos, inputModifier);
			HandleMouseClickEvent(io, mousePos, inputModifier);
		}

		protected static InputModifier GetInputModifier(ImGuiIOPtr io) {
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

			if (io.MouseDown[0]) {
				inputModifier |= InputModifier.MouseLeft;
			}

			if (io.MouseDown[1]) {
				inputModifier |= InputModifier.MouseRight;
			}

			if (io.MouseDown[2]) {
				inputModifier |= InputModifier.MouseMiddle;
			}

			return inputModifier;
		}

		protected void HandleMouseMoveEvent(ImGuiIOPtr io, Vector2 mousePos, InputModifier inputModifier) {
			if (io.MouseDelta == Vector2.Zero) {
				return;
			}

			overlay.RequestMouseMoveEvent(new MouseMoveEventRequest {
				x = mousePos.X,
				y = mousePos.Y,
				modifier = inputModifier,
			});
		}

		protected readonly bool[] prevMouseState = new bool[3];

		protected void HandleMouseClickEvent(ImGuiIOPtr io, Vector2 mousePos, InputModifier inputModifier) {
			for (var i = 0; i < 3; i++) {
				var stateChanged = io.MouseDown[i] != prevMouseState[i];
				var isUp = !io.MouseDown[i];

				if (!stateChanged) {
					continue;
				}

				overlay.RequestMouseClickEvent(new MouseClickEventRequest {
					x = mousePos.X,
					y = mousePos.Y,
					mouseButtonType = (MouseButtonType)i,
					isUp = isUp,
					clickCount = io.MouseDoubleClicked[i] ? 2 : 1,
					modifier = inputModifier
				});

				prevMouseState[i] = io.MouseDown[i];
			}
		}

		protected bool HandleMouseLeave(Vector2 windowPos, Vector2 mousePos) {
			var hovered = captureCursor
				? ImGui.IsWindowHovered()
				: ImGui.IsMouseHoveringRect(windowPos, windowPos + ImGui.GetWindowSize());

			// If the cursor is outside the window, send a final mouse leave then noop
			if (!hovered && mouseInWindow) {
				overlay.RequestMouseLeaveEvent(new MouseLeaveEventRequest {
					x = mousePos.X,
					y = mousePos.Y,
				});
			}

			mouseInWindow = hovered;
			return hovered;
		}

		protected void HandleMouseWheelEvent(ImGuiIOPtr io, Vector2 mousePos, InputModifier inputModifier) {
			var wheelX = io.MouseWheelH;
			var wheelY = io.MouseWheel;

			if (wheelX == 0 && wheelY == 0) {
				return;
			}

			overlay.RequestMouseWheelEvent(new MouseWheelEventRequest {
				x = mousePos.X,
				y = mousePos.Y,
				wheelX = wheelX,
				wheelY = wheelY,
				modifier = inputModifier
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