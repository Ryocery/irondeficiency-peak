using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System.IO;
using IronDeficiency.Handlers;

namespace IronDeficiency;

[BepInAutoPlugin]
public partial class Plugin : BaseUnityPlugin {
    public new static ManualLogSource Logger { get; private set; } = null!;

    private Harmony _harmony = null!;

    private void Awake() {
        Logger = base.Logger;

        Logger.LogInfo($"Plugin {Name} is loading...");
        new Config(Config);

        string? pluginDir = Path.GetDirectoryName(Info.Location);
        if (string.IsNullOrEmpty(pluginDir)) pluginDir = Paths.PluginPath;
        AssetHandler.Init(pluginDir);

        _harmony = new Harmony(Id);
        _harmony.PatchAll();

        Logger.LogInfo($"Plugin {Name} loaded successfully!");
    }

    private void OnDestroy() {
        _harmony?.UnpatchSelf();
    }
}