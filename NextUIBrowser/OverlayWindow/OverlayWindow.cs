using System;
using System.Collections.Generic;
using System.Drawing;
using System.Reactive.Linq;
using Dalamud.Logging;
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

			var browserSettings = new CefBrowserSettings {
				WindowlessFrameRate = 60,
			};

			client.AfterBrowserLoad += cefBrowser => {
				this.browser = cefBrowser;
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
				client.renderHandler.CursorChanged += RenderHandlerOnCursorChanged;
			};

			CefBrowserHost.CreateBrowser(
				windowInfo,
				client,
				browserSettings,
				overlay.Url
			);

			// browser = CefBrowserHost.CreateBrowserSync(
			// 	windowInfo,
			// 	client,
			// 	browserSettings,
			// 	overlay.Url
			// );
			
			// PluginLog.Log(
			// 	$"BR CREATED {browser.IsLoading} {browser.IsValid} " +
			// 	$"{browser.FrameCount} {browser.HasDocument} {browser?.GetMainFrame().Url}"
			// );
			

			
			//browser = new ChromiumWebBrowser(overlay.Url, automaticallyCreateBrowser: false);
			//browser.RenderHandler = renderHandler;
			//browser.MenuHandler = new CustomMenuHandler();
			//var size = renderHandler.GetViewRect();

			// General browser config
			// var windowInfo = new WindowInfo() {
			// 	Width = size.Width,
			// 	Height = size.Height,
			// };
			//windowInfo.SetAsWindowless(IntPtr.Zero);

			// WindowInfo gets ignored sometimes, be super sure:
			//browser.BrowserInitialized += (_, _) => { browser.Size = new Size(size.Width, size.Height); };

			// BrowserSettings browserSettings = new() {
			// 	WindowlessFrameRate = 60,
			// };

			// Ready, boot up the browser
			//browser.CreateBrowser(windowInfo, browserSettings);

			//browserSettings.Dispose();
			//windowInfo.Dispose();

			// Handle any changes done on overlay data
			
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
			client.renderHandler.CursorChanged -= RenderHandlerOnCursorChanged;

			overlay.UrlChange -= Navigate;
			overlay.MouseMoveEvent -= HandleMouseMoveEvent;
			overlay.MouseClickEvent -= HandleMouseClickEvent;
			overlay.MouseWheelEvent -= HandleMouseWheelEvent;
			overlay.MouseLeaveEvent -= HandleMouseLeaveEvent;
			overlay.KeyEvent -= HandleKeyEvent;
			sizeObservableSub?.Dispose();

			//browser.RenderHandler = null;
			// renderHandler.Dispose();
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
			// browser?.ReloadIgnoreCache();
			PluginLog.Log(
				$"BR CREATED 1-{browser.IsLoading} 2-{browser.IsValid} " +
				$"3-{browser.FrameCount} 4-{browser.HasDocument} 5-{browser?.GetMainFrame().Url} " +
				$"6-{browser.GetMainFrame().IsValid}"
			);
		}

		protected void Debug() {
			ShowDevTools();
		}

		protected void ShowDevTools() {
			PluginLog.Log("Dev tools");
			if (browser == null) {
				return;
			}
			PluginLog.Log("Dev tools browser exists");
			var host = browser.GetHost();
			var wi = CefWindowInfo.Create();
			wi.SetAsPopup(IntPtr.Zero, "DevTools");

			host.ShowDevTools(wi, new DevToolsWebClient(), new CefBrowserSettings(), new CefPoint(0, 0));
		}

		public void Resize(Size size) {
			PluginLog.Log("CREATED WITH ZIE " + overlay.Size);
			overlay.Resizing = true;
			// Need to resize renderer first, the browser will check it (and hence the texture) when browser.
			// We are disregarding param as Size will adjust based on Fullscreen prop
			client.renderHandler.Resize(overlay.Size);
			// if (browser != null) {
				//browser.Size = overlay.Size;
			// }
		}

		protected void HandleMouseMoveEvent(object? sender, MouseMoveEventRequest request) {
			if (BrowserLoading) {
				return;
			}

			var cursorX = (int)request.x;
			var cursorY = (int)request.y;
			
			client.renderHandler.SetMousePosition(cursorX, cursorY);

			var evt = new CefMouseEvent(cursorX, cursorY, DecodeInputModifier(request.modifier));

			// Ensure the mouse position is up to date
			browser?.GetHost().SendMouseMoveEvent(evt, false);
		}

		protected void HandleMouseClickEvent(object? sender, MouseClickEventRequest request) {
			if (BrowserLoading) {
				return;
			}

			var cursorX = (int)request.x;
			var cursorY = (int)request.y;

			var evt = new CefMouseEvent(cursorX, cursorY, DecodeInputModifier(request.modifier));

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

			var cursorX = (int)request.x;
			var cursorY = (int)request.y;

			var evt = new CefMouseEvent(cursorX, cursorY, DecodeInputModifier(request.modifier));

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

			var cursorX = (int)request.x;
			var cursorY = (int)request.y;

			var evt = new CefMouseEvent(cursorX, cursorY, CefEventFlags.None);

			browser?.GetHost().SendMouseMoveEvent(evt, true);
		}

		public void HandleKeyEvent(object? sender, KeyEventRequest request) {
			if (browser == null || browser.IsLoading) {
				return;
			}

			var type = request.keyEventType switch {
				NextUIShared.Data.KeyEventType.KeyDown => CefKeyEventType.RawKeyDown,
				NextUIShared.Data.KeyEventType.KeyUp => CefKeyEventType.KeyUp,
				NextUIShared.Data.KeyEventType.Character => CefKeyEventType.Char,
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