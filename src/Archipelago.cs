using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Helpers;
using Archipelago.MultiClient.Net.Models;
using Archipelago.MultiClient.Net.Packets;
using I2.Loc;

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

        Plugin.logger.LogInfo("Trying to connect to AP!");

        session = ArchipelagoSessionFactory.CreateSession(Host);

        Plugin.logger.LogWarning($"thread outside: {System.Threading.Thread.CurrentThread.ManagedThreadId}");

        _ = ConnectSetupAndLogin();
    }

    async static Task ConnectSetupAndLogin()
    {
        Plugin.logger.LogWarning($"thread inside: {System.Threading.Thread.CurrentThread.ManagedThreadId}");

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
            Plugin.logger.LogError($"Failed to connect to AP: {e}");

            MenuV2PopupScript.SpawnNew(
                _title: "Archipelago",
                _text: "Failed to connect!",
                _prompt: "",
                canSelfClose: true,
                isQuestion: false
            );

            OnGameEnd();
            return;
        }

        session.Items.ItemReceived += OnItemReceived;

        LoginResult loginResult;

        try
        {
            loginResult = await session.LoginAsync(
                game: GAME,
                name: Slot,
                itemsHandlingFlags: ItemsHandlingFlags.IncludeOwnItems
            );

            // version: new Version("0.4.4"),
            // tags: [
            //     // "AP",
            //     // "DeathLink",
            //     // "Tracker",
            //     // "TextOnly",
            // ],
            // uuid: null,
            // password: Password,
            // requestSlotData: false
            // );
        }
        catch (Exception e)
        {
            Plugin.logger.LogError($"Failed login to AP: {e}");

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

        if (loginResult is LoginFailure loginFailure)
        {
            Plugin.logger.LogError($"Failed login to AP:");

            foreach (var error in loginFailure.Errors)
            {
                Plugin.logger.LogError(error);
            }

            foreach (var error in loginFailure.ErrorCodes)
            {
                Plugin.logger.LogError(error);
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

        Plugin.logger.LogInfo($"Connected to AP!");

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

        Plugin.logger.LogInfo("Disconnecting from AP!");

        try
        {
            session?.Socket?.DisconnectAsync();
        }
        catch (Exception e)
        {
            Plugin.logger.LogError($"Failed to disconnect from AP: {e}");
        }
        finally
        {
            session = null;
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

        switch (itemName)
        {
            case "Gear":
                {
                    Interlocked.Increment(ref GearsReceived);
                    Interlocked.Increment(ref Data.gearsUnlockedNumber[Data.gameDataIndex]);
                    Plugin.logger.LogWarning($"Num gears: `{GearsReceived}`");
                    break;
                }
            default:
                {
                    Plugin.logger.LogError($"Received unknown item: `{itemName}`");
                    break;
                }
        }
    }

    public static void OnGearCollected(MapAreaScriptableObject mapArea, int gearArrayIndex)
    {
        if (session == null)
        {
            return;
        }

        string location = ArchipelagoGearLocation(mapArea, gearArrayIndex);

        OnLocationCollected(location);
    }

    static string ArchipelagoGearLocation(MapAreaScriptableObject mapArea, int gearArrayIndex)
    {
        try
        {
            Plugin.logger.LogInfo($"gear array index: {gearArrayIndex}");

            var apGearIndex = mapArea.gearsId.AsEnumerable()
                .OrderBy(arrayIndex => arrayIndex)
                .Select((arrayIndex, apIndex) => (arrayIndex, apIndex))
                .First((gear) => gear.arrayIndex == gearArrayIndex)
                .apIndex;

            Plugin.logger.LogInfo($"YTGV {gearArrayIndex} -> AP {apGearIndex}");

            var localAreaName = LocalizationManager.GetTranslation(mapArea.areaName, overrideLanguage: "English");
            var location = $"{localAreaName} | Gear {apGearIndex + 1}";

            Plugin.logger.LogWarning($"Got gear: `{location}`");

            return location;
        }
        catch (Exception e)
        {
            Plugin.logger.LogError($"Collectible error: {e}");

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
            Plugin.logger.LogInfo($"Trying to send check `{name}` to AP");
            var locationId = session.Locations.GetLocationIdFromName(GAME, name);

            session.Locations.CompleteLocationChecks(locationId);
        }
        catch (Exception e)
        {
            Plugin.logger.LogError($"Failed to send check `{name}` to AP: {e}");
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
                var isCollected = Data.GearStateGet(
                    (int)mapArea.levelId,
                    gearArrayIndex / 32,
                    gearArrayIndex % 32
                );

                if (isCollected)
                {
                    OnGearCollected(mapArea, gearArrayIndex);
                }
            }
        }
    }

    public static void FixGameState()
    {
        if (session == null)
        {
            return;
        }

        Data.gearsUnlockedNumber[Data.gameDataIndex] = GearsReceived;
    }
}

