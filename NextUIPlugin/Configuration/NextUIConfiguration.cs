using System;
using Dalamud.Configuration;

namespace NextUIPlugin.Configuration {
	
	[Serializable]
	// ReSharper disable once InconsistentNaming
	public class NextUIConfiguration : IPluginConfiguration {
		public int Version { get; set; } = 1;
		public int socketPort = 32805;
	}
}