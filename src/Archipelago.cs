using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Helpers;
using Archipelago.MultiClient.Net.Models;
using Archipelago.MultiClient.Net.Packets;
using I2.Loc;
using TMPro;

namespace yellow_taxi_goes_ap;

public class Archipelago
{
    public static bool Enabled;
    public static string Host;
    public static int Port;
    public static string Slot;
    public static string Password;

    const string GAME = "Yellow Taxi Goes Vroom";

    static ArchipelagoSession session;

    public static int GearsReceived = 0;

    static string Key(string name) => $"{Data.gameDataIndex}_mod_ap_{name}";

    public static void OnGameStart()
    {
        if (!Enabled)
        {
            return;
        }

        if (session != null)
        {
            OnGameEnd();
        }

        GearsReceived = 0;

        Logger.LogInfo("Trying to connect to AP!");

        session = ArchipelagoSessionFactory.CreateSession(Host, Port);

        Logger.LogWarning($"thread outside: {System.Threading.Thread.CurrentThread.ManagedThreadId}");

        _ = ConnectSetupAndLogin();
    }

    async static Task ConnectSetupAndLogin()
    {
        Logger.LogWarning($"thread inside: {System.Threading.Thread.CurrentThread.ManagedThreadId}");

        MenuV2PopupScript.SpawnNew(
            _title: "Archipelago",
            _text: "Connecting...",
            _prompt: "",
            canSelfClose: false,
            isQuestion: false
        );

        RoomInfoPacket roomInfo;

        try
        {
            roomInfo = await session.ConnectAsync();
        }
        catch (Exception e)
        {
            Logger.LogError($"Failed to connect to AP: {e}");

            OnGameEnd();

            MenuV2PopupScript.SpawnNew(
                _title: "Archipelago",
                _text: "Failed to connect!",
                _prompt: "",
                canSelfClose: true,
                isQuestion: false
            );

            return;
        }

        session.Items.ItemReceived += OnItemReceived;

        LoginResult loginResult;

        try
        {
            loginResult = await session.LoginAsync(
                game: GAME,
                name: Slot,
                itemsHandlingFlags: ItemsHandlingFlags.IncludeOwnItems,
                password: Password
            );

            // version: new Version("0.4.4"),
            // tags: [
            //     // "AP",
            //     // "DeathLink",
            //     // "Tracker",
            //     // "TextOnly",
            // ],
            // uuid: null,
            // requestSlotData: false
            // );
        }
        catch (Exception e)
        {
            Logger.LogError($"Failed login to AP: {e}");

            MenuV2PopupScript.SpawnNew(
                _title: "Archipelago",
                _text: "Login failed!",
                _prompt: "",
                canSelfClose: true,
                isQuestion: false
            );

            OnGameEnd();
            return;
        }

        if (loginResult is LoginSuccessful login)
        {
            Logger.LogInfo($"Connected to AP!");
        }
        else if (loginResult is LoginFailure loginFailure)
        {
            Logger.LogError($"Failed login to AP:");

            foreach (var error in loginFailure.Errors)
            {
                Logger.LogError(error);
            }

            foreach (var error in loginFailure.ErrorCodes)
            {
                Logger.LogError(error);
            }

            MenuV2PopupScript.SpawnNew(
                _title: "Archipelago",
                _text: "Login failed!",
                _prompt: "",
                canSelfClose: true
            );

            OnGameEnd();
            return;
        }
        else
        {
            Logger.LogError("Unknown login result");

            OnGameEnd();
            return;
        }

        try
        {
            var slotData = SlotData.FromDictionary(login.SlotData);

            SetLevelCostsFromSlotData(slotData);
        }
        catch (Exception e)
        {
            Logger.LogError($"Failed to set level costs from slot data: {e}");

            OnGameEnd();
            return;
        }

        MenuV2PopupScript.instance?.Close();
        MenuV2Script.instance.GotoStoryScene("SoundMenuPlayModeSelect");

        SendSaveDataItems();
    }

