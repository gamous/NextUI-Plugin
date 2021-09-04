using System;
using Dalamud.Configuration;

namespace NextUIPlugin.Configuration {
	
	[Serializable]
	// ReSharper disable once InconsistentNaming
	public class NextUIConfiguration : IPluginConfiguration {
		public int Version { get; set; }
		public int SocketPort { get; set; }
	}
}