using System;
using System.Drawing;
using System.Reactive.Subjects;
using NextUIShared.Data;
using NextUIShared.Request;

namespace NextUIShared.Model {
	/**
	 * Pure overlay data
	 */
	[Serializable]
	public class Overlay : IDisposable {
		public Guid Guid { get; set; }

		protected string name = "New Overlay";
		protected string url = "";
		protected object? texture; // It is D3D11.Texture2D but Shared cannot have references to that
		protected Size size;
		protected bool fullscreen;
		protected Point position;
		protected Cursor cursor;

		public string Name {
			get { return name; }
			set {
				if (name == value) {
					return;
				}

				name = value;
				NameChange.OnNext(value);
			}
		}

		public string Url {
			get { return url; }
			set {
				if (url == value) {
					return;
				}

				url = value;
				UrlChange.OnNext(url);
			}
		}

		public object? Texture {
			get { return texture; }
			set {
				if (texture == value) {
					return;
				}

				texture = value;
				TextureChange.OnNext(value);
			}
		}

		public Size Size {
			get { return FullScreen ? FullScreenSize : size; }
			set {
				if (size.Equals(value)) {
					return;
				}

				var s = new Size(value.Width, value.Height);
				if (s.Width < 1) {
					s.Width = 1;
				}
				if (s.Height < 1) {
					s.Height = 1;
				}

				size = s;
				SizeChange.OnNext(s);
			}
		}

		public Point Position {
			get { return FullScreen ? Point.Empty : position; }
			set {
				if (position == value) {
					return;
				}

				position = value;
				PositionChange.OnNext(position);
			}
		}

		public Cursor Cursor {
			get { return cursor; }
			set {
				if (cursor == value) {
					return;
				}

				cursor = value;
				CursorChange.OnNext(cursor);
			}
		}

		public bool FullScreen {
			get { return fullscreen; }
			set {
				if (fullscreen == value) {
					return;
				}

				fullscreen = value;
				SizeChange.OnNext(FullScreenSize);
			}
		}

		public bool ClickThrough { get; set; }
		public bool TypeThrough { get; set; }
		public bool Locked { get; set; } = true;
		public bool Hidden { get; set; }
		public bool Toggled { get; set; }
		public Size FullScreenSize { get; set; }
		public OverlayVisibility Visibility { get; set; }

		// ReSharper disable InconsistentNaming
		public Subject<object?> TextureChange = new();
		public Subject<string> NameChange = new();
		public Subject<string> UrlChange = new();
		public Subject<Size> SizeChange = new();
		public Subject<Point> PositionChange = new();
		public Subject<Cursor> CursorChange = new();
		public Subject<MouseEventRequest> MouseEvent = new();
		public Subject<KeyEventRequest> KeyEvent = new();
		// ReSharper restore InconsistentNaming

		public event Action? DebugRequest;
		public event Action? ReloadRequest;
		public event Action? DisposeRequest;

		public Overlay(string url, Size newSize) {
			Guid = Guid.NewGuid();
			Url = url;
			size = newSize;
			if (size.Width != 0 && size.Height != 0) {
				return;
			}

			size.Width = 800;
			size.Height = 600;
		}

		public void Navigate(string newUrl) {
			Url = newUrl;
		}

		public void Reload() {
			ReloadRequest?.Invoke();
		}

		public void Debug() {
			DebugRequest?.Invoke();
		}

		public void Dispose() {
			DisposeRequest?.Invoke();
		}

		public void SetCursor(Cursor newCursor) {
			Cursor = newCursor;
		}

		public void RequestMouseEvent(MouseEventRequest request) {
			MouseEvent.OnNext(request);
		}

		public void RequestKeyEvent(KeyEventRequest request) {
			KeyEvent.OnNext(request);
		}
	}
}