    public static void OnGameEnd()
    {
        if (session == null)
        {
            return;
        }

        MenuV2PopupScript.instance?.Close();

        Logger.LogInfo("Disconnecting from AP!");

        try
        {
            session?.Socket?.DisconnectAsync();
        }
        catch (Exception e)
        {
            Logger.LogError($"Failed to disconnect from AP: {e}");
        }
        finally
        {
            session = null;
            RestoreLevelCosts();
        }
    }

    public static void SaveSettings()
    {
        DataEncryptionMasterExt.SetBool(Key("enabled"), Enabled);
        DataEncryptionMasterExt.SetString(Key("host"), Host);
        DataEncryptionMasterExt.SetInt(Key("port"), Port);
        DataEncryptionMasterExt.SetString(Key("slot"), Slot);
        DataEncryptionMasterExt.SetString(Key("password"), Password);
    }

    public static void LoadSettings()
    {
        Enabled = DataEncryptionMasterExt.GetBool(Key("enabled"), false);
        Host = DataEncryptionMasterExt.GetString(Key("host"), "archipelago.gg");
        Port = DataEncryptionMasterExt.GetInt(Key("port"), 0);
        Slot = DataEncryptionMasterExt.GetString(Key("slot"), "");
        Password = DataEncryptionMasterExt.GetString(Key("password"), "");
    }

    public static void OnItemReceived(ReceivedItemsHelper items)
    {
        var item = items.DequeueItem();
        var itemName = items.GetItemName(item.Item);

        Logger.LogWarning($"!!!! on item received !!!");

        switch (itemName)
        {
            case "Gear":
                {
                    Interlocked.Increment(ref GearsReceived);
                    Interlocked.Increment(ref Data.gearsUnlockedNumber[Data.gameDataIndex]);

                    Logger.LogWarning($"Num gears: `{GearsReceived}`");
                    break;
                }
            default:
                {
                    Logger.LogError($"Received unknown item: `{itemName}`");
                    break;
                }
        }

        // Update portals
        for (int i = 0; i < PortalScript.list.Count; i++)
        {
            if (!(PortalScript.list[i] == null))
            {
                PortalScript.list[i].CostUpdateTry();
                PortalScript.list[i].UpdatePortalToLevelName();
            }
        }
    }

    public static void OnGearCollected(Data.LevelId levelId, int gearArrayIndex)
    {
        if (session == null)
        {
            return;
        }

        string location = ArchipelagoGearLocation(levelId, gearArrayIndex);

        OnLocationCollected(location);
    }

    public static string ArchipelagoGearLocation(Data.LevelId levelId, int gearArrayIndex)
    {
        try
        {
            Logger.LogInfo($"gear array index: {gearArrayIndex}");

            var level = Data.GetLevel(levelId);
            var localLevelName = LocalizationManager.GetTranslation(level.levelName, overrideLanguage: "English");
            var location = $"{localLevelName} | Gear {gearArrayIndex + 1}";

            Logger.LogWarning($"Location: `{location}`");

            return location;
        }
        catch (Exception e)
        {
            Logger.LogError($"Collectible error: {e}");

            return "";
        }
    }
    public static void OnLocationCollected(string name)
    {
        if (session == null)
        {
            return;
        }

        try
        {
            Logger.LogInfo($"Trying to send check `{name}` to AP");
            var locationId = session.Locations.GetLocationIdFromName(GAME, name);

            if (locationId == -1)
            {
                Logger.LogError("Location `{}` could not be resolved to an id");
            }

            session.Locations.CompleteLocationChecks(locationId);
        }
        catch (Exception e)
        {
            Logger.LogError($"Failed to send check `{name}` to AP: {e}");
        }
    }

    public static void OnGrandmaBeaten()
    {
        if (session == null)
        {
            return;
        }

        var statusUpdatePacket = new StatusUpdatePacket();
        statusUpdatePacket.Status = ArchipelagoClientState.ClientGoal;
        session.Socket.SendPacket(statusUpdatePacket);
    }

