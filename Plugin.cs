﻿using System.Collections.Generic;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine.SceneManagement;
using System.Linq;
using I2.Loc;
using Extensions.Enumerable;

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

    private void OnDestroy()
    {
        harmony?.UnpatchSelf();
    }
}

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

        // var menuElement = Object.Instantiate<MenuV2Element>(null);

        // menuElement.isSubTitle = true;
        // menuElement.gameObject.name = "mod menu item";
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

[HarmonyPatch(typeof(MenuV2Script))]
[HarmonyPatch("MenuVoicesInit")]
public class MenuVoicesInitPatch
{
    public static List<string> menuSubTitles;
    public static List<List<string>> menuVoices;

    public static int originalSettingsLength;
    public static int indexArchipelagoVoice;

    public static Menu archipelagoMenu;

    static void Postfix(ref string[] ___menuSubTitles, ref List<string[]> ___menuVoices)
    {
        menuSubTitles = [.. ___menuSubTitles];
        menuVoices = ___menuVoices.Select((voices) => voices.ToList()).ToList();
        originalSettingsLength = ___menuVoices[MenuV2Script.indexSettings].Length;

        // System.Console.WriteLine("Main menu subtitles:");
        // foreach (var subTitle in ___menuSubTitles)
        // {
        //     System.Console.WriteLine(subTitle);
        // }
        // System.Console.WriteLine("==========");

        // System.Console.WriteLine("Main menu title: " + ___menuSubTitles[4]);

        // ___menuSubTitles[4] = "Pain Menu";


        // System.Console.WriteLine("Main menu voices:");
        // foreach (var voice in ___menuVoices[4])
        // {
        //     System.Console.WriteLine(voice);
        // }
        // System.Console.WriteLine("==========");

        archipelagoMenu = new Menu("Archipelago", [
            "Enabled: ???",
            "Host: ???",
            "Port: ???",
            "Password: ???",
            LocalizationManager.GetTermTranslation("MENU_VOICE_BACK", true, 0, true, false, null, null, true),
        ]);

        var settings = menuVoices[MenuV2Script.indexSettings];

        indexArchipelagoVoice = settings.Count;
        menuVoices[MenuV2Script.indexSettings].Add("Archipelago");

        Plugin.logger.LogInfo($"Archipelago voice index: {indexArchipelagoVoice}");
        Plugin.logger.LogInfo($"Archipelago original settings length: {originalSettingsLength}");

        ___menuSubTitles = [.. menuSubTitles];
        ___menuVoices = menuVoices.Select((voices) => voices.ToArray()).ToList();
    }

    // static void AddSettingsVoice(string name)
    // {
    //     var voices = SettingsVoices.ToList();
    //     voices.Insert(SettingsVoices.Length - 1, name);
    //     SettingsVoices = voices.ToArray();
    // }

    public class Menu
    {
        public int index;
        public string title;
        public string[] entries;

        public Menu(string title, string[] entries)
        {
            this.title = title;
            this.entries = entries;
            this.index = menuSubTitles.Count;

            menuSubTitles.Add(title);
            menuVoices.Add([.. entries]);

            if (!MenuV2Script.instance.isPauseMenu)
            {
                ref var cameraPoints = ref MenuV2CameraController.instance.cameraPoints;

                while (cameraPoints.Length < menuSubTitles.Count)
                {
                    cameraPoints = [.. cameraPoints, null];
                }

                cameraPoints[this.index] = cameraPoints[MenuV2Script.indexSettings];
            }
        }
    }
}

[HarmonyPatch(typeof(MenuV2Script))]
[HarmonyPatch("MenuSelection")]
public class MenuSelectionPatch
{
    static int menuIndex
    {
        get => MenuV2Script.instance.menuIndex;
        set { MenuV2Script.instance.menuIndex = value; }
    }

    static int voiceIndex
    {
        get => MenuV2Script.instance.voiceIndex;
        set { MenuV2Script.instance.voiceIndex = value; }
    }

