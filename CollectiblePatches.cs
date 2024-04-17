using System;
using System.Linq;
using HarmonyLib;
using I2.Loc;
using UnityEngine;
using UnityEngine.Bindings;

namespace yellow_taxi_goes_ap;

[HarmonyPatch(typeof(PlayerScript))]
[HarmonyPatch(nameof(PlayerScript.OnTriggerStay))]
public class OnPlayerOnTriggerStayPatch
{
    static void Prefix(Collider other)
    {
        if (!Tick.IsGameRunning || !Archipelago.Enabled)
        {
            return;
        }

        var bonusScript = other.GetComponent<BonusScript>(); ;

        if (bonusScript == null)
        {
            return;
        }

        // On gear collect
        if (bonusScript.myIdentity == BonusScript.Identity.gear && GameplayMaster.instance.levelId >= Data.LevelId.Hub)
        {
            Plugin.logger.LogWarning($"Trying to collect gear");

            bool alreadyTaken = Data.GearStateGet(
                (int)GameplayMaster.instance.levelId,
                bonusScript.gearArrayIndex / 32,
                bonusScript.gearArrayIndex % 32
            );

            Plugin.logger.LogWarning($"Gear already taken: {alreadyTaken}");

            if (!alreadyTaken)
            {
                Data.GearStateSet(
                    (int)GameplayMaster.instance.levelId,
                    bonusScript.gearArrayIndex / 32,
                    bonusScript.gearArrayIndex % 32,
                    true
                );

                var mapArea = MapArea.instancePlayerInside;
                var mapAreaObject = MapMaster.GetAreaScriptableObject_ByAreaName(mapArea.areaNameKey);

                Archipelago.OnGearCollected(mapAreaObject, bonusScript.gearArrayIndex);
            }
        }
    }
}

[HarmonyPatch(typeof(GrandmaFinalBoss))]
[HarmonyPatch("OnFinalBlow")]
public class GrandmaOnFinalBlowPatch
{
    static void Prefix()
    {
        Archipelago.OnGrandmaBeaten();
    }
}
