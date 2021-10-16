namespace NextUIShared {
	public interface INuPlugin {
		string GetName();
		void Initialize(string dir, IGuiManager guiManager);
		void Shutdown();
	}
}