    static bool Prefix()
    {
        MenuV2Element.FlashTextIndex(voiceIndex);
        MenuV2Script.instance.SetUpdateVisualElements(true);

        var selectionDelayTimer = MenuV2Script.instance.GetSelectionDelayTimer();
        MenuV2Script.instance.SetSelectionDelayTimer(System.Math.Max(0.1f, selectionDelayTimer));

        if (menuIndex == MenuV2Script.indexSettings)
        {
            return handleSettingsMenu();
        }
        else if (menuIndex == MenuVoicesInitPatch.archipelagoMenu.index)
        {
            return handleArchipelagoMenu();
        }

        return true;
    }

    // private static bool handleMenus()
    // {

    //     // this.selectionDelayTimer = 0.75f;
    // }

    private static bool handleSettingsMenu()
    {
        ref var menuIndex = ref MenuV2Script.instance.menuIndex;
        ref var voiceIndex = ref MenuV2Script.instance.voiceIndex;

        if (voiceIndex < MenuVoicesInitPatch.originalSettingsLength)
        {
            return true;
        }

        if (voiceIndex == MenuVoicesInitPatch.indexArchipelagoVoice)
        {
            Plugin.logger.LogInfo("Selected Archipelago menu!");
            menuIndex = MenuVoicesInitPatch.archipelagoMenu.index;
            voiceIndex = 0;
            Sound.Play_Unpausable("SoundMenuSelect", 1f, 1f);
            MenuV2Script.instance.SetSelectionDelayTimer(0.25f);
            return false;
        }
        else
        {
            Plugin.logger.LogError($"Unhandled menu,voice: {menuIndex},{voiceIndex}");
            return true;
        }
    }

    private static bool handleArchipelagoMenu()
    {
        MenuV2Script.instance.SetInstantUpdateVisualElements(true);
        Sound.Play_Unpausable("SoundMenuSelect", 1f, 1f);
        MenuV2Script.instance.SetSelectionDelayTimer(0.1f);

        if (voiceIndex == 0)
        {
            Archipelago.Enabled = !Archipelago.Enabled;
        }
        else if (voiceIndex == 4)
        {
            MenuV2Script.instance.MenuBack();
            MenuV2Script.instance.SetInstantUpdateVisualElements(false);
            MenuV2Script.instance.SetSelectionDelayTimer(0.25f);
        }

        return false;
    }
}

[HarmonyPatch(typeof(MenuV2Script))]
[HarmonyPatch("MenuBack")]
public class MenuBackPatch
{
    static int menuIndex
    {
        get => MenuV2Script.instance.menuIndex;
        set { MenuV2Script.instance.menuIndex = value; }
    }

    static int voiceIndex
    {
        get => MenuV2Script.instance.voiceIndex;
        set { MenuV2Script.instance.voiceIndex = value; }
    }

    static bool Prefix()
    {
        MenuV2Script.instance.SetUpdateVisualElements(true);

        if (menuIndex == MenuVoicesInitPatch.archipelagoMenu.index)
        {
            menuIndex = MenuV2Script.indexSettings;
            voiceIndex = MenuVoicesInitPatch.indexArchipelagoVoice;
            Sound.Play_Unpausable("SoundMenuBack", 1f, 1f);
            Data.SaveGame();
            return false;
        }

        return true;
    }
}

[HarmonyPatch(typeof(MenuV2Script))]
[HarmonyPatch("VoicesUpdate")]
public class VoicesUpdatePatch
{
    static int menuIndex
    {
        get => MenuV2Script.instance.menuIndex;
        set { MenuV2Script.instance.menuIndex = value; }
    }

    static int voiceIndex
    {
        get => MenuV2Script.instance.voiceIndex;
        set { MenuV2Script.instance.voiceIndex = value; }
    }

    static void Postfix()
    {
        MenuV2Script.instance.menuVoices[MenuVoicesInitPatch.archipelagoMenu.index] = [
            $"Enabled: {(Archipelago.Enabled ? "Yes" : "No")}",
            $"Host: {Archipelago.Host}",
            $"Port: {Archipelago.Port}",
            $"Password: {(Archipelago.Password.IsEmpty() ? "Not set" : "Set")}",
            LocalizationManager.GetTermTranslation("MENU_VOICE_BACK", true, 0, true, false, null, null, true),
        ];
    }
}

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
