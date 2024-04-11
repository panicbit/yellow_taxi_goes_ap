namespace yellow_taxi_goes_ap;

public class Archipelago
{
    public static bool Enabled;
    public static string Host;
    public static int Port;
    public static string Password;

    static string Key(string name) => $"{Data.gameDataIndex}_mod_ap_{name}";

    public static void SaveSettings()
    {

        DataEncryptionMasterExt.SetBool(Key("enabled"), Enabled);
        DataEncryptionMasterExt.SetString(Key("Host"), Host);
        DataEncryptionMasterExt.SetInt(Key("Port"), Port);
        DataEncryptionMasterExt.SetString(Key("Password"), Password);
    }

    public static void LoadSettings()
    {
        Enabled = DataEncryptionMasterExt.GetBool(Key("enabled"), false);
        Host = DataEncryptionMasterExt.GetString(Key("host"), "archipelago.gg");
        Port = DataEncryptionMasterExt.GetInt(Key("Port"), 0);
        Password = DataEncryptionMasterExt.GetString(Key("Password"), "");
    }
}

