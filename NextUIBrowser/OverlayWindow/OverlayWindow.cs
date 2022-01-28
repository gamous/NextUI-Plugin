using System;
using System.Drawing;
using System.Reactive.Linq;
using Dalamud.Logging;
using NextUIBrowser.Cef;
using NextUIBrowser.Cef.App;
using NextUIShared.Data;
using NextUIShared.Model;
using NextUIShared.Request;
using Xilium.CefGlue;
using CefEventFlags = Xilium.CefGlue.CefEventFlags;
using MouseButtonType = NextUIShared.Data.MouseButtonType;

namespace NextUIBrowser.OverlayWindow {
	public class OverlayWindow : IDisposable {
		protected readonly Overlay overlay;
		protected NUCefClient client;

		protected CefBrowser? browser;

		protected IDisposable? sizeObservableSub;

		protected bool BrowserLoading => browser == null || browser.IsLoading;

		public OverlayWindow(Overlay overlay) {
			this.overlay = overlay;
			client = new NUCefClient(overlay);
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
			client.displayHandler.CursorChanged += RenderHandlerOnCursorChanged;

			client.lifeSpanHandler.AfterBrowserLoad -= LifeSpanHandlerOnAfterBrowserLoad;
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
			client.displayHandler.CursorChanged -= RenderHandlerOnCursorChanged;

			overlay.UrlChange -= Navigate;
			overlay.MouseMoveEvent -= HandleMouseMoveEvent;
			overlay.MouseClickEvent -= HandleMouseClickEvent;
			overlay.MouseWheelEvent -= HandleMouseWheelEvent;
			overlay.MouseLeaveEvent -= HandleMouseLeaveEvent;
			overlay.KeyEvent -= HandleKeyEvent;
			sizeObservableSub?.Dispose();

			//browser.RenderHandler = null;
			// renderHandler.Dispose();
			client.Dispose();
			browser.Dispose();
			browser = null;
			PluginLog.Log("Browser was disposed");
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

		protected void Reload() {
			browser?.ReloadIgnoreCache();
		}

		protected void Debug() {
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

		protected void HandleMouseMoveEvent(object? sender, MouseMoveEventRequest request) {
			if (BrowserLoading) {
				return;
			}

			var cursor = DpiScaling.ScaleViewPoint(request.x, request.y);
			client.displayHandler.SetMousePosition(cursor.X, cursor.Y);
			var evt = new CefMouseEvent(cursor.X, cursor.Y, DecodeInputModifier(request.modifier));

			// Ensure the mouse position is up to date
			browser?.GetHost().SendMouseMoveEvent(evt, false);
		}

		protected void HandleMouseClickEvent(object? sender, MouseClickEventRequest request) {
			if (BrowserLoading) {
				return;
			}

			var cursor = DpiScaling.ScaleViewPoint(request.x, request.y);
			var evt = new CefMouseEvent(cursor.X, cursor.Y, DecodeInputModifier(request.modifier));

			browser?.GetHost().SendMouseClickEvent(
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

			var cursor = DpiScaling.ScaleViewPoint(request.x, request.y);
			var evt = new CefMouseEvent(cursor.X, cursor.Y, DecodeInputModifier(request.modifier));

			// CEF treats the wheel delta as mode 0, pixels. Bump up the numbers to match typical in-browser experience.
			const int deltaMult = 100;
			browser?.GetHost().SendMouseWheelEvent(
				evt,
				(int)request.wheelX * deltaMult,
				(int)request.wheelY * deltaMult
			);
		}

		protected void HandleMouseLeaveEvent(object? sender, MouseLeaveEventRequest request) {
			if (BrowserLoading) {
				return;
			}

			var cursor = DpiScaling.ScaleViewPoint(request.x, request.y);
			var evt = new CefMouseEvent(cursor.X, cursor.Y, CefEventFlags.None);

			browser?.GetHost().SendMouseMoveEvent(evt, true);
		}

		public void HandleKeyEvent(object? sender, KeyEventRequest request) {
			if (BrowserLoading) {
				return;
			}

			var type = request.keyEventType switch {
				KeyEventType.KeyDown => CefKeyEventType.RawKeyDown,
				KeyEventType.KeyUp => CefKeyEventType.KeyUp,
				KeyEventType.Character => CefKeyEventType.Char,
				_ => throw new ArgumentException($"Invalid KeyEventType {request.keyEventType}")
			};

			browser?.GetHost().SendKeyEvent(new CefKeyEvent {
				EventType = type,
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

		protected static CefMouseButtonType DecodeButtonType(MouseButtonType buttonType) {
			switch (buttonType) {
				case MouseButtonType.Middle: return CefMouseButtonType.Middle;
				case MouseButtonType.Right: return CefMouseButtonType.Right;
				default: return CefMouseButtonType.Left;
			}
		}
	}

	public class DevToolsWebClient : CefClient {
	}
}