using ImGuiNET;
using System;
using System.Drawing;
using System.Numerics;
using System.Reactive.Linq;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Logging;
using ImGuiScene;
using NextUIPlugin.Cef;
using NextUIPlugin.Cef.App;
using NextUIPlugin.Data.Input;
using NextUIPlugin.Model;
using NextUIPlugin.Service;
using SharpDX;
using Xilium.CefGlue;
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

		protected readonly NUCefClient client;
		protected CefBrowser? browser;
		protected IDisposable? sizeObservableSub;
		protected bool BrowserLoading => browser == null || browser.IsLoading;

		public OverlayGui(
			Overlay overlay
		) {
			this.overlay = overlay;
			BuildTextureWrap(overlay.Size);

			client = new NUCefClient(overlay);
			// sizeChangeSub = overlay.SizeChange.AsObservable().Subscribe(OnSizeChange);
			// .Throttle(TimeSpan.FromMilliseconds(300))

			Initialize();
		}

		public void Initialize() {
			var windowInfo = CefWindowInfo.Create();
			windowInfo.SetAsWindowless(IntPtr.Zero, true);
			windowInfo.WindowlessRenderingEnabled = true;
			PluginLog.Log($"WindowInfo {overlay.Size.Width}, {overlay.Size.Height}");
			windowInfo.Bounds = new CefRectangle(0, 0, overlay.Size.Width, overlay.Size.Height);
			windowInfo.Hidden = false;
			windowInfo.SharedTextureEnabled = true;

			var browserSettings = new CefBrowserSettings {
				WindowlessFrameRate = 60,
			};

			client.lifeSpanHandler.AfterBrowserLoad += LifeSpanHandlerOnAfterBrowserLoad;

			CefBrowserHost.CreateBrowser(
				windowInfo,
				client,
				browserSettings,
				overlay.Url
			);
		}

		protected void LifeSpanHandlerOnAfterBrowserLoad(CefBrowser cefBrowser) {
			browser = cefBrowser;
			PluginLog.Log(
				$"BR CREATED {browser.IsLoading} {browser.IsValid} " +
				$"{browser.FrameCount} {browser.HasDocument} {browser?.GetMainFrame().Url}"
			);

			overlay.UrlChange += Navigate;

			sizeObservableSub = overlay.SizeChange.AsObservable()
				.Throttle(TimeSpan.FromMilliseconds(300)).Subscribe(Resize);

			// Also request cursor if it changes
			client.displayHandler.CursorChanged += SetCursor;
			client.renderHandler.Paint += OnPaint;

			client.lifeSpanHandler.AfterBrowserLoad -= LifeSpanHandlerOnAfterBrowserLoad;
		}

		public void Navigate(object? sender, string newUrl) {
			// If navigating to the same url, force a clean reload
			if (browser?.GetMainFrame().Url == newUrl) {
				PluginLog.Log($"RELOAD {newUrl}");
				browser.ReloadIgnoreCache();
				return;
			}

			// Otherwise load regularly
			browser?.GetMainFrame().LoadUrl(newUrl);
			PluginLog.Log($"LOAD {newUrl}");
		}

		public void Reload() {
			browser?.ReloadIgnoreCache();
		}

		public void Debug() {
			ShowDevTools();
		}

		protected void ShowDevTools() {
			if (browser == null) {
				return;
			}

			var host = browser.GetHost();
			var wi = CefWindowInfo.Create();
			wi.SetAsPopup(IntPtr.Zero, "DevTools");
			host.ShowDevTools(wi, new DevToolsWebClient(), new CefBrowserSettings(), new CefPoint(0, 0));
		}

		public void Resize(Size size) {
			//PluginLog.Log("CREATED WITH ZIE " + overlay.Size);
			overlay.Resizing = true;
			// Need to resize renderer first, the browser will check it (and hence the texture) when browser.
			// We are disregarding param as Size will adjust based on Fullscreen prop
			client.renderHandler.Resize(overlay.Size);
			browser?.GetHost().WasResized();
			browser?.GetHost().NotifyScreenInfoChanged();
			// browser?.GetHost().Invalidate(CefPaintElementType.View);
			// if (browser != null) {
			//browser.Size = overlay.Size;
			// }
		}

		protected void HandleMouseMoveEvent(
			float x,
			float y,
			InputModifier inputModifier
		) {
			if (BrowserLoading) {
				return;
			}

			var scaledCursor = DpiScaling.ScaleViewPoint(x, y);
			client.displayHandler.SetMousePosition(scaledCursor.X, scaledCursor.Y);
			var evt = new CefMouseEvent(scaledCursor.X, scaledCursor.Y, DecodeInputModifier(inputModifier));

			browser?.GetHost().SendMouseMoveEvent(evt, false);
		}

		protected void HandleMouseClickEvent(
			float x, float y,
			MouseButtonType mouseButtonType,
			bool isUp,
			int clickCount,
			InputModifier inputModifier
		) {
			if (BrowserLoading) {
				return;
			}

			var scaledCursor = DpiScaling.ScaleViewPoint(x, y);
			var evt = new CefMouseEvent(scaledCursor.X, scaledCursor.Y, DecodeInputModifier(inputModifier));

			browser?.GetHost().SendMouseClickEvent(
				evt,
				DecodeButtonType(mouseButtonType),
				isUp,
				clickCount
			);
		}

		protected void HandleMouseWheelEvent(
			float x,
			float y,
			float wheelX,
			float wheelY,
			InputModifier inputModifier
		) {
			if (BrowserLoading) {
				return;
			}

			var scaledCursor = DpiScaling.ScaleViewPoint(x, y);
			var evt = new CefMouseEvent(scaledCursor.X, scaledCursor.Y, DecodeInputModifier(inputModifier));

			// CEF treats the wheel delta as mode 0, pixels. Bump up the numbers to match typical in-browser experience.
			const int deltaMult = 100;
			browser?.GetHost().SendMouseWheelEvent(
				evt,
				(int)wheelX * deltaMult,
				(int)wheelY * deltaMult
			);
		}

		protected void HandleMouseLeaveEvent(float x, float y) {
			if (BrowserLoading) {
				return;
			}

			var scaledCursor = DpiScaling.ScaleViewPoint(x, y);
			var evt = new CefMouseEvent(scaledCursor.X, scaledCursor.Y, CefEventFlags.None);

			browser?.GetHost().SendMouseMoveEvent(evt, true);
		}

		public void HandleKeyEvent(
			KeyEventType keyEventType,
			InputModifier inputModifier,
			int userKeyCode,
			int nativeKeyCode,
			bool systemKey
		) {
			if (BrowserLoading) {
				return;
			}

			var type = keyEventType switch {
				KeyEventType.KeyDown => CefKeyEventType.RawKeyDown,
				KeyEventType.KeyUp => CefKeyEventType.KeyUp,
				KeyEventType.Character => CefKeyEventType.Char,
				_ => throw new ArgumentException($"Invalid KeyEventType {keyEventType}")
			};

			browser?.GetHost().SendKeyEvent(new CefKeyEvent {
				EventType = type,
				Modifiers = DecodeInputModifier(inputModifier),
				WindowsKeyCode = userKeyCode,
				NativeKeyCode = nativeKeyCode,
				IsSystemKey = systemKey,
			});
		}

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
			// sizeChangeSub?.Dispose();
			textureWrap?.Dispose();
			texture?.Dispose();
			PluginLog.Log("Disposed overlay GUI");

			NextUIPlugin.guiManager.RemoveOverlay(this);

			// After gui has been disposed, dispose browser
			BrowserDispose();
		}

		public void BrowserDispose() {
			if (browser == null) {
				return;
			}

			client.displayHandler.CursorChanged -= SetCursor;
			client.renderHandler.Paint -= OnPaint;

			overlay.UrlChange -= Navigate;
			sizeObservableSub?.Dispose();

			client.Dispose();
			browser.Dispose();
			browser = null;
			PluginLog.Log("Browser was disposed");
		}

		public void Navigate(string newUrl) {
			overlay.Navigate(newUrl);
		}

		public void SetCursor(object? sender, Cursor newCursor) {
			captureCursor = newCursor != Cursor.BrowserHostNoCapture;
			cursor = DecodeCursor(newCursor);
		}

		public (bool, long) WndProcMessage(WindowsMessageS msg, ulong userKeyCode, long nativeKeyCode) {
			if (disposing) {
				return (false, 0);
			}

			// Check if there was a click, and use it to set the window focused state
			// We're avoiding ImGui for this, as we want to check for clicks entirely outside
			if (msg == WindowsMessageS.WmLButtonDown) {
				windowFocused = mouseInWindow && captureCursor;
			}

			// Bail if we're not focused or we're typethrough
			// TODO: Revisit the focus check for UI stuff, might not hold
			if (!windowFocused || overlay.TypeThrough) {
				return (false, 0);
			}

			KeyEventType? eventType = msg switch {
				WindowsMessageS.WmKeyDown => KeyEventType.KeyDown,
				WindowsMessageS.WmSysKeyDown => KeyEventType.KeyDown,
				WindowsMessageS.WmKeyUp => KeyEventType.KeyUp,
				WindowsMessageS.WmSysKeyUp => KeyEventType.KeyUp,
				WindowsMessageS.WmChar => KeyEventType.Character,
				WindowsMessageS.WmSysChar => KeyEventType.Character,
				_ => null,
			};

			// If the event isn't something we're tracking, bail early with no capture
			if (eventType == null) {
				return (false, 0);
			}

			var isSystemKey =
				msg is WindowsMessageS.WmSysKeyDown or WindowsMessageS.WmSysKeyUp or WindowsMessageS.WmSysChar;

			// TODO: Technically this is only firing once, because we're checking focused before this point,
			// but having this logic essentially duped per-inlay is a bit eh. Dedupe at higher point?
			var modifierAdjust = InputModifier.None;
			if (userKeyCode == (int)VirtualKey.Shift) {
				modifierAdjust |= InputModifier.Shift;
			}

			if (userKeyCode == (int)VirtualKey.Control) {
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

			HandleKeyEvent(
				eventType.Value,
				modifier,
				(int)userKeyCode, // userKeyCode
				(int)nativeKeyCode, // nativeKeyCode
				isSystemKey
			);

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

		protected void OnPaint(
			IntPtr buffer,
			int width,
			int height
		) {
			if (disposing) {
				return;
			}

			needRepaint = true;
			lastBuffer = buffer;
			if (width != bufferWidth || height != bufferHeight) {
				sizeChanged = true;
				bufferWidth = width;
				bufferHeight = height;
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
			if (overlay.ClickThrough) {
				return;
			}

			var io = ImGui.GetIO();
			var windowPos = ImGui.GetWindowPos();
			var mousePos = io.MousePos - windowPos - ImGui.GetWindowContentRegionMin();

			var hovered = HandleImMouseLeave(windowPos, mousePos);

			// If we are outside of window do not process other events
			if (!hovered) {
				return;
			}

			ImGui.SetMouseCursor(cursor);

			var inputModifier = GetInputModifier(io);

			HandleImMouseMoveEvent(io, mousePos, inputModifier);
			HandleImMouseWheelEvent(io, mousePos, inputModifier);
			HandleImMouseClickEvent(io, mousePos, inputModifier);
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

		protected void HandleImMouseMoveEvent(ImGuiIOPtr io, Vector2 mousePos, InputModifier inputModifier) {
			if (io.MouseDelta == Vector2.Zero) {
				return;
			}

			HandleMouseMoveEvent(
				mousePos.X,
				mousePos.Y,
				inputModifier
			);
		}

		protected readonly bool[] prevMouseState = new bool[3];

		protected void HandleImMouseClickEvent(ImGuiIOPtr io, Vector2 mousePos, InputModifier inputModifier) {
			for (var i = 0; i < 3; i++) {
				var stateChanged = io.MouseDown[i] != prevMouseState[i];
				var isUp = !io.MouseDown[i];

				if (!stateChanged) {
					continue;
				}

				HandleMouseClickEvent(
					mousePos.X,
					mousePos.Y,
					(MouseButtonType)i,
					isUp,
					io.MouseDoubleClicked[i] ? 2 : 1,
					inputModifier
				);

				prevMouseState[i] = io.MouseDown[i];
			}
		}

		protected bool HandleImMouseLeave(Vector2 windowPos, Vector2 mousePos) {
			var hovered = captureCursor
				? ImGui.IsWindowHovered()
				: ImGui.IsMouseHoveringRect(windowPos, windowPos + ImGui.GetWindowSize());

			// If the cursor is outside the window, send a final mouse leave then noop
			if (!hovered && mouseInWindow) {
				HandleMouseLeaveEvent(mousePos.X, mousePos.Y);
			}

			mouseInWindow = hovered;
			return hovered;
		}

		protected void HandleImMouseWheelEvent(ImGuiIOPtr io, Vector2 mousePos, InputModifier inputModifier) {
			var wheelX = io.MouseWheelH;
			var wheelY = io.MouseWheel;

			if (wheelX == 0 && wheelY == 0) {
				return;
			}

			HandleMouseWheelEvent(
				mousePos.X,
				mousePos.Y,
				wheelX,
				wheelY,
				inputModifier
			);
		}


		#region Encoding status stuff

		protected static CefEventFlags DecodeInputModifier(InputModifier modifier) {
			var result = CefEventFlags.None;
			if ((modifier & InputModifier.Shift) == InputModifier.Shift) {
				result |= CefEventFlags.ShiftDown;
			}

			if ((modifier & InputModifier.Control) == InputModifier.Control) {
				result |= CefEventFlags.ControlDown;
			}

			if ((modifier & InputModifier.Alt) == InputModifier.Alt) {
				result |= CefEventFlags.AltDown;
			}

			if ((modifier & InputModifier.MouseLeft) == InputModifier.MouseLeft) {
				result |= CefEventFlags.LeftMouseButton;
			}

			if ((modifier & InputModifier.MouseRight) == InputModifier.MouseRight) {
				result |= CefEventFlags.RightMouseButton;
			}

			if ((modifier & InputModifier.MouseMiddle) == InputModifier.MouseMiddle) {
				result |= CefEventFlags.MiddleMouseButton;
			}

			return result;
		}

		protected static CefMouseButtonType DecodeButtonType(MouseButtonType buttonType) {
			switch (buttonType) {
				case MouseButtonType.Middle: return CefMouseButtonType.Middle;
				case MouseButtonType.Right: return CefMouseButtonType.Right;
				default: return CefMouseButtonType.Left;
			}
		}

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