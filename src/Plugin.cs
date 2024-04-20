using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine.SceneManagement;
using System;
using System.Threading;

namespace yellow_taxi_goes_ap;

[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    public static Plugin instance;
    public static ManualLogSource logger;

    public Harmony harmony;

    private void Awake()
    {
        instance = this;
        logger = Logger;

        // Plugin startup logic
        Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");

        PatchWhenReady();
        DumpMapInfoWhenReady();
    }

    void PatchWhenReady()
    {
        var patched = false;

        SceneManager.sceneLoaded += (scene, loadSceneMode) =>
        {
            System.Console.WriteLine("Checking if we need to patch");

            if (patched)
            {
                return;
            }

            System.Console.WriteLine("Patching now");

            patched = true;

            harmony = new Harmony("yellow_taxi_goes_ap");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        };
    }

    void DumpMapInfoWhenReady()
    {
        new Thread(() =>
        {
            while (true)
            {
                Thread.Sleep(1000);

                try
                {
                    MapDumper.SaveMapInfos("map_infos.json");
                    logger.LogInfo("Saved map infos!");
                    break;
                }
                catch (Exception e)
                {
                    logger.LogError($"Dump error: {e}");
                }
            }
        }).Start();
    }

    void OnDestroy()
    {
        harmony?.UnpatchSelf();
    }
}
