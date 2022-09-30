//#define RELEASE_TEST

using System;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Dalamud.Logging;
using ImGuiNET;
using NextUIPlugin.Cef;

#if RELEASE
using System.Diagnostics;
#endif

namespace NextUIPlugin.Service {
	public static class MicroPluginService {
		internal const string MicroPluginDirName = "MicroPlugin";
		// Manually updated, not every new version would require new microplugin
		internal const string RequiredVersion = "0.5.2.0";

		internal static string? pluginDir;
		internal static string baseDir;
		internal static string pidFile;
		internal static string cacheDir;

		static float downloadProgress = -1;
		static bool showWindowWarning;
		static string warningMessage = "";

		// Thread zone
		internal static Thread microPluginThread = null!;
		internal static readonly ManualResetEventSlim microPluginResetEvent = new();

		// PID zone
		internal static int lastPid;

		public static void Initialize() {
			pluginDir = NextUIPlugin.pluginInterface.AssemblyLocation.DirectoryName ?? "";
			if (String.IsNullOrEmpty(pluginDir)) {
				throw new Exception("Could not determine plugin directory");
			}
			baseDir = Path.Combine(NextUIPlugin.pluginInterface.GetPluginConfigDirectory(), "NUCefSharp");
			pidFile = Path.Combine(baseDir, "pid.txt");
			cacheDir = Path.Combine(baseDir, "Cache");
			ReadLastPid();

			var microPluginThreadStart = new ThreadStart(LoadMicroPlugin);
			microPluginThread = new Thread(microPluginThreadStart);
			microPluginThread.Start();
		}

