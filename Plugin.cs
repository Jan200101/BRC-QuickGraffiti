using BepInEx;
using HarmonyLib;

namespace QuickGraffiti
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        private static Harmony harmony;

        public Plugin() => harmony = new Harmony(PluginInfo.PLUGIN_GUID + ".patch");
        private void Awake()
        {
            harmony?.PatchAll();

            // Plugin startup logic
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        }

        private void OnDestroy()
        {
            harmony?.UnpatchSelf();
        }
    }
}
