using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using I2.Loc;

namespace yellow_taxi_goes_ap;

[HarmonyPatch(typeof(ModMaster))]
[HarmonyPatch(nameof(ModMaster.OnPlayerOnGearCollect))]
public class OnPlayerOnGearCollectPatch
{
    static void Prefix(bool alreadyTaken)
    {
        try
        {
            var mapArea = MapArea.instancePlayerInside;
            var levelId = GameplayMaster.instance.levelId;
            var bonusScript = PlayerScript.instance.GetPrivateField<BonusScript>("bonusScr");

            Plugin.logger.LogInfo($"gear array index: {bonusScript.gearArrayIndex}");

            var apGearIndex = mapArea.gearsId.AsEnumerable()
                .OrderBy(arrayIndex => arrayIndex)
                .Select((arrayIndex, apIndex) => (arrayIndex, apIndex))
                .First((gear) => gear.arrayIndex == bonusScript.gearArrayIndex)
                .apIndex;

            Plugin.logger.LogInfo($"YTGV {bonusScript.gearArrayIndex} -> AP {apGearIndex}");

            var localAreaName = LocalizationManager.GetTranslation(mapArea.areaNameKey, overrideLanguage: "English");
            var location = $"{localAreaName} | Gear {apGearIndex + 1}";

            Plugin.logger.LogWarning($"Got gear: `{location}`{(alreadyTaken ? " (already taken)" : "")}");

            Archipelago.OnLocationCollected(location);
        }
        catch (Exception e)
        {
            Plugin.logger.LogError($"Collectible error: {e}");
        }
    }
}