		public static void Shutdown() {
			microPluginResetEvent.Set();
			microPluginThread.Join(100);
		}

#if RELEASE
		public static async void LoadMicroPlugin() {
#else
		public static void LoadMicroPlugin() {
#endif
			// This shouldn't realistically ever occur
			if (pluginDir == null) {
				PluginLog.Error("Unable to load MicroPlugin, unexpected error");
				return;
			}

			if (!Directory.Exists(baseDir)) {
				Directory.CreateDirectory(baseDir);
			}

#if RELEASE
			var microPluginDir = Path.Combine(baseDir, MicroPluginDirName);
			var dllName = "NextUIBrowser.dll";
			var dllPath = Path.Combine(microPluginDir, dllName);
			var cefDir = microPluginDir;

			var downloadNeeded = false;
			if (!Directory.Exists(microPluginDir)) {
				downloadNeeded = true;
			}
			else if (!File.Exists(dllPath)) {
				downloadNeeded = true;
			}
			else {
				// Get the file version.
				var microPluginVersion = FileVersionInfo.GetVersionInfo(dllPath);
				PluginLog.Log("MicroPlugin version " + microPluginVersion.FileVersion);
				if (microPluginVersion.FileVersion != RequiredVersion) {
					downloadNeeded = true;
				}
			}

			if (downloadNeeded) {
				if (IsColdBoot()) {
					await DownloadMicroPlugin(microPluginDir);
				}
				else {
					// We cannot do anything, bail
					warningMessage = "Unable to update, plugin please restart the game";
					showWindowWarning = true;
					PluginLog.Error(warningMessage);
					return;
				}
			}
			
			if (!IsColdBoot()) {
				warningMessage = "This plugin is not possible to reload, please restart the game";
				showWindowWarning = true;
				PluginLog.Error(warningMessage);
				return;
			}
#else
			var timestamp = DateTime.Now.ToFileTime();

			// We are only able to clear up trash on cold boot
			if (IsColdBoot()) {
				var oldCopies = Directory.EnumerateDirectories(pluginDir, $"{MicroPluginDirName}-*");
				foreach (var oldCopy in oldCopies) {
					try {
						Directory.Delete(oldCopy, true);
					}
					catch (Exception) {
						PluginLog.Log("Unable to delete old copy " + oldCopy);
					}
				}
			}

			var baseMicroPluginDir = Path.Combine(baseDir, MicroPluginDirName);
			var microPluginDir = Path.Combine(pluginDir, $"{MicroPluginDirName}-{timestamp}");
			var cefDir = microPluginDir;

			Copy(baseMicroPluginDir, microPluginDir);
#endif
			PluginLog.Log("Loaded MicroPlugin");

			InitializeBrowser(cefDir);

			// Saving pid once micro plugin loads in case of any errors inside of it
			// If plugin doesn't load, we don't have to save pid because CEF was not loaded
			// But we can't wait till it gracefully exits since once CEF is loaded, it's over
			WriteLastPid();

			microPluginResetEvent.Wait();

			ShutdownBrowser();

			GC.Collect(); // collects all unused memory
			GC.WaitForPendingFinalizers(); // wait until GC has finished its work
			GC.Collect();
		}
		
		public static void InitializeBrowser(string cefDir) {
			PluginLog.Log("Initializing Browser");

			CefHandler.Initialize(cacheDir, cefDir, baseDir);

			// Notify gui manager that micro plugin is ready to go
			NextUIPlugin.guiManager.MicroPluginLoaded();
		}

		public static void ShutdownBrowser() {
			CefHandler.Shutdown();
			PluginLog.Log("Cef was shut down");
		}

		#region Utils

		internal static async Task DownloadMicroPlugin(string microPluginDir) {
			try {
				var downloadPath = Path.Combine(baseDir, "mp.zip");
				if (File.Exists(downloadPath)) {
					File.Delete(downloadPath);
				}

				// we assume tag is the same as version
#if RELEASE_TEST
				var artifactUrl = @"A:\Projects\Kaminaris\ffxiv\NextUIPlug\NextUIPlugin\NextUIBrowser\bin\latest.zip";
#else
			var artifactUrl = $"https://gitlab.com/kaminariss/nextui-plugin/-/jobs/artifacts/v{RequiredVersion}/raw" +
			                  "/NextUIBrowser/bin/latest.zip?job=build";
#endif

				using var webClient = new WebClient();
				webClient.DownloadProgressChanged += (_, e) => {
					downloadProgress = e.ProgressPercentage;
					PluginLog.Log("MicroPlugin progress " + e.ProgressPercentage);
				};
				webClient.DownloadFileCompleted += (_, _) => {
					downloadProgress = 100;
				};

				await webClient.DownloadFileTaskAsync(
					new Uri(artifactUrl),
					downloadPath
				);

				PluginLog.Log("Downloaded latest MicroPlugin");

				if (Directory.Exists(microPluginDir)) {
					Directory.Delete(microPluginDir, true);
				}

				Directory.CreateDirectory(microPluginDir);

				ZipFile.ExtractToDirectory(downloadPath, microPluginDir);
				PluginLog.Log("Extracted MicroPlugin");

				if (File.Exists(downloadPath)) {
					File.Delete(downloadPath);
				}
			}
			catch (Exception) {
				PluginLog.Warning("Unable to download MicroPlugin");
			}
		}

		public static void DrawProgress() {
			if (downloadProgress is >= 100 or < 0) {
				return;
			}

			ImGui.SetNextWindowSize(new Vector2(300, 70));
			ImGui.Begin(
				"Downloading MicroPlugin",
				ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoScrollbar
			);

			ImGui.ProgressBar(downloadProgress / 100f, new Vector2(280, 30));
			ImGui.End();
		}

		public static void DrawWarningWindow() {
			if (!showWindowWarning) {
				return;
			}

			ImGui.SetNextWindowSize(new Vector2(500, 80));
			ImGui.Begin(
				"NextUI",
				ref showWindowWarning,
				ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoScrollbar
			);

			ImGui.TextColored(new Vector4(1f, 0f, 0f, 1f), warningMessage);
			ImGui.End();
		}

		internal static void ReadLastPid() {
			if (!File.Exists(pidFile)) {
				return;
			}

			var pidFileContent = File.ReadAllText(pidFile);
			try {
				lastPid = int.Parse(pidFileContent);
			}
			catch (Exception) {
				lastPid = 0;
			}
		}

		internal static void WriteLastPid() {
			if (!Directory.Exists(baseDir)) {
				Directory.CreateDirectory(baseDir);
			}

			File.WriteAllText(pidFile, Environment.ProcessId.ToString());
		}

		internal static bool IsColdBoot() {
			return lastPid != Environment.ProcessId;
		}

#if DEBUG
		public static void Copy(string sourceDirectory, string targetDirectory) {
			var diSource = new DirectoryInfo(sourceDirectory);
			var diTarget = new DirectoryInfo(targetDirectory);

			CopyAll(diSource, diTarget);
		}

		public static void CopyAll(DirectoryInfo source, DirectoryInfo target) {
			Directory.CreateDirectory(target.FullName);

			// Copy each file into the new directory.
			foreach (FileInfo fi in source.GetFiles()) {
				fi.CopyTo(Path.Combine(target.FullName, fi.Name), true);
			}

			// Copy each subdirectory using recursion.
			foreach (var diSourceSubDir in source.GetDirectories()) {
				var nextTargetSubDir = target.CreateSubdirectory(diSourceSubDir.Name);
				CopyAll(diSourceSubDir, nextTargetSubDir);
			}
		}
#endif

		#endregion
	}
}