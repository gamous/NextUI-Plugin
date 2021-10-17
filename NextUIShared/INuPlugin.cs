namespace NextUIShared {
	public interface INuPlugin {
		string GetName();
		void Initialize(string pluginDir, string cefDir, IGuiManager manager);
		void Shutdown();
	}
}