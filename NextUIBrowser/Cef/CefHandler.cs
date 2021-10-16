using CefSharp;
using CefSharp.OffScreen;

namespace NextUIBrowser.Cef {
	internal static class CefHandler {
		public static void Initialize(string cefCacheDir) {
			var settings = new CefSettings() {
				CachePath = cefCacheDir,
				CefCommandLineArgs = {
					["autoplay-policy"] = "no-user-gesture-required"
				},
#if !DEBUG
				LogSeverity = LogSeverity.Fatal,
#endif
			};

			settings.EnableAudio();
			settings.SetOffScreenRenderingBestPerformanceArgs();

			CefSharp.Cef.Initialize(settings, performDependencyCheck: false, browserProcessHandler: null);
		}

		public static void Shutdown() {
			CefSharp.Cef.Shutdown();
		}
	}
}