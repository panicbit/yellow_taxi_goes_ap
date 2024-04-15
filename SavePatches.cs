using HarmonyLib;

namespace yellow_taxi_goes_ap;

[HarmonyPatch(typeof(DataEncryptionMaster))]
[HarmonyPatch("PrepareDataForSaving")]
public class PrepareDataForSavingPatch
{
    static void Postfix()
    {
        Plugin.logger.LogInfo($"Saving data to slot {Data.gameDataIndex}!");
        Archipelago.SaveSettings();
    }
}

[HarmonyPatch(typeof(DataEncryptionMaster))]
[HarmonyPatch("Load")]
public class LoadPatch
{
    static void Postfix()
    {
        Plugin.logger.LogInfo($"Loading data from slot {Data.gameDataIndex}!");
        Archipelago.LoadSettings();
    }
}
