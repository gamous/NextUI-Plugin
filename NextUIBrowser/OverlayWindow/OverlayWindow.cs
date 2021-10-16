﻿using CefSharp;
using CefSharp.OffScreen;
using System;
using System.Collections.Generic;
using System.Drawing;
using NextUIBrowser.RenderHandlers;
using NextUIShared.Data;
using NextUIShared.Model;
using NextUIShared.Request;

namespace NextUIBrowser.OverlayWindow {
	public class OverlayWindow : IDisposable {
		protected readonly Overlay overlay;
		protected readonly BaseRenderHandler renderHandler;

		protected ChromiumWebBrowser? browser;

		public OverlayWindow(Overlay overlay, BaseRenderHandler renderHandler) {
			this.renderHandler = renderHandler;
			this.overlay = overlay;
		}

		public void Initialize() {
			browser = new ChromiumWebBrowser(overlay.Url, automaticallyCreateBrowser: false);
			browser.RenderHandler = renderHandler;
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
			overlay.SizeChange += Resize;
			overlay.MouseEvent += HandleMouseEvent;
			overlay.KeyEvent += HandleKeyEvent;
		}

		public void Dispose() {
			if (browser == null) {
				return;
			}

			overlay.DebugRequest -= Debug;
			overlay.ReloadRequest -= Reload;
			overlay.UrlChange -= Navigate;
			overlay.SizeChange -= Resize;
			overlay.MouseEvent -= HandleMouseEvent;
			overlay.KeyEvent -= HandleKeyEvent;

			browser.RenderHandler = null;
			renderHandler.Dispose();
			browser.Dispose();
		}

		public void Navigate(string newUrl) {
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
			// Need to resize renderer first, the browser will check it (and hence the texture) when browser.Size is set.
			renderHandler.Resize(size);
			if (browser != null) {
				browser.Size = size;
			}
		}

		protected void HandleMouseEvent(MouseEventRequest request) {
			// If the browser isn't ready yet, noop
			if (browser == null || !browser.IsBrowserInitialized || browser.IsLoading) {
				return;
			}

			var cursorX = (int)request.x;
			var cursorY = (int)request.y;

			// Update the renderer's concept of the mouse cursor
			renderHandler.SetMousePosition(cursorX, cursorY);

			MouseEvent evt = new(cursorX, cursorY, DecodeInputModifier(request.modifier));

			var host = browser.GetBrowserHost();

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
			var deltaMult = 100;
			host.SendMouseWheelEvent(evt, (int)request.wheelX * deltaMult, (int)request.wheelY * deltaMult);
		}

		public void HandleKeyEvent(KeyEventRequest request) {
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

			return result;
		}
	}
}