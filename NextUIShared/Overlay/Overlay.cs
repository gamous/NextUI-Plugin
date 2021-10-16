using System;
using System.Drawing;
using System.Numerics;
using NextUIShared.Data;
using NextUIShared.Request;

namespace NextUIShared.Overlay {
	/**
	 * Pure overlay data
	 */
	[Serializable]
	public class Overlay : IDisposable {
		public Guid Guid { get; set; }

		protected string name;
		protected string url;
		protected IntPtr texturePointer;
		protected Size size;
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

		public Action<IntPtr> TexturePointerChange;
		public Action<string> NameChange;
		public Action<string> UrlChange;
		public Action<Size> SizeChange;
		public Action<Cursor> CursorChange;
		public Action<MouseEventRequest> MouseEvent;
		public Action<KeyEventRequest> KeyEvent;

		public Action DebugRequest;
		public Action DisposeRequest;

		protected bool mouseInWindow;
		protected bool windowFocused;
		public bool acceptFocus;
		protected bool captureCursor;

		public Overlay(string url) {
			Guid = new Guid();
			Url = url;
		}

		public void Navigate(string newUrl) {
			Url = newUrl;
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