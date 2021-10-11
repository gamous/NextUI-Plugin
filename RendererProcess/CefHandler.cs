using CefSharp;
using CefSharp.OffScreen;

namespace RendererProcess {
	internal static class CefHandler {
		public static void Initialize(string cefCacheDir) {
			CefSettings settings = new() {
				CachePath = cefCacheDir,
#if !DEBUG
				LogSeverity = LogSeverity.Fatal,
#endif
			};

			settings.CefCommandLineArgs["autoplay-policy"] = "no-user-gesture-required";
			settings.EnableAudio();
			settings.SetOffScreenRenderingBestPerformanceArgs();

			Cef.Initialize(settings, performDependencyCheck: false, browserProcessHandler: null);
		}

		public static void Shutdown() {
			Cef.Shutdown();
		}
	}
}