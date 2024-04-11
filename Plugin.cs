using BepInEx;

namespace yellow_taxi_mod
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        private void Awake()
        {
            // Plugin startup logic
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");

            if (!ModMaster.instance)
            {
                Logger.LogError("Mod master is not instanced yet!");
                return;
            }

            ModMaster.instance.ModEnableSet(true);
        }
    }
}
