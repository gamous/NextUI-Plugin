using System;
using System.IO;
using System.Threading;
using Dalamud.Logging;
using NextUIShared;

namespace NextUIPlugin.Service {
	public static class MicroPluginService {
		internal static Thread microPluginThread = null!;
		internal static readonly ManualResetEventSlim MicroPluginResetEvent = new();
		internal static string? pluginDir;
		internal static McMaster.NETCore.Plugins.PluginLoader? microPluginLoader;
		internal static INuPlugin? microPlugin;

		public static void Initialize(string baseDir) {
			pluginDir = baseDir;

			var microPluginThreadStart = new ThreadStart(LoadMicroPlugin);
			microPluginThread = new Thread(microPluginThreadStart);
			microPluginThread.Start();
		}

		public static void Shutdown() {
			MicroPluginResetEvent.Set();
			microPluginThread.Join(1000);
		}

		public static void Copy(string sourceDirectory, string targetDirectory) {
			DirectoryInfo diSource = new DirectoryInfo(sourceDirectory);
			DirectoryInfo diTarget = new DirectoryInfo(targetDirectory);

			CopyAll(diSource, diTarget);
		}

		public static void CopyAll(DirectoryInfo source, DirectoryInfo target) {
			Directory.CreateDirectory(target.FullName);

			// Copy each file into the new directory.
			foreach (FileInfo fi in source.GetFiles()) {
				Console.WriteLine(@"Copying {0}\{1}", target.FullName, fi.Name);
				fi.CopyTo(Path.Combine(target.FullName, fi.Name), true);
			}

			// Copy each subdirectory using recursion.
			foreach (DirectoryInfo diSourceSubDir in source.GetDirectories()) {
				DirectoryInfo nextTargetSubDir =
					target.CreateSubdirectory(diSourceSubDir.Name);
				CopyAll(diSourceSubDir, nextTargetSubDir);
			}
		}

		//
		// [MethodImpl(MethodImplOptions.NoInlining)]
		public static void LoadMicroPlugin() {
			if (pluginDir == null) {
				PluginLog.Warning("Unable to load MicroPlugin, plugin directory was not passed");
				return;
			}

			var timestamp = DateTime.Now.ToFileTime();


			/*
			 * Either this
			 */
			// Clean up trash
			var oldCopies = Directory.EnumerateDirectories(pluginDir, "microplugin-*");
			foreach (var oldCopy in oldCopies) {
				PluginLog.Log("OLD COPY FOUND "  + oldCopy);
				// try {
				// 	Directory.Delete(oldCopy, true);
				// }
				// catch (Exception e) {
				// 	PluginLog.Log("Unable to delete old copy "  + oldCopy);
				// }
			}
			
			var dllName = $"NextUIBrowser.dll";
			var baseMicroPluginDir = Path.Combine(pluginDir, "microplugin");
			var microPluginDir = Path.Combine(pluginDir, $"microplugin-{timestamp}");
			var cefDir = microPluginDir;
			
			Copy(baseMicroPluginDir, microPluginDir);

			/*
			 * Or this
			 */
			// var dllName = $"NextUIBrowser-{timestamp}.dll";
			//
			// var microPluginDir = Path.Combine(pluginDir, $"microplugin");
			// File.Copy(
			// 	Path.Combine(microPluginDir, "NextUIBrowser.dll"),
			// 	Path.Combine(microPluginDir, dllName)
			// );

			/*
			 * Or this
			 */
			// var shadowCopyFiles = new[] {
			// 	"CefSharp.dll",
			// 	"CefSharp.Core.dll",
			// 	"CefSharp.Core.Runtime.dll",
			// 	"CefSharp.OffScreen.dll",
			// 	"Ijwhost.dll",
			// 	"NextUIBrowser.dll"
			// };
			// var baseMicroPluginDir = Path.Combine(pluginDir, "microplugin");
			// var microPluginDir = Path.Combine(pluginDir, $"microplugin-{timestamp}");
			// var dllName = $"NextUIBrowser.dll";
			// var cefDir = baseMicroPluginDir;
			// Directory.CreateDirectory(microPluginDir);
			// foreach (var fileToCopy in shadowCopyFiles) {
			// 	File.Copy(
			// 		Path.Combine(baseMicroPluginDir, fileToCopy),
			// 		Path.Combine(microPluginDir, fileToCopy)
			// 	);
			// }

			/*
			 * Oh god why
			 */
			// var dllName = "NextUIBrowser.dll";
			// var baseMicroPluginDir = Path.Combine(pluginDir, "microplugin");
			// var microPluginDir = Path.Combine(NextUIPlugin.pluginInterface.GetPluginConfigDirectory(), "microplugin");
			// var cefDir = microPluginDir;
			// // Files doesn't exist, copy and forget
			// if (!File.Exists(Path.Combine(microPluginDir, dllName))) {
			// 	PluginLog.Log("Copying MicroPlugin to " + microPluginDir);
			// 	Copy(baseMicroPluginDir, microPluginDir);
			// }


			/*
			 * OR RAW
			 */
			// var dllName = $"NextUIBrowser.dll";
			// var microPluginDir = Path.Combine(pluginDir, $"microplugin");

			// sharedTypes: new[] { typeof(INuPlugin), typeof(IGuiManager) },
			// isUnloadable: false,
			microPluginLoader = McMaster.NETCore.Plugins.PluginLoader.CreateFromAssemblyFile(
				assemblyFile: Path.Combine(microPluginDir, dllName),
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

			MicroPluginResetEvent.Wait();

			PluginLog.Log("Is plugin collect? " + assembly.IsCollectible);


			microPlugin.Shutdown();
			microPluginLoader.Dispose();

			microPlugin = null;
			microPluginLoader = null;

			GC.Collect(); // collects all unused memory
			GC.WaitForPendingFinalizers(); // wait until GC has finished its work
			GC.Collect();
		}
	}
}