    public static void SendSaveDataItems()
    {
        if (session == null)
        {
            return;
        }

        if (Data.finalBossDefeated[Data.gameDataIndex])
        {
            OnGrandmaBeaten();
        }

        foreach (var mapArea in MapMaster.instance.mapAreasList)
        {
            // Gears
            foreach (var gearArrayIndex in mapArea.gearsId)
            {
                var isCollected = Data.GearStateGetAbsolute(
                    (int)mapArea.levelId,
                    gearArrayIndex
                );

                if (isCollected)
                {
                    OnGearCollected(mapArea.levelId, gearArrayIndex);
                }
            }
        }
    }

    public static void SetLevelCostsFromSlotData(SlotData slotData)
    {
        Logger.LogInfo("Setting level costs from slot data");

        foreach (var level in Data.levelDataList)
        {
            ref var cost = ref level.levelCost;

            switch (level.levelName)
            {
                case "LEVEL_NAME_MORIOS_HOME": cost = slotData.MoriosIslandRequiredGears; break;
                case "LEVEL_NAME_BOMBEACH": cost = slotData.BombeachRequiredGears; break;
                case "LEVEL_NAME_ARCADE_PANIK": cost = slotData.ArcadePlazaRequiredGears; break;
                case "LEVEL_NAME_PIZZA_TIME": cost = slotData.PizzaTimeRequiredGears; break;
                case "LEVEL_NAME_TOSLA_OFFICES": cost = slotData.ToslaSquareRequiredGears; break;
                case "LEVEL_NAME_CITY": cost = slotData.MauriziosCityRequiredGears; break;
                case "LEVEL_NAME_CRASH_TEST_INDUSTRIES": cost = slotData.CrashTestIndustriesRequiredGears; break;
                case "LEVEL_NAME_MORIOS_MIND": cost = slotData.MoriosMindRequiredGears; break;
                case "LEVEL_NAME_STARMAN_CASTLE": cost = slotData.ObservingRequiredGears; break;
                case "LEVEL_NAME_TOSLA_HQ": cost = slotData.AnticipationRequiredGears; break;
            }
        }
    }

    public static void RestoreLevelCosts()
    {
        Logger.LogInfo("Restoring level costs");

        foreach (var level in Data.levelDataList)
        {
            ref var cost = ref level.levelCost;

            switch (level.levelName)
            {
                case "LEVEL_NAME_MORIOS_HOME": cost = 3; break;
                case "LEVEL_NAME_BOMBEACH": cost = 6; break;
                case "LEVEL_NAME_ARCADE_PANIK": cost = 18; break;
                case "LEVEL_NAME_PIZZA_TIME": cost = 32; break;
                case "LEVEL_NAME_TOSLA_OFFICES": cost = 50; break;
                case "LEVEL_NAME_CITY": cost = 65; break;
                case "LEVEL_NAME_CRASH_TEST_INDUSTRIES": cost = 80; break;
                case "LEVEL_NAME_MORIOS_MIND": cost = 0; break;
                case "LEVEL_NAME_STARMAN_CASTLE": cost = 0; break;
                case "LEVEL_NAME_TOSLA_HQ": cost = 130; break;
            }
        }
    }

    public static void RefreshGameState()
    {
        if (session == null)
        {
            return;
        }

        if (GameplayMaster.instance == null)
        {
            return;
        }

        // Logger.LogWarning($"Gears received: {GearsReceived}");
        Data.gearsUnlockedNumber[Data.gameDataIndex] = GearsReceived;

        if (!GameplayMaster.instance.timeAttackLevel)
        {
            GameplayMaster.instance.UpdateLevelCollectedGearsNumber();
        }

        // Logger.LogWarning($"Gears unlocked after all the updating: {Data.gearsUnlockedNumber[Data.gameDataIndex]}");
    }
}

