using CefSharp;
using CefSharp.OffScreen;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Reactive.Linq;
using Dalamud.Logging;
using NextUIBrowser.Cef;
using NextUIBrowser.RenderHandlers;
using NextUIShared.Data;
using NextUIShared.Model;
using NextUIShared.Request;
using MouseButtonType = NextUIShared.Data.MouseButtonType;

namespace NextUIBrowser.OverlayWindow {
	public class OverlayWindow : IDisposable {
		protected readonly Overlay overlay;
		protected readonly TextureRenderHandler renderHandler;

		protected ChromiumWebBrowser? browser;

		protected IDisposable? sizeObservableSub;

		protected bool BrowserLoading => browser == null || !browser.IsBrowserInitialized || browser.IsLoading;

		public OverlayWindow(Overlay overlay, TextureRenderHandler renderHandler) {
			this.renderHandler = renderHandler;
			this.overlay = overlay;
		}

		public void Initialize() {
			browser = new ChromiumWebBrowser(overlay.Url, automaticallyCreateBrowser: false);
			browser.RenderHandler = renderHandler;
			browser.MenuHandler = new CustomMenuHandler();
			var size = renderHandler.GetViewRect();

			// General browser config
			var windowInfo = new WindowInfo() {
				Width = size.Width,
				Height = size.Height,
			};
			windowInfo.SetAsWindowless(IntPtr.Zero);

			// WindowInfo gets ignored sometimes, be super sure:
			browser.BrowserInitialized += (_, _) => { browser.Size = new Size(size.Width, size.Height); };

			BrowserSettings browserSettings = new() {
				WindowlessFrameRate = 60,
			};

			// Ready, boot up the browser
			browser.CreateBrowser(windowInfo, browserSettings);

			browserSettings.Dispose();
			windowInfo.Dispose();

			// Handle any changes done on overlay data
			overlay.DebugRequest += Debug;
			overlay.ReloadRequest += Reload;

			overlay.UrlChange += Navigate;

			overlay.MouseMoveEvent += HandleMouseMoveEvent;
			overlay.MouseClickEvent += HandleMouseClickEvent;
			overlay.MouseWheelEvent += HandleMouseWheelEvent;
			overlay.MouseLeaveEvent += HandleMouseLeaveEvent;

			overlay.KeyEvent += HandleKeyEvent;
			sizeObservableSub = overlay.SizeChange.AsObservable()
				.Throttle(TimeSpan.FromMilliseconds(300)).Subscribe(Resize);

			// Also request cursor if it changes
			renderHandler.CursorChanged += RenderHandlerOnCursorChanged;
		}

		protected void RenderHandlerOnCursorChanged(object? sender, Cursor cursor) {
			overlay.SetCursor(cursor);
		}

		public void Dispose() {
			if (browser == null) {
				return;
			}

			overlay.DebugRequest -= Debug;
			overlay.ReloadRequest -= Reload;
			renderHandler.CursorChanged -= RenderHandlerOnCursorChanged;

			overlay.UrlChange -= Navigate;
			overlay.MouseMoveEvent -= HandleMouseMoveEvent;
			overlay.MouseClickEvent -= HandleMouseClickEvent;
			overlay.MouseWheelEvent -= HandleMouseWheelEvent;
			overlay.MouseLeaveEvent -= HandleMouseLeaveEvent;
			overlay.KeyEvent -= HandleKeyEvent;
			sizeObservableSub?.Dispose();

			browser.RenderHandler = null;
			renderHandler.Dispose();
			browser.Dispose();
			browser = null;
			PluginLog.Log("Browser was disposed");
		}

		public void Navigate(object? sender, string newUrl) {
			// If navigating to the same url, force a clean reload
			if (browser?.Address == newUrl) {
				browser.Reload(true);
				return;
			}

			// Otherwise load regularly
			browser?.Load(newUrl);
		}

		protected void Reload() {
			browser.Reload(true);
		}

