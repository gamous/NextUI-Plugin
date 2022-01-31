using System;
using System.Drawing;
using NextUIPlugin.Model;

using Newtonsoft.Json;

namespace NextUIPlugin.Configuration {
	// This class needs to be created because shared project cannot reference anything otherwise it breaks
	[Serializable]
	public class OverlayConfig {
		public string Guid { get; set; } = null!;

		public string Name { get; set; } = null!;
		public string Url { get; set; } = null!;

		public string Size { get; set; } = null!;

		public string Position { get; set; } = null!;
		public bool FullScreen { get; set; }
		public bool ClickThrough { get; set; }
		public bool TypeThrough { get; set; }
		public bool Locked { get; set; }
		public bool Hidden { get; set; }
		public OverlayVisibility VisibilityShow { get; set; }
		public OverlayVisibility VisibilityHide { get; set; }

		public string FullScreenSize { get; set; } = null!;

		public static OverlayConfig FromOverlay(Overlay overlay) {
			var overlayConfig = new OverlayConfig();
			overlayConfig.Guid = overlay.Guid.ToString();
			overlayConfig.Name = overlay.Name;
			overlayConfig.Url = overlay.Url;
			overlayConfig.Size = SizeToString(overlay.Size);
			overlayConfig.Position = PointToString(overlay.Position);
			overlayConfig.FullScreen = overlay.FullScreen;
			overlayConfig.ClickThrough = overlay.ClickThrough;
			overlayConfig.TypeThrough = overlay.TypeThrough;
			overlayConfig.Locked = overlay.Locked;
			overlayConfig.Hidden = overlay.Hidden;
			overlayConfig.VisibilityShow = overlay.VisibilityShow;
			overlayConfig.VisibilityHide = overlay.VisibilityHide;
			overlayConfig.FullScreenSize = SizeToString(overlay.FullScreenSize);

			return overlayConfig;
		}

		public Overlay ToOverlay() {
			var parsedSize = ParseSize(Size);
			var overlay = new Overlay(Url, parsedSize);
			overlay.Guid = new Guid(Guid);
			overlay.Name = Name;
			overlay.Url = Url;
			overlay.Size = parsedSize;
			overlay.Position = ParsePoint(Position);
			overlay.FullScreen = FullScreen;
			overlay.ClickThrough = ClickThrough;
			overlay.TypeThrough = TypeThrough;
			overlay.Locked = Locked;
			overlay.Hidden = Hidden;
			overlay.VisibilityShow = VisibilityShow;
			overlay.VisibilityHide = VisibilityHide;
			overlay.FullScreenSize = ParseSize(FullScreenSize);

			return overlay;
		}

		protected static Point ParsePoint(string value) {
			var obj = value.Split('|');
			return new Point(int.Parse(obj[0]), int.Parse(obj[1]));
		}

		protected static Size ParseSize(string value) {
			var obj = value.Split('|');
			return new Size(int.Parse(obj[0]), int.Parse(obj[1]));
		}

		protected static string SizeToString(Size size) {
			return size.Width.ToString() + '|' + size.Height;
		}

		protected static string PointToString(Point size) {
			return size.X.ToString() + '|' + size.Y;
		}
	}
}