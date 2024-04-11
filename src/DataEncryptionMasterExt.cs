namespace yellow_taxi_goes_ap;

public static class DataEncryptionMasterExt
{
    public static void SetBool(string key, bool value)
    {
        DataEncryptionMaster.SetInt(key, value ? 1 : 0);
    }

    public static void SetString(string key, string value)
    {
        DataEncryptionMaster.SetString(key, value);
    }

    public static void SetInt(string key, int value)
    {
        DataEncryptionMaster.SetInt(key, value);
    }

    public static bool GetBool(string key, bool defaultValue)
    {
        int value = DataEncryptionMaster.GetInt(Data.gameDataIndex, key, defaultValue ? 1 : 0);

        return value == 1;
    }

    public static string GetString(string key, string defaultValue)
    {
        return DataEncryptionMaster.GetString(Data.gameDataIndex, key, defaultValue);
    }

    public static int GetInt(string key, int defaultValue)
    {
        return DataEncryptionMaster.GetInt(Data.gameDataIndex, key, defaultValue);
    }
}