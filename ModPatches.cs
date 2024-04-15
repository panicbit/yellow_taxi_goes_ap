using HarmonyLib;

namespace yellow_taxi_goes_ap;

[HarmonyPatch(typeof(ModMaster))]
[HarmonyPatch(nameof(ModMaster.Start))]
public class ModMasterStartPatch
{
    static void Prefix()
    {
        System.Console.WriteLine("Patching works!");
        ModMaster.instance.ModEnableSet(true);
        Master.instance.DEBUG = true;
        Master.instance.SHOW_TESTER_BUTTONS = true;
    }
}

[HarmonyPatch(typeof(ModMaster))]
[HarmonyPatch(nameof(ModMaster.Update))]
public class ModMasterUpdatePatch
{
    static void Prefix()
    {
    }
}
