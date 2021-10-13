using CefSharp;
using CefSharp.OffScreen;
using System;
using System.Collections.Generic;
using System.Drawing;
using RendererProcess.Data;
using RendererProcess.Ipc;
using RendererProcess.RenderHandlers;

namespace RendererProcess {
	public class Overlay : IDisposable {
		protected string url;

		protected ChromiumWebBrowser? browser;
		public BaseRenderHandler RenderHandler;

		public Overlay(string url, BaseRenderHandler renderHandler) {
			this.url = url;
			RenderHandler = renderHandler;
		}

		public void Initialize() {
			browser = new ChromiumWebBrowser(url, automaticallyCreateBrowser: false);
			browser.RenderHandler = RenderHandler;
			CefSharp.Structs.Rect size = RenderHandler.GetViewRect();

			// General browser config
			WindowInfo windowInfo = new() {
				Width = size.Width,
				Height = size.Height,
			};
			windowInfo.SetAsWindowless(IntPtr.Zero);

			// WindowInfo gets ignored sometimes, be super sure:
			browser.BrowserInitialized += (sender, args) => { browser.Size = new Size(size.Width, size.Height); };

			BrowserSettings browserSettings = new() {
				WindowlessFrameRate = 60,
			};

			// Ready, boot up the browser
			browser.CreateBrowser(windowInfo, browserSettings);

			browserSettings.Dispose();
			windowInfo.Dispose();
		}

		public void Dispose() {
			if (browser == null) {
				return;
			}

			browser.RenderHandler = null;
			RenderHandler.Dispose();
			browser.Dispose();
		}

		public void Navigate(string newUrl) {
			// If navigating to the same url, force a clean reload
			if (browser?.Address == newUrl) {
				browser.Reload(true);
				return;
			}

			// Otherwise load regularly
			url = newUrl;
			browser?.Load(newUrl);
		}

		public void Debug() {
			browser.ShowDevTools();
		}

		public void HandleMouseEvent(MouseEventRequest request) {
			// If the browser isn't ready yet, noop
			if (browser == null || !browser.IsBrowserInitialized || browser.IsLoading) {
				return;
			}

			int cursorX = (int)request.x;
			int cursorY = (int)request.y;

			// Update the renderer's concept of the mouse cursor
			RenderHandler.SetMousePosition(cursorX, cursorY);

			MouseEvent evt = new(cursorX, cursorY, DecodeInputModifier(request.modifier));

			IBrowserHost? host = browser.GetBrowserHost();

			// Ensure the mouse position is up to date
			host.SendMouseMoveEvent(evt, request.leaving);

			// Fire any relevant click events
			List<MouseButtonType> doubleClicks = DecodeMouseButtons(request.mouseDouble);
			DecodeMouseButtons(request.mouseDown)
				.ForEach(button =>
					host.SendMouseClickEvent(evt, button, false, doubleClicks.Contains(button) ? 2 : 1)
				);
			DecodeMouseButtons(request.mouseUp).ForEach(button => host.SendMouseClickEvent(evt, button, true, 1));

			// CEF treats the wheel delta as mode 0, pixels. Bump up the numbers to match typical in-browser experience.
			int deltaMult = 100;
			host.SendMouseWheelEvent(evt, (int)request.wheelX * deltaMult, (int)request.wheelY * deltaMult);
		}

		public void HandleKeyEvent(KeyEventRequest request) {
			if (browser == null || !browser.IsBrowserInitialized || browser.IsLoading) {
				return;
			}

			CefSharp.KeyEventType type = request.keyEventType switch {
				Data.KeyEventType.KeyDown => CefSharp.KeyEventType.RawKeyDown,
				Data.KeyEventType.KeyUp => CefSharp.KeyEventType.KeyUp,
				Data.KeyEventType.Character => CefSharp.KeyEventType.Char,
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

		public void Resize(Size size) {
			// Need to resize renderer first, the browser will check it (and hence the texture) when browser.Size is set.
			RenderHandler.Resize(size);
			if (browser != null) {
				browser.Size = size;
			}
		}

		protected List<MouseButtonType> DecodeMouseButtons(MouseButton buttons) {
			List<MouseButtonType> result = new();
			if ((buttons & MouseButton.Primary) == MouseButton.Primary) {
				result.Add(MouseButtonType.Left);
			}

			if ((buttons & MouseButton.Secondary) == MouseButton.Secondary) {
				result.Add(MouseButtonType.Right);
			}

			if ((buttons & MouseButton.Tertiary) == MouseButton.Tertiary) {
				result.Add(MouseButtonType.Middle);
			}

			return result;
		}

		protected CefEventFlags DecodeInputModifier(InputModifier modifier) {
			CefEventFlags result = CefEventFlags.None;
			if ((modifier & InputModifier.Shift) == InputModifier.Shift) {
				result |= CefEventFlags.ShiftDown;
			}

			if ((modifier & InputModifier.Control) == InputModifier.Control) {
				result |= CefEventFlags.ControlDown;
			}

			if ((modifier & InputModifier.Alt) == InputModifier.Alt) {
				result |= CefEventFlags.AltDown;
			}

			return result;
		}
	}
}