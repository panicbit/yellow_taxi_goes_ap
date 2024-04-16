using System;
using System.Linq;
using System.Threading;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Enums;

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

        Plugin.logger.LogInfo("Trying to connect to AP!");

        session = ArchipelagoSessionFactory.CreateSession(Host);

        var loginResult = session.TryConnectAndLogin(
            game: GAME,
            name: Slot,
            itemsHandlingFlags: ItemsHandlingFlags.IncludeOwnItems
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
        );

        if (loginResult is LoginFailure loginFailure)
        {
            foreach (var error in loginFailure.Errors)
            {
                Plugin.logger.LogError(error);
            }

            foreach (var error in loginFailure.ErrorCodes)
            {
                Plugin.logger.LogError(error);
            }
        }

        if (loginResult.Successful)
        {
            Plugin.logger.LogInfo($"Connected to AP!");
        }
        else
        {
            Plugin.logger.LogError($"Failed to connect to AP!");
            session = null;
        }
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
            session?.Socket?.DisconnectAsync().Wait(5000);
        }
        catch (Exception e)
        {
            Plugin.logger.LogError($"Failed to disconnect from AP: {e}");
        }

        session = null;
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

    public static void OnLocationCollected(string name)
    {
        if (session == null)
        {
            return;
        }

        try
        {
            var locationId = session.Locations.GetLocationIdFromName(GAME, name);

            session.Locations.CompleteLocationChecks(locationId);
        }
        catch (Exception e)
        {
            Plugin.logger.LogError($"Failed to send check to AP: {e}");
        }
    }
}

