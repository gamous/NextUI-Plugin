﻿using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Threading;
using Dalamud.Interface.Internal.Notifications;
using Dalamud.Logging;
using McMaster.NETCore.Plugins;
using NextUIShared;

namespace NextUIPlugin.Service {
	public static class MicroPluginService {
		internal const string MicroPluginDirName = "MicroPlugin";
		internal const string RequiredVersion = "0.1.0.1";

		internal static string? pluginDir;
		internal static string? configDir;
		internal static PluginLoader? microPluginLoader;
		internal static INuPlugin? microPlugin;

		// Thread zone
		internal static Thread microPluginThread = null!;
		internal static readonly ManualResetEventSlim MicroPluginResetEvent = new();

		// PID zone
		internal static int lastPid;

		public static void Initialize() {
			pluginDir = NextUIPlugin.pluginInterface.AssemblyLocation.DirectoryName;
			configDir = NextUIPlugin.pluginInterface.GetPluginConfigDirectory();

			ReadLastPid();

			var microPluginThreadStart = new ThreadStart(LoadMicroPlugin);
			microPluginThread = new Thread(microPluginThreadStart);
			microPluginThread.Start();
		}

		public static void Shutdown() {
			MicroPluginResetEvent.Set();
			microPluginThread.Join(1000);
		}

		public static void LoadMicroPlugin() {
			// This shouldn't realistically ever occur
			if (pluginDir == null || configDir == null || NextUIPlugin.guiManager == null) {
				PluginLog.Error("Unable to load MicroPlugin, unexpected error");
				return;
			}

#if RELEASE
			var microPluginDir = Path.Combine(configDir, MicroPluginDirName);
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
					DownloadMicroPlugin(microPluginDir);
				}
				else {
					// We cannot do anything, bail
					NextUIPlugin.pluginInterface.UiBuilder.AddNotification(
						"Unable to update, plugin please restart the game",
						"NextUI Error",
						NotificationType.Error
					);
					PluginLog.Error("Unable to download micro plugin while not in cold boot");
					return;
				}
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
					catch (Exception e) {
						PluginLog.Log("Unable to delete old copy " + oldCopy);
					}
				}
			}

			var dllName = "NextUIBrowser.dll";
			var baseMicroPluginDir = Path.Combine(pluginDir, MicroPluginDirName);
			var microPluginDir = Path.Combine(pluginDir, $"{MicroPluginDirName}-{timestamp}");
			var cefDir = microPluginDir;
			var dllPath = Path.Combine(microPluginDir, dllName);

			Copy(baseMicroPluginDir, microPluginDir);
#endif

			microPluginLoader = PluginLoader.CreateFromAssemblyFile(
				assemblyFile: dllPath,
				sharedTypes: new[] { typeof(INuPlugin), typeof(IGuiManager) },
				isUnloadable: false,
				configure: config => {
					config.IsUnloadable = false;
					config.LoadInMemory = false;
				}
			);
			PluginLog.Log("Loaded MicroPlugin");

			var assembly = microPluginLoader.LoadDefaultAssembly();
			microPlugin = (INuPlugin?)assembly.CreateInstance("NextUIBrowser.BrowserPlugin");
			if (microPlugin == null) {
				PluginLog.Warning("Unable to load BrowserPlugin");
				return;
			}

			microPlugin.Initialize(microPluginDir, cefDir, NextUIPlugin.guiManager);
			PluginLog.Log("Successfully loaded BrowserPlugin");

			// Saving pid once micro plugin loads in case of any errors inside of it
			// If plugin doesn't load, we don't have to save pid because CEF was not loaded
			// But we can't wait till it gracefully exits since once CEF is loaded, it's over
			WriteLastPid();

			MicroPluginResetEvent.Wait();

			microPlugin.Shutdown();
			microPluginLoader.Dispose();

			microPlugin = null;
			microPluginLoader = null;

			GC.Collect(); // collects all unused memory
			GC.WaitForPendingFinalizers(); // wait until GC has finished its work
			GC.Collect();
		}

		#region Utils

		internal static void DownloadMicroPlugin(string microPluginDir) {
			if (configDir == null) {
				return;
			}

			var downloadPath = Path.Combine(configDir, "mp.zip");
			if (File.Exists(downloadPath)) {
				File.Delete(downloadPath);
			}

			// we assume tag is the same as version
#if RELEASE_TEST
			var artifactUrl = $"https://localhost:4200/latest.zip";
#else
			var artifactUrl = $"https://gitlab.com/kaminariss/nextui-plugin/-/jobs/artifacts/v{RequiredVersion}/raw" +
			                  "/NextUIBrowser/bin/latest.zip?job=build";
#endif

			var webClient = new WebClient();
			// webClient.Headers.Add("Accept: text/html, application/xhtml+xml, */*");
			// webClient.Headers.Add("User-Agent: Mozilla/5.0 (compatible; MSIE 9.0; Windows NT 6.1; WOW64; Trident/5.0)");
			webClient.DownloadFile(
				new Uri(artifactUrl),
				downloadPath
			);

			Directory.Delete(microPluginDir, true);
			ZipFile.ExtractToDirectory(downloadPath, microPluginDir);
		}

		internal static void ReadLastPid() {
			if (configDir == null) {
				return;
			}

			var pidFile = Path.Combine(configDir, "pid.txt");
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
			if (configDir == null) {
				return;
			}

			var pidFile = Path.Combine(configDir, "pid.txt");

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