		protected void Debug() {
			browser.ShowDevTools();
		}

		public void Resize(Size size) {
			PluginLog.Log("CREATED WITH ZIE " + overlay.Size);
			overlay.Resizing = true;
			// Need to resize renderer first, the browser will check it (and hence the texture) when browser.
			// We are disregarding param as Size will adjust based on Fullscreen prop
			renderHandler.Resize(overlay.Size);
			if (browser != null) {
				browser.Size = overlay.Size;
			}
		}

		protected void HandleMouseMoveEvent(object? sender, MouseMoveEventRequest request) {
			if (BrowserLoading) {
				return;
			}

			var cursorX = (int)request.x;
			var cursorY = (int)request.y;

			renderHandler.SetMousePosition(cursorX, cursorY);

			var evt = new MouseEvent(cursorX, cursorY, DecodeInputModifier(request.modifier));

			// Ensure the mouse position is up to date
			browser.GetBrowserHost().SendMouseMoveEvent(evt, false);
		}

		protected void HandleMouseClickEvent(object? sender, MouseClickEventRequest request) {
			if (BrowserLoading) {
				return;
			}

			var cursorX = (int)request.x;
			var cursorY = (int)request.y;

			var evt = new MouseEvent(cursorX, cursorY, DecodeInputModifier(request.modifier));

			browser.GetBrowserHost().SendMouseClickEvent(
				evt,
				DecodeButtonType(request.mouseButtonType),
				request.isUp,
				request.clickCount
			);
		}

		protected void HandleMouseWheelEvent(object? sender, MouseWheelEventRequest request) {
			if (BrowserLoading) {
				return;
			}

			var cursorX = (int)request.x;
			var cursorY = (int)request.y;

			var evt = new MouseEvent(cursorX, cursorY, DecodeInputModifier(request.modifier));

			// CEF treats the wheel delta as mode 0, pixels. Bump up the numbers to match typical in-browser experience.
			const int deltaMult = 100;
			browser.GetBrowserHost().SendMouseWheelEvent(
				evt,
				(int)request.wheelX * deltaMult,
				(int)request.wheelY * deltaMult
			);
		}

		protected void HandleMouseLeaveEvent(object? sender, MouseLeaveEventRequest request) {
			if (BrowserLoading) {
				return;
			}

			var cursorX = (int)request.x;
			var cursorY = (int)request.y;

			var evt = new MouseEvent(cursorX, cursorY, CefEventFlags.None);

			browser.GetBrowserHost().SendMouseMoveEvent(evt, true);
		}

		public void HandleKeyEvent(object? sender, KeyEventRequest request) {
			if (browser == null || !browser.IsBrowserInitialized || browser.IsLoading) {
				return;
			}

			var type = request.keyEventType switch {
				NextUIShared.Data.KeyEventType.KeyDown => CefSharp.KeyEventType.RawKeyDown,
				NextUIShared.Data.KeyEventType.KeyUp => CefSharp.KeyEventType.KeyUp,
				NextUIShared.Data.KeyEventType.Character => CefSharp.KeyEventType.Char,
				_ => throw new ArgumentException($"Invalid KeyEventType {request.keyEventType}")
			};

			browser.GetBrowserHost().SendKeyEvent(new KeyEvent {
				Type = type,
				Modifiers = DecodeInputModifier(request.modifier),
				WindowsKeyCode = request.userKeyCode,
				NativeKeyCode = request.nativeKeyCode,
				IsSystemKey = request.systemKey,
			});
		}

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

		protected static CefSharp.MouseButtonType DecodeButtonType(MouseButtonType buttonType) {
			switch (buttonType) {
				case MouseButtonType.Middle: return CefSharp.MouseButtonType.Middle;
				case MouseButtonType.Right: return CefSharp.MouseButtonType.Right;
				default: return CefSharp.MouseButtonType.Left;
			}
		}
	}
}