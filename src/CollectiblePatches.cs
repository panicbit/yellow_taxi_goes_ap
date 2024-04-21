using System;
using System.CodeDom;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Extensions.Enum;
using HarmonyLib;
using I2.Loc;
using UnityEngine;
using UnityEngine.Bindings;
using SharpGLTF.Schema2;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

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
        if (bonusScript.myIdentity == BonusScript.Identity.gear /* && GameplayMaster.instance.levelId >= Data.LevelId.Hub */)
        {
            if (GameplayMaster.instance.timeAttackLevel)
            {
                return;
            }

            var levelId = GameplayMaster.instance.levelId;
            var gearName = Archipelago.ArchipelagoGearLocation(levelId, bonusScript.gearArrayIndex);

            Logger.LogWarning($"Collected gear `{gearName}`");

            bool alreadyTaken = Data.GearStateGetAbsolute(
                (int)levelId,
                bonusScript.gearArrayIndex
            );

            Logger.LogWarning($"Gear already taken: {alreadyTaken}");

            if (!alreadyTaken)
            {
                Data.GearStateSetAbsolute(
                    (int)levelId,
                    bonusScript.gearArrayIndex,
                    true
                );

                Data.SaveGame();

                Archipelago.OnGearCollected(levelId, bonusScript.gearArrayIndex);
            }
        }
    }
}

[HarmonyPatch(typeof(PersonScenziatoV2))]
[HarmonyPatch(nameof(PersonScenziatoV2.Awake))]
public class MorioAwakePatch
{
    static void Postfix(PersonScenziatoV2 __instance)
    {
        var moriosGearCollected = Data.GearStateGetAbsolute((int)Data.LevelId.Hub, 0);

        if (!moriosGearCollected)
        {
            __instance.instantDialogueInsideRing = true;
        }
    }
}

[HarmonyPatch(typeof(PersonScenziatoV2))]
[HarmonyPatch("ChooseDialogue")]
public class MorioDialoguePatch
{
    static void Postfix(PersonScenziatoV2 __instance)
    {
        var moriosGearCollected = Data.GearStateGetAbsolute((int)Data.LevelId.Hub, 0);

        if (!moriosGearCollected)
        {
            __instance.instantDialogueInsideRing = true;
            __instance.dialoguePickup = __instance.dialogue_initialNoGears;
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

[HarmonyPatch(typeof(Data))]
[HarmonyPatch(nameof(Data.SaveGame))]
public class SaveGamePatch
{
    static void Postfix()
    {
        Logger.LogWarning("Save game data called!");

        if (Archipelago.Enabled)
        {
            Archipelago.RefreshGameState();
        }
    }
}

[HarmonyPatch(typeof(Data))]
[HarmonyPatch(nameof(Data.LoadGame))]
public class LoadGamePatch
{
    static void Postfix()
    {
        Logger.LogWarning("Load game data called!");

        if (Archipelago.Enabled)
        {
            Archipelago.RefreshGameState();
        }
    }
}

[HarmonyPatch(typeof(Data))]
[HarmonyPatch("CreateLevelData")]
public class CreateLevelDataPatch
{
    static void Postfix()
    {
        Logger.LogError("CreateLevelData called!");
    }
}

[HarmonyPatch(typeof(BonusScript))]
[HarmonyPatch(nameof(BonusScript.Awake))]
public class VisualsPatch
{

    static void Postfix(BonusScript __instance)
    {
        // TODO: replace gear mesh only if location has AP item
        return;

        if (!Archipelago.Enabled)
        {
            return;
        }

        if (__instance.myIdentity != BonusScript.Identity.gear)
        {
            return;
        }

        try
        {
            var meshFilter = __instance.myMeshFilter;
            var pluginRoot = Path.GetDirectoryName(Plugin.instance.Info.Location);

            // TODO: Move outside of `Awake` method
            var modelRoot = ModelRoot.Load(Path.Combine(pluginRoot, "archipelago.gltf"));

            {
                var (mesh, position, scale, rotation) = gltfModelToMesh(modelRoot, "AP Logo");
                meshFilter.transform.localPosition = position;
                meshFilter.transform.localScale = scale;
                meshFilter.transform.localRotation = rotation;
                meshFilter.mesh = mesh;
            }

            {
                var (mesh, position, scale, rotation) = gltfModelToMesh(modelRoot, "AP Logo Outline");
                var outlineMeshFilter = __instance.gearOutlineTr.gameObject.GetComponentInChildren<MeshFilter>();
                outlineMeshFilter.transform.localPosition = position;
                outlineMeshFilter.transform.localScale = scale;
                outlineMeshFilter.transform.localRotation = rotation;
                outlineMeshFilter.mesh = mesh;
            }
        }
        catch (Exception e)
        {
            Logger.LogError($"Failed to patch visuals: {e}");
        }
    }

    public static (UnityEngine.Mesh, Vector3, Vector3, Quaternion) gltfModelToMesh(ModelRoot root, string name, bool inverted = false)
    {
        var node = root.LogicalNodes.First((node) => node.Name == name);
        var gltfMesh = root.LogicalMeshes.First((mesh) => mesh.Name == name);

        var allVertices = new List<Vector3>();
        var allTriangles = new List<int>();

        Logger.LogWarning("======= vertice lengths");
        foreach (var primitive in gltfMesh.Primitives)
        {
            var triangles = primitive.GetTriangleIndices()
                .SelectMany((triangle) => new int[] {
                        allVertices.Count + triangle.A,
                        allVertices.Count + triangle.B,
                        allVertices.Count + triangle.C,
                });

            allTriangles.AddRange(triangles);

            var vertices = primitive
                .GetVertices("POSITION").AsVector3Array()
                .Select((v) => new Vector3(v.X, v.Y, v.Z));

            // var uv = primitive.GetVertices("TEXCOORD_0").AsVector2Array()
            //     .Select((v) => new Vector2(v.X, v.Y))
            //     .ToArray();
            allVertices.AddRange(vertices);
            // var normals = primitive.GetVertices("NORMAL").AsVector3Array()
            //     .Select((v) => new Vector3(v.X, v.Y, v.Z))
            //     .ToArray();
        }

        var mesh = new UnityEngine.Mesh();
        // mesh.subMeshCount = gltfMesh.Primitives.Count;
        mesh.vertices = allVertices.ToArray();

        if (inverted)
        {
            allTriangles.Reverse();
        }
        mesh.triangles = allTriangles.ToArray();
        // mesh.uv = gltfMesh.Primitives
        //     .SelectMany((primitive) =>
        //         primitive
        //         .GetVertices("TEXCOORD_0").AsVector2Array()
        //         .Select((v) => new Vector2(v.X, v.Y))
        //     )
        //     .ToArray();

        mesh.normals = gltfMesh.Primitives
            .SelectMany((primitive) =>
                primitive.GetVertices("NORMAL").AsVector3Array()
                .Select((v) => new Vector3(v.X, v.Y, v.Z))
            )
            .ToArray();

        var position = new Vector3
        {
            x = node.LocalTransform.Translation.X,
            y = node.LocalTransform.Translation.Y,
            z = node.LocalTransform.Translation.Z,
        };

        var scale = new Vector3
        {
            x = node.LocalTransform.Scale.X,
            y = node.LocalTransform.Scale.Y,
            z = node.LocalTransform.Scale.Z,
        };

        var rotation = new Quaternion
        {
            x = node.LocalTransform.Rotation.X,
            y = node.LocalTransform.Rotation.Y,
            z = node.LocalTransform.Rotation.Z,
            w = node.LocalTransform.Rotation.W,
        };

        return (mesh, position, scale, rotation);
    }
}
