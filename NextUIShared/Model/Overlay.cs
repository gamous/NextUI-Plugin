using System;
using System.Drawing;
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
		protected IntPtr texturePointer;
		protected Size size;
		protected Point position;
		protected Cursor cursor;

		public string Name {
			get { return name; }
			set {
				if (name == value) {
					return;
				}

				name = value;
				NameChange?.Invoke(name);
			}
		}

		public string Url {
			get { return url; }
			set {
				if (url == value) {
					return;
				}

				url = value;
				UrlChange?.Invoke(url);
			}
		}


		public IntPtr TexturePointer {
			get { return texturePointer; }
			set {
				if (texturePointer == value) {
					return;
				}

				texturePointer = value;
				TexturePointerChange?.Invoke(texturePointer);
			}
		}

		public Size Size {
			get { return size; }
			set {
				if (size == value) {
					return;
				}

				size = value;
				SizeChange?.Invoke(size);
			}
		}

		public Point Position {
			get { return position; }
			set {
				if (position == value) {
					return;
				}

				position = value;
				PositionChange?.Invoke(position);
			}
		}

		public Cursor Cursor {
			get { return cursor; }
			set {
				if (cursor == value) {
					return;
				}

				cursor = value;
				CursorChange?.Invoke(cursor);
			}
		}

		public bool ClickThrough { get; set; }
		public bool TypeThrough { get; set; }
		public bool Locked { get; set; } = true;
		public bool Hidden { get; set; }

		public event Action<IntPtr>? TexturePointerChange;
		public event Action<string>? NameChange;
		public event Action<string>? UrlChange;
		public event Action<Size>? SizeChange;
		public event Action<Point>? PositionChange;
		public event Action<Cursor>? CursorChange;
		public event Action<MouseEventRequest>? MouseEvent;
		public event Action<KeyEventRequest>? KeyEvent;

		public event Action? DebugRequest;
		public event Action? ReloadRequest;
		public event Action? DisposeRequest;

		public Overlay(string url, Size newSize) {
			Guid = new Guid();
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
			MouseEvent?.Invoke(request);
		}

		public void RequestKeyEvent(KeyEventRequest request) {
			KeyEvent?.Invoke(request);
		}
